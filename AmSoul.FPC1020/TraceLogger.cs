using System.Diagnostics;

namespace AmSoul.FPC1020;

public class TraceLogger : ILogger
{
    //private static readonly TraceLogger traceLogger;
    //public static TraceLogger Instance
    //{
    //    get { return traceLogger ?? new TraceLogger(); }
    //}

    public TraceLogger(TraceListener traceListener = null)
    {
        Trace.Listeners.Clear();
        if (traceListener == null)
            Trace.Listeners.Add(new TraceLoggerListener());
        else
            Trace.Listeners.Add(traceListener);
    }

    public void SendPacketInfo(object msg)
    {
        WriteLog(msg, "SendPacket");
    }
    public void SendDataPacketInfo(object msg)
    {
        WriteLog(msg, "SendDataPacket");
    }

    public void ReceivedPacketInfo(object msg)
    {
        WriteLog(msg, "ReceivedPacket");
    }

    public void ReceivedDataPacketInfo(object msg)
    {
        WriteLog(msg, "ReceivedDataPacket");
    }

    public void SuccessInfo(object msg)
    {
        WriteLog(msg, "Success");
    }

    public void ErrorInfo(object msg)
    {
        WriteLog(msg, "Error");
    }
    public void Info(object msg)
    {
        WriteLog(msg, "");
    }
    private void WriteLog(object message, string category)
    {
        Trace.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}, {message}", category);
        //Trace.WriteLine($"  ");
    }
}
