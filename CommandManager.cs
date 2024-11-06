using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LabConfig;

public static class CommandManager
{
    private const int CountUsers = 1;

    public static string? NameUser { get; set; }
    public static string? PathArchive { get; set; }

    public static string CurrDir { get; private set; } = "/";

    public static event Action Close;
    public static event Action<string> SetNewNameUser;
    public static event Action ActivateInputField;

    private static DateTime _startTime;
    
    public static void SetStartTime(DateTime time) => _startTime = time;
    
    public static string Execute(string command)
    {
        if (command == "exit")
        {
            Close?.Invoke();
            return string.Empty;
        }

        if (command.StartsWith("uptime"))
            return UptimeCommand(command);
        
        if (command.StartsWith("who"))
            return WhoCommand(command);
        
        if (command.StartsWith("wc"))
            return FileWcCommand(command.Remove(0, Math.Min(3, command.Length)));
        
        if (command.StartsWith("cd"))
            return CdCommand(command.Remove(0, Math.Min(3, command.Length)));

        if (command.StartsWith("ls"))
            return LsCommandString(command);
        
        return CommandNotFound(command);
    }
    
    public static bool SpecialCommand(string command)
    {
        return command == "wc";
    }
    
    public static void ExecuteSpecialCommand(string command)
    {
        if (command == "wc")
            ActivateInputField?.Invoke();
    }

    #region UptimeCommand
    
    public static string UptimeCommand(string command)
    {
        if (command.Contains("-p") || command.Contains("--pretty"))
            return UptimePretty(command.Replace("uptime -p", "").Replace("uptime --pretty", ""));

        if (command.Contains("-s") || command.Contains("--since"))
            return UptimeSince(command.Replace("uptime -s", "").Replace("uptime --since", ""));
        
        if (command == "uptime")
            return UptimeCommon(command);
            
        return UnknownOption(command);
    }

    private static string UptimeCommon(string command)
    {
        var uptime = DateTime.Now - _startTime;
        var timeup = string.Empty;
            
        if (uptime.Seconds > 0)
            timeup = uptime.Seconds + " sec";
            
        if (uptime.Minutes > 0)
            timeup = uptime.Minutes + " min" + (string.IsNullOrWhiteSpace(timeup) ? "" : " ") + timeup;
            
        if (uptime.Hours > 0)
            timeup = uptime.Hours + " hours" + (string.IsNullOrWhiteSpace(timeup) ? "" : " ") + timeup;
            
        if (uptime.Days > 0)
            timeup = uptime.Days + " days" + (string.IsNullOrWhiteSpace(timeup) ? "" : " ") + timeup;
            
        return 
            DateTime.Now.ToString("HH:mm:ss") + 
            " up " + timeup + "," +
            $" {CountUsers} user, " + 
            "load average: 0.00, 0.00, 0.00";
    }
    
    private static string UptimePretty(string command)
    {
        var uptime = DateTime.Now - _startTime;
        var timeup = string.Empty;
            
        if (uptime.Seconds > 0)
            timeup = uptime.Seconds + " sec";
            
        if (uptime.Minutes > 0)
            timeup = uptime.Minutes + " min" + (string.IsNullOrWhiteSpace(timeup) ? "" : " ") + timeup;
            
        if (uptime.Hours > 0)
            timeup = uptime.Hours + " hours" + (string.IsNullOrWhiteSpace(timeup) ? "" : " ") + timeup;
            
        if (uptime.Days > 0)
            timeup = uptime.Days + " days" + (string.IsNullOrWhiteSpace(timeup) ? "" : " ") + timeup;
            
        return 
            "up " + timeup;
    }

    private static string UptimeSince(string command)
    {
        return DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
    }
    
    #endregion

    #region WhoCommand
    
    public static string WhoCommand(string command)
    {
        if (command.Contains("-a") || command.Contains("--all"))
            return WhoAllCommand(command.Replace("who -a", "").Replace("who --all", ""));
        
        if (command == "who")
            return WhoCommonCommand(command);
            
        return UnknownOption(command);
    }

    private static string WhoAllCommand(string command)
    {
        return WhoCommandGetString(GetAllUsers());
    }

    private static string WhoCommonCommand(string command)
    {
        var users = GetAllUsers().Where(x => x.userName == NameUser).ToList();

        return WhoCommandGetString(users);
    }

    private static string WhoCommandGetString(
        List<(string userName, string nameConnection, DateTime dateIn, string fromConnection)> users
    )
    {
        var result = string.Empty;
        
        foreach (var (userName, nameConnection, dateIn, fromConnection) in users)
        {
            var currUserName = userName.PadRight(users.Max(x => x.userName.Length) + 5, ' ');
            var currNameConnection = nameConnection.PadRight(users.Max(x => x.nameConnection.Length) + 10, ' ');
            var currDateIn = dateIn.ToString("yyyy-MM-dd hh:mm");
            var currFromConnection = string.IsNullOrWhiteSpace(fromConnection) ? "" : $"({fromConnection})";
            
            result += $"{currUserName}{currNameConnection}{currDateIn} {currFromConnection}\n";
        }
        
        return result.Remove(result.Length - 1);
    }

    private static List<(string userName, string nameConnection, DateTime dateIn, string fromConnection)> GetAllUsers()
    {
        return new List<(string userName, string nameConnection, DateTime dateIn, string fromConnection)>()
        {
            (NameUser, "custom-terminal", _startTime, ":0"),
            (NameUser, "custom-terminal-v2", _startTime.AddMinutes(-5), ":0:0"),
            ("god", "void", new DateTime(2077, 11, 9, 1, 1, 1), ""),
        };
    }

    #endregion

    #region WcCommand

    public static string WcCommand(string text)
    {
        var countWords = text.Replace("\r", "").Split(' ', '\n', '\t').Count(x => !string.IsNullOrWhiteSpace(x)).ToString();
        var countLines = text.Replace("\r", "").Count(x => x == '\n').ToString();
        var countChars = text.Replace("\r", "").Length.ToString();
        
        return $"{countWords,7}{countLines,7}{countChars,8}";
    }

    public static string FileWcCommand(string filename)
    {
        if (!File.Exists(PathArchive))
            return FileSystemNotFound();
        
        if (!filename.StartsWith('/'))
            filename = CurrDir + (CurrDir != "/" ? "/" : "") + filename;

        var nameFile = filename.Split("/", StringSplitOptions.RemoveEmptyEntries)[^1];
        var startRemove = filename.Length - nameFile.Length;

        if (filename.Count(x => x == '/') > 1)
            startRemove--;
            
        var filePath = filename.Remove(startRemove);
        
        var checkPath = CheckPath(filePath);

        if (!checkPath.success)
            return FileOrDirNotFound(string.IsNullOrWhiteSpace(checkPath.message) ? "file" : checkPath.message);

        filename = checkPath.message + (checkPath.message != "/" ? "/" : "") + nameFile;
        
        if (!IsFile(filename[1..]))
            return FileOrDirNotFound("(is not a file) " + filename);

        using var archive = ZipFile.OpenRead(PathArchive);
        var file = archive.GetEntry(filename[1..]);
        
        if (file == null)
            return FileOrDirNotFound("(is not a file) " + filename);

        using var reader = new StreamReader(file.Open());
        var text = reader.ReadToEnd();
        
        return WcCommand(text);
    }

    #endregion

    #region CdCommand

    public static string CdCommand(string path)
    {
        if (!File.Exists(PathArchive))
            return FileSystemNotFound();
        
        var checkPath = CheckPath(path);

        if (!checkPath.success)
            return FileOrDirNotFound(string.IsNullOrWhiteSpace(checkPath.message) ? "file" : checkPath.message);

        CurrDir = checkPath.message;

        return string.Empty;
    }

    #endregion

    #region LsCommand
    
    public static string LsCommandString(string command)
    {
        if (command.StartsWith("ls -l") || command.StartsWith("ls --long"))
            return LsLongCommandString(command);
        
        return LsCommonCommandString(command);
    }

    public static string LsCommonCommandString(string command)
    {
        return LsCommonCommand(command.Remove(0, 2)).Aggregate("", (result, item) => result + item.name + "  ");
    }
    
    public static string LsLongCommandString(string command)
    {
        return LsLongCommand(command
            .Replace("ls -l", "")
            .Replace("ls --long", "")
        ).Aggregate("", (result, item) => result + (item.isDir ? "d" : "-") + item.name + "  ");
    }

    public static List<(bool isDir, string name)> LsCommand(string command)
    {
        if (command.StartsWith("ls -l") || command.StartsWith("ls --long"))
            return LsLongCommand(command.Replace("ls -l", "").Replace("ls --long", ""));

        return LsCommonCommand(command.Remove(0, 2));
    }
    
    private static List<(bool isDir, string name)> LsCommonCommand(string path)
    {
        if (!File.Exists(PathArchive))
            return new() { (false, FileSystemNotFound()) };
        
        if (path.StartsWith(' '))
            path = path.Remove(0, 1);
        
        if (string.IsNullOrWhiteSpace(path))
            path = CurrDir;

        var checkPath = CheckPath(path);

        if (!checkPath.success)
            return new()
            {
                (false, FileOrDirNotFound(string.IsNullOrWhiteSpace(checkPath.message) ? "file" : checkPath.message))
            };
        
        if (IsFile(checkPath.message)) return new() { (false, path) };
        
        using var archive = ZipFile.OpenRead(PathArchive);
        var result = archive.Entries.Where(x =>
        {
            var currPathLen = x.FullName.Count(x => x == '/');
            var checkPathLen = checkPath.message.Count(x => x == '/');

            if (checkPath.message != "/")
                checkPathLen++;
            
            var isNeedFile = x.FullName.StartsWith(checkPath.message[1..]) && 
                             currPathLen == checkPathLen - 1 &&
                             !x.FullName.EndsWith('/');
            var isNeedFolder = x.FullName.StartsWith(checkPath.message[1..]) && 
                               currPathLen == checkPathLen &&
                               x.FullName.EndsWith('/');

            return isNeedFile || isNeedFolder;
        }).Select(x => (IsDir(x.FullName), GetName(x))).ToList();
        
        return result;
    }
    
    public static List<(bool isDir, string name)> LsLongCommand(string path)
    {
        if (!File.Exists(PathArchive))
            return new() { (false, FileSystemNotFound()) };
        
        if (path.StartsWith(' '))
            path = path.Remove(0, 1);
        
        if (string.IsNullOrWhiteSpace(path))
            path = CurrDir;

        var checkPath = CheckPath(path);

        if (!checkPath.success)
            return new()
            {
                (false, FileOrDirNotFound(string.IsNullOrWhiteSpace(checkPath.message) ? "file" : checkPath.message))
            };
        
        if (IsFile(checkPath.message)) return new() { (false, path) };
        
        using var archive = ZipFile.OpenRead(PathArchive);
        var resultList = archive.Entries.Where(x =>
        {
            var currPathLen = x.FullName.Count(x => x == '/');
            var checkPathLen = checkPath.message.Count(x => x == '/');

            if (checkPath.message != "/")
                checkPathLen++;
            
            var isNeedFile = x.FullName.StartsWith(checkPath.message[1..]) && 
                             currPathLen == checkPathLen - 1 &&
                             !x.FullName.EndsWith('/');
            var isNeedFolder = x.FullName.StartsWith(checkPath.message[1..]) && 
                               currPathLen == checkPathLen &&
                               x.FullName.EndsWith('/');

            return isNeedFile || isNeedFolder;
        }).ToList();
        
        return resultList.Select(x => 
            (
                IsDir(x.FullName), 
                GetNameFull(x, resultList.Max(x => x.Length.ToString().Length))
            )
        ).ToList();
    }

    #endregion

    private static (bool success, string message) CheckPath(string path)
    {
        var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        
        var curr = path.StartsWith("/") ? "/" : CurrDir;

        foreach (var part in parts)
        {
            if (part == ".") continue;

            if (part == "..")
            {
                if (curr == "/") return (false, $"(absolute path) {part}/");
                
                var lastSlashIndex = curr.LastIndexOf('/');
                if (lastSlashIndex >= 0) curr = curr[..lastSlashIndex];

                if (string.IsNullOrEmpty(curr)) curr = "/";
                
                continue;
            }
            
            if (part == "...") return (false, $"(is a file) {part}");

            if (curr == "/")
                curr += part;
            else
                curr = curr + "/" + part;

            if (!IsFileOrDirExist(curr[1..])) return (false, part);
            
            if (IsFile(curr[1..])) return (false, $"(is a file) {part}");
        }
        
        return (true, curr);
    }
    
    private static bool IsFileOrDirExist(string filename)
    {
        if (!File.Exists(PathArchive))
            return false;
        
        using var archive = ZipFile.OpenRead(PathArchive);

        if (archive.Entries.Any(x => x.FullName == filename + "/" && x.ExternalAttributes == 16))
            return true;

        if (archive.Entries.Any(x => x.FullName == filename))
            return true;

        return false;
    }
    
    private static bool IsFile(string filename)
    {
        if (!File.Exists(PathArchive))
            return false;
        
        using var archive = ZipFile.OpenRead(PathArchive);
        
        return archive.Entries.Any(x => x.FullName == filename);
    }
    
    private static bool IsDir(string filename)
    {
        if (!File.Exists(PathArchive))
            return false;
        
        using var archive = ZipFile.OpenRead(PathArchive);
        
        return archive.Entries.Any(x => x.FullName == filename && x.ExternalAttributes == 16);
    }

    private static string CommandNotFound(string command)
    {
        return "command not found: " + command;
    }
    
    private static string UnknownOption(string command)
    {
        if (command.Split().Length < 2)
            return string.Empty;
        
        var nameCommand = command.Split()[0];
        
        return $"Unknown {nameCommand} option: {command.Replace(nameCommand + " ", "")}";
    }
    
    private static string FileOrDirNotFound(string filename)
    {
        return $"No such file or directory: {filename}";
    }
    
    private static string FileSystemNotFound()
    {
        return $"File system not found: {PathArchive}";
    }

    private static string GetName(ZipArchiveEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Name) && string.IsNullOrWhiteSpace(entry.FullName))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(entry.Name))
            return entry.FullName.Split("/", StringSplitOptions.RemoveEmptyEntries)[^1];

        return entry.Name;
    }
    
    private static string GetNameFull(ZipArchiveEntry entry, int maxSize)
    {
        var result = string.Empty;

        if (IsDir(entry.FullName))
            result += "rwxr-xr-x ";
        else
            result += "rw-r--r-- ";

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        result += $"1 {NameUser} {NameUser} {(IsDir(entry.FullName) ? "-" : entry.Length.ToString()).PadLeft(maxSize, ' ')} {entry.LastWriteTime:MMM dd HH:mm} {GetName(entry)}";

        return result;
    }
}