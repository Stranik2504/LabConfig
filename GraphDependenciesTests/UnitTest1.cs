using System.Text;
using GraphDependencies;

namespace GraphDependenciesTests;

public class UnitTest1
{
    [Fact]
    public async Task TestFindPackageUrl()
    {
        var res = await GraphDependencies.GraphDependencies.FindPackageUrl(
            "http://archive.ubuntu.com/ubuntu/ubuntu/dists/noble/main/"
        );
        
        Assert.Equal("http://archive.ubuntu.com/ubuntu/ubuntu/dists/noble/main/binary-amd64/Packages.gz", res);
    }
    
    [Fact]
    public async Task TestGetAllPackages()
    {
        var res = await GraphDependencies.GraphDependencies.GetAllPackages(
            "http://archive.ubuntu.com/ubuntu/ubuntu/dists/noble/main/binary-amd64/Packages.gz"
        );
        
        Assert.Contains(res, x => x.Name == "accountsservice");
    }
    
    [Fact]
    public void TestGetPackageByName()
    {
        var list = new List<Package>()
        {
            new Package()
            {
                Name = "accountsservice",
                Dependencies =
                [
                    "libc6",
                    "libglib2.0-0",
                    "libpolkit-gobject-1-0",
                    "libaccountsservice0"
                ]
            },
            new Package()
            {
                Name = "acl",
                Dependencies = ["libc6"]
            },
            new Package()
            {
                Name = "adduser",
                Dependencies =
                [
                    "debconf",
                    "libc6",
                    "libaudit1",
                    "libcap2",
                    "libdebconfclient0",
                    "liblocale-gettext-perl",
                    "libselinux1",
                    "libsemanage1",
                    "libsepol1",
                    "libslang2",
                    "passwd"
                ]
            },
            new Package()
            {
                Name = "adwaita-icon-theme",
                Dependencies =
                [
                    "libgdk-pixbuf2.0-0",
                    "libglib2.0-0",
                    "libgtk-3-0",
                    "librsvg2-2",
                    "librsvg2-common"
                ]
            },
        };

        var res = GraphDependencies.GraphDependencies.GetPackageByName(list, "adduser");
        
        Assert.Equal(list[2], res);
    }
    
    [Fact]
    public void TestGetDependenciesRecursive()
    {
        var list = new List<Package>()
        {
            new Package()
            {
                Name = "accountsservice",
                Dependencies =
                [
                    "libc6",
                    "libglib2.0-0",
                    "libpolkit-gobject-1-0",
                    "libaccountsservice0"
                ]
            },
            new Package()
            {
                Name = "acl",
                Dependencies = ["libc6"]
            },
            new Package()
            {
                Name = "adduser",
                Dependencies =
                [
                    "debconf",
                    "libc6",
                    "libaudit1",
                    "libcap2",
                    "libdebconfclient0",
                    "liblocale-gettext-perl",
                    "libselinux1",
                    "libsemanage1",
                    "libsepol1",
                    "libslang2",
                    "passwd"
                ]
            },
            new Package()
            {
                Name = "adwaita-icon-theme",
                Dependencies =
                [
                    "libgdk-pixbuf2.0-0",
                    "libglib2.0-0",
                    "libgtk-3-0",
                    "librsvg2-2",
                    "librsvg2-common"
                ]
            },
            new Package()
            {
                Name = "test",
                Dependencies =
                [
                    "acl",
                    "accountsservice"
                ]
            },
        };

        var res = GraphDependencies.GraphDependencies.GetDependenciesRecursive(
            new RecursionParams(list, list[^1], 0)
        );
        
        const string expected = "\ttest [label=\"test\"];\n\tacl [label=\"acl\"];\n\ttest -> acl;\n\tacl [label=\"acl\"];\n\tlibc6 [label=\"libc6\"];\n\tacl -> libc6;\n\taccountsservice [label=\"accountsservice\"];\n\ttest -> accountsservice;\n\taccountsservice [label=\"accountsservice\"];\n\tlibc6 [label=\"libc6\"];\n\taccountsservice -> libc6;\n\tlibglib2_0_0 [label=\"libglib2.0-0\"];\n\taccountsservice -> libglib2_0_0;\n\tlibpolkit_gobject_1_0 [label=\"libpolkit-gobject-1-0\"];\n\taccountsservice -> libpolkit_gobject_1_0;\n\tlibaccountsservice0 [label=\"libaccountsservice0\"];\n\taccountsservice -> libaccountsservice0;\n";
        
        Assert.Equal(expected, res.ToString());
    }
    
    [Fact]
    public void TestGetDependenciesRecursive2()
    {
        var list = new List<Package>()
        {
            new Package()
            {
                Name = "accountsservice",
                Dependencies =
                [
                    "libc6",
                    "libglib2.0-0",
                    "libpolkit-gobject-1-0",
                    "libaccountsservice0"
                ]
            },
            new Package()
            {
                Name = "acl",
                Dependencies = ["libc6"]
            },
            new Package()
            {
                Name = "adduser",
                Dependencies =
                [
                    "debconf",
                    "libc6",
                    "libaudit1",
                    "libcap2",
                    "libdebconfclient0",
                    "liblocale-gettext-perl",
                    "libselinux1",
                    "libsemanage1",
                    "libsepol1",
                    "libslang2",
                    "passwd"
                ]
            },
            new Package()
            {
                Name = "adwaita-icon-theme",
                Dependencies =
                [
                    "libgdk-pixbuf2.0-0",
                    "libglib2.0-0",
                    "libgtk-3-0",
                    "librsvg2-2",
                    "librsvg2-common"
                ]
            },
            new Package()
            {
                Name = "test",
                Dependencies =
                [
                    "acl",
                    "accountsservice"
                ]
            },
        };

        var res = GraphDependencies.GraphDependencies.GetDependenciesRecursive(
            new RecursionParams(list, list[^1], 1)
        );
        
        const string expected = "\ttest [label=\"test\"];\n\tacl [label=\"acl\"];\n\ttest -> acl;\n\taccountsservice [label=\"accountsservice\"];\n\ttest -> accountsservice;\n";
        
        Assert.Equal(expected, res.ToString());
    }
}