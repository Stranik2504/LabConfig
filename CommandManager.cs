using System;

namespace LabConfig;

public static class CommandManager
{
    private const int CountUsers = 1;
    
    public static event Action Close;
    public static event Action<string> ChangeDir;

    private static DateTime _startTime;

    static CommandManager()
    {
        _startTime = DateTime.Now;
    }
    
    public static string Execute(string command)
    {
        if (command == "exit")
        {
            Close?.Invoke();
            return string.Empty;
        }
        
        if (command.StartsWith("uptime"))
        {
            
        }
        
        return "command not found: " + command;
    }

    public static string UptimeCommand(string command)
    {
        
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
        
    }
}