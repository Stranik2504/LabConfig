using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Mono.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GraphDependencies;

internal class Program
{
    private const bool IsDebug = true;
    
    public static async Task Main(string[] args)
    {
        var pathVisualizator = string.Empty;
        var namePackage = string.Empty;
        var maxDepthRecursion = 0;
        var urlRepository = string.Empty;
        
        new OptionSet()
        {
            { "p|path=", "Set the path to visualizator", v => pathVisualizator = v },
            { "n|name=", "Set the name package", v => namePackage = v },
            { "d|depth=", "Set the max depth recursion", (int v) => maxDepthRecursion = v },
            { "u|url=", "Set the url repository", v => urlRepository = v }
        }.Parse(args);
        
        if (IsDebug)
        {
            pathVisualizator = @"C:\Users\rund2\Downloads\Graphviz-12.2.0-win64\bin\dot.exe";
            // namePackage = "libreoffice-core";
            namePackage = "erlang-asn1";
            maxDepthRecursion = -1;
            urlRepository = "http://archive.ubuntu.com/ubuntu/ubuntu/dists/noble/main/";
        }

        if (string.IsNullOrWhiteSpace(urlRepository))
        {
            Console.WriteLine("Url Repository is required, but it empty");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(namePackage))
        {
            Console.WriteLine("Name package is required, but it empty");
            return;
        }
        
        var packageUrl = await GraphDependencies.FindPackageUrl(urlRepository);
        
        if (packageUrl == null)
        {
            Console.WriteLine("Package not found");
            return;
        }
        
        var packages = await GraphDependencies.GetAllPackages(packageUrl);
        packages = packages.OrderBy(x => x.Name).ToList();
        var package = GraphDependencies.GetPackageByName(packages, namePackage);

        if (package == default)
        {
            Console.WriteLine("Package not found");
            return;
        }
        
        var result = GraphDependencies.GetDependenciesRecursive(
            new RecursionParams(packages, package, maxDepthRecursion)
        );

        result.Insert(0, "digraph {\n");
        result.Append('}');

        var a = result.ToString();
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = pathVisualizator,
                Arguments = $"-v -Tpng",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        await process.StandardInput.WriteLineAsync(a);
        process.StandardInput.Close();

        using var memoryStream = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        
        process.Close();

        if (IsDebug)
        {
            await using var fileStream = new FileStream("output.png", FileMode.Create, FileAccess.Write);
            await memoryStream.CopyToAsync(fileStream);
        
            memoryStream.Seek(0, SeekOrigin.Begin);
        }
        
        Console.WriteLine(ConvertImageToAscii(memoryStream));

        Console.ReadLine();
    }

    private static string ConvertImageToAscii(Stream stream)
    {
        using var image = Image.Load<Rgba32>(stream);

        var sb = new StringBuilder();
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                sb.Append(GetAsciiChar(pixel));
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static char GetAsciiChar(Rgba32 pixel)
    {
        const string chars = "@%#*+=-:. ";
        var gray = (pixel.R + pixel.G + pixel.B) / 3;
        var index = gray * (chars.Length - 1) / 255;
        return chars[index];
    }
}

public static class GraphDependencies
{
    #region FindPackageUrl

    // Example urlRepository: http://archive.ubuntu.com/ubuntu/ubuntu/dists/noble/main/
    public static async Task<string?> FindPackageUrl(string urlRepository)
    {
        using var client = new HttpClient();
        var visitedUrls = new HashSet<string>();
        return await FindPackageUrlRecursive(client, urlRepository, visitedUrls);
    }

    private static async Task<string?> FindPackageUrlRecursive(HttpClient client, string url, HashSet<string> visitedUrls)
    {
        if (!visitedUrls.Add(url)) return null;

        var response = await client.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
        {
            var href = link.GetAttributeValue("href", string.Empty);
            var nextUrl = new Uri(new Uri(url), href).ToString();
            
            if (url.StartsWith(nextUrl)) continue;
            
            if (href.EndsWith("Packages.gz"))
                return nextUrl;

            if (!href.EndsWith($"/")) continue;
            
            var result = await FindPackageUrlRecursive(client, nextUrl, visitedUrls);
            if (result != null) return result;
        }

        return default;
    }

    #endregion

    public static async Task<List<Package>> GetAllPackages(string packageUrl)
    {
        var packages = new List<Package>();
        
        using var client = new HttpClient();
        var response = await client.GetStreamAsync(packageUrl);
        
        if (response is not { CanRead: true }) return packages;

        await using var gzipStream = new GZipStream(response, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);

        var tmp = new Package();
        
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            
            if (string.IsNullOrWhiteSpace(line))
            {
                packages.Add(tmp);
                tmp = new Package();
                continue;
            }

            if (line.StartsWith("Package: "))
                tmp.Name = line["Package: ".Length..];

            if (!line.StartsWith("Depends: ")) continue;
            
            tmp.Dependencies = line["Depends: ".Length..].Split(", ").Select(x => x.Split('|', '(', ')')[0].TrimEnd()).ToList();
        }

        return packages;
    }

    public static Package? GetPackageByName(List<Package> packages, string name)
    {
        return packages.FirstOrDefault(x => x.Name == name);
    }
    
    public static StringBuilder GetDependenciesRecursive(RecursionParams recParams)
    {
        if (!recParams.Used.Add(recParams.CurrPackage)) return recParams.Result;
        if (recParams.MaxDepth > 0 && recParams.CurrDepth >= recParams.MaxDepth) return recParams.Result;
        
        if (!recParams.AddedTag.Contains(recParams.CurrPackage.Name))
            recParams.Result.Append(MakeTag(recParams.CurrPackage.Name));
        
        foreach (var dependency in recParams.CurrPackage.Dependencies)
        {
            if (!recParams.AddedTag.Contains(dependency))
                recParams.Result.Append(MakeTag(dependency));
            
            recParams.Result.Append($"\t{ToTag(recParams.CurrPackage.Name)} -> {ToTag(dependency)};\n");
            
            var nextPackage = GetPackageByName(recParams.Packages, dependency);
            
            if (nextPackage == default) continue;
            
            GetDependenciesRecursive(
                new RecursionParams(recParams.Packages, nextPackage, recParams.MaxDepth)
                {
                    AddedTag = recParams.AddedTag,
                    Used = recParams.Used,
                    Result = recParams.Result,
                    CurrDepth = recParams.CurrDepth + 1
                }
            );
        }

        return recParams.Result;
    }

    private static string MakeTag(string name)
    {
        return "\t" + 
               ToTag(name) + 
               " [label=\"" + name + "\"];" +
               "\n";
    }
    
    private static string ToTag(string name)
    {
        return Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
    }
}

public class Package
{
    public string Name { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = [];
}

public class RecursionParams(List<Package> packages, Package currPackage, int maxDepth = -1)
{
    public List<Package> Packages { get; set; } = packages;
    public Package CurrPackage { get; set; } = currPackage;
    public HashSet<string> AddedTag { get; set; } = [];
    public HashSet<Package> Used { get; set; } = [];
    public int MaxDepth { get; set; } = maxDepth;
    public StringBuilder Result { get; set; } = new();
    public int CurrDepth { get; set; }
}
