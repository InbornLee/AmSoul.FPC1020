namespace AmSoul.FPC1020;

public interface ILogger
{
    void SendPacketInfo(object msg);
    void ReceivedPacketInfo(object msg);
    void ReceivedDataPacketInfo(object msg);
    void SuccessInfo(object msg);
    void ErrorInfo(object msg);
}
