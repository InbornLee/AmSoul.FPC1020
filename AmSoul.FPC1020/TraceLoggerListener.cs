using System.Diagnostics;

namespace AmSoul.FPC1020;

public class TraceLoggerListener : TraceListener
{
    private string loggerFileName;
    public TraceLoggerListener()
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);
        loggerFileName = basePath + $"Log-{DateTime.Now:yyyyMMdd}.txt";
    }

    public override void Write(string message)
    {
        OutputMessage(message, "");
    }
    public override void Write(object message)
    {
        OutputMessage(message, "");
    }

    public override void WriteLine(string message)
    {
        OutputMessage(message, "");
    }
    public override void WriteLine(object message)
    {
        OutputMessage(message, "");
    }

    public override void WriteLine(string message, string category)
    {
        OutputMessage(message, category);
    }
    public override void WriteLine(object message, string category)
    {
        OutputMessage(message, category);
    }
    private void OutputMessage(object message, string category)
    {
        Console.ForegroundColor = category switch
        {
            "SendPacket" => ConsoleColor.Blue,
            "SendDataPacket" => ConsoleColor.DarkBlue,
            "ReceivedPacket" => ConsoleColor.Cyan,
            "ReceivedDataPacket" => ConsoleColor.DarkCyan,
            "Success" => ConsoleColor.Green,
            "Error" => ConsoleColor.Red,
            _ => ConsoleColor.White,
        };
        //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{category} :");
        Console.WriteLine($"{message}");

    }
}
