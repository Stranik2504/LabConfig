using LabConfig;

namespace LabConfigTest;

public class UptimeTests
{
    private readonly DateTime _time;
    
    public UptimeTests()
    {
        _time = DateTime.Now;
        CommandManager.SetStartTime(_time);
    }
    
    [Fact]
    public void Test1()
    {
        var uptime = DateTime.Now - _time;
        var expected = DateTime.Now.ToString("HH:mm:ss") +
                       $" up {uptime.Seconds} sec, 1 user, load average: 0.00, 0.00, 0.00";

        var result = CommandManager.UptimeCommand("uptime");
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test2()
    {
        var uptime = DateTime.Now - _time;
        var expected = $"up {uptime.Seconds} sec";

        var result = CommandManager.UptimeCommand("uptime -p");
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test3()
    {
        var expected = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

        var result = CommandManager.UptimeCommand("uptime -s");
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test4()
    {
        var result = CommandManager.UptimeCommand("uptime -asd");
        
        Assert.Equal("Unknown uptime option: -asd", result);
    }
}

public class WhoTests
{
    private readonly DateTime _time;
    
    public WhoTests()
    {
        _time = DateTime.Now;
        CommandManager.SetStartTime(_time);
    }
    
    [Fact]
    public void Test1()
    {
        CommandManager.SetNameUser("me");
        
        var expected = $"me     custom-terminal             {_time:yyyy-MM-dd hh:mm} (:0)\n" +
                       $"me     custom-terminal-v2          {_time.AddMinutes(-5):yyyy-MM-dd hh:mm} (:0:0)";
        var result = CommandManager.WhoCommand("who");
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test2()
    {
        CommandManager.SetNameUser("me");
        
        var expected = $"me      custom-terminal             {_time:yyyy-MM-dd hh:mm} (:0)\n" +
                       $"me      custom-terminal-v2          {_time.AddMinutes(-5):yyyy-MM-dd hh:mm} (:0:0)\n" +
                       $"god     void                        {new DateTime(2077, 11, 9, 1, 1, 1):yyyy-MM-dd hh:mm} ";
        var result = CommandManager.WhoCommand("who -a");
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test3()
    {
        var result = CommandManager.UptimeCommand("who -asd");
        
        Assert.Equal("Unknown who option: -asd", result);
    }
}

public class WcTests
{
    [Fact]
    public void Test1()
    {
        const string text = "Hello, World!";
        const string expected = $"      2      0      13";
        
        var result = CommandManager.WcCommand(text);
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test2()
    {
        CommandManager.PathArchive = "archive.zip";
        CommandManager.CdCommand("/");
        
        const string expected = $"      2      1      18";
        var result = CommandManager.FileWcCommand("hm.txt");
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Test3()
    {
        CommandManager.PathArchive = "asd.zip";
        
        const string expected = $"File system not found: asd.zip";
        var result = CommandManager.FileWcCommand("hm.txt");
        
        Assert.Equal(expected, result);
    }
}

public class CdTests
{
    [Fact]
    public void Test1()
    {
        CommandManager.PathArchive = "asd.zip";

        const string expected = "File system not found: asd.zip";
        var result = CommandManager.CdCommand("asd");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Test2()
    {
        CommandManager.PathArchive = "archive.zip";
        CommandManager.CdCommand("/");

        const string expected = "No such file or directory: hms.txt";
        var result = CommandManager.CdCommand("hms.txt");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Test3()
    {
        CommandManager.PathArchive = "archive.zip";
        CommandManager.CdCommand("/");

        var result = CommandManager.CdCommand("folder_1");

        Assert.Empty(result);
    }
}

public class LsTests
{
    [Fact]
    public void Test1()
    {
        CommandManager.PathArchive = "asd.zip";

        const string expected = "File system not found: asd.zip";
        var result = CommandManager.LsCommand("ls");

        Assert.Equal(expected, result[0].name);
    }
    
    [Fact]
    public void Test2()
    {
        CommandManager.PathArchive = "archive.zip";
        CommandManager.CdCommand("/folder_2");
        
        var result = CommandManager.LsCommandString("ls");

        Assert.Equal("top_anime", result);
    }
    
    [Fact]
    public void Test3()
    {
        CommandManager.SetNameUser("me");
        CommandManager.PathArchive = "archive.zip";
        CommandManager.CdCommand("/folder_2");
        
        const string expected = "drwxr-xr-x 1 me me - Sep 29 23:24 top_anime";
        var result = CommandManager.LsCommandString("ls -l");

        Assert.Equal(expected, result);
    }
}

public class ExitTests
{
    [Fact]
    public void Test1()
    {
        var result = CommandManager.Execute("exit");

        Assert.Equal("", result);
    }
    
    [Fact]
    public void Test2()
    {
        var result = false;
        CommandManager.Close += () => { result = true; };
        
        CommandManager.Execute("exit");

        Assert.True(result);
    }
    
    [Fact]
    public void Test3()
    {
        var result = CommandManager.Execute("exit sadad");

        Assert.Equal("", result);
    }
}