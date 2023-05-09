using AmSoul.FPC1020.SCSI;
using AmSoul.FPC1020.Utility;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AmSoul.FPC1020;

internal sealed class USBDeviceDriver
{
    private enum IoctlCodes
    {
        IOCTL_SCSI_PASS_THROUGH_DIRECT = 0x0004D004,
        IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808
    }

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;

    //private const byte CDB6GENERIC_LENGTH = 6;
    //private const byte CDB10GENERIC_LENGTH = 10;

    //private const uint METHOD_BUFFERED = 0;
    //private const uint IOCTL_SCSI_BASE = 0x00000004;
    //private const uint FILE_READ_ACCESS = 0x0001;
    //private const uint FILE_WRITE_ACCESS = 0x0002;
    //private const uint IOCTL_SCSI_PASS_THROUGH = ((IOCTL_SCSI_BASE) << 16) | ((FILE_READ_ACCESS | FILE_WRITE_ACCESS) << 14) | (0x0401 << 2) | (METHOD_BUFFERED);
    //private const uint IOCTL_SCSI_PASS_THROUGH_DIRECT = ((IOCTL_SCSI_BASE) << 16) | ((FILE_READ_ACCESS | FILE_WRITE_ACCESS) << 14) | (0x0405 << 2) | (METHOD_BUFFERED);
    private SafeFileHandle handle { get; set; }
    private uint lastError = 0;

    public CommandPacket CmdPacket { get; private set; }
    public CommandDataPacket CmdDataPacket { get; private set; }
    public ResponsePacket ReceivedPacket { get; private set; }
    public ResponseDataPacket ReceivedDataPacket { get; private set; }
    public TraceLogger TraceLogger { get; set; }

    public uint GetError() => lastError;

    public USBDeviceDriver(TraceListener traceListener = null)
    {
        TraceLogger = traceListener == null ? new() : new(traceListener);
    }

    public bool Connect(string driveName)
    {
        string path = @"\\.\" + driveName;
        if (path.Length - path.LastIndexOf(':') > 1)
            path = path.Remove(path.LastIndexOf(':') + 1);

        uint desiredAccess = GENERIC_READ | GENERIC_WRITE;
        uint shareMode = FILE_SHARE_READ | FILE_SHARE_WRITE;
        handle = Win32Native.CreateFile(path,
            desiredAccess,
            shareMode,
            IntPtr.Zero,
            (uint)FileMode.Open,
            0,//(uint)FileAttributes.Normal, 
            IntPtr.Zero);

        return !handle.IsInvalid;
    }
    public void Disconnect()
    {
        if (handle != null && !handle.IsClosed)
        {
            handle.Dispose();
            //Win32Native.InvokeCloseHandle(handle.DangerousGetHandle());
        }
    }
    public bool Ioctl(SCSIPassThroughDirectWrapper sptdw)
    {
        IntPtr bufferPointer = IntPtr.Zero;
        bool ioresult = false;

        try
        {
            DeviceIoOverlapped deviceIoOverlapped = new();
            ManualResetEvent hEvent = new(false);

            deviceIoOverlapped.ClearAndSetEvent(hEvent.SafeWaitHandle.DangerousGetHandle());

            var bufferSize = Marshal.SizeOf(sptdw.sptdwb);
            bufferPointer = Marshal.AllocHGlobal(bufferSize);
            Marshal.StructureToPtr(sptdw.sptdwb, bufferPointer, true);

            ioresult = Win32Native.DeviceIoControl(handle,
                (uint)IoctlCodes.IOCTL_SCSI_PASS_THROUGH_DIRECT,
                bufferPointer,
                (uint)bufferSize,
                bufferPointer,
                (uint)bufferSize,
                out uint bytesReturned,
                deviceIoOverlapped.GlobalOverlapped) && (bytesReturned > 0);

            if (ioresult)
            {
                sptdw.sptdwb = (SCSIPassThroughDirectWithBuffers)Marshal.PtrToStructure(bufferPointer, typeof(SCSIPassThroughDirectWithBuffers));
            }
            else
            {
                lastError = Win32Native.GetLastError();
            }
        }
        catch (Exception e)
        {
            TraceLogger.ErrorInfo($"IO Control FAIL: {e.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPointer);
        }
        return ioresult;
    }
    public CommandPacket InitCmdPacket(CommandCode cmdCode, byte[] data = null, byte srcDeviceID = 0, byte dstDeviceID = 0)
    {
        CommandPacket packet = new()
        {
            Prefix = Convert.ToUInt16(PacketCode.CMD_PREFIX_CODE),
            SrcDeviceID = srcDeviceID,
            DstDeviceID = dstDeviceID,
            CMDCode = Convert.ToUInt16(cmdCode),
            Data = new byte[16],
            DataLen = (ushort)(data != null ? data.Length : 0)
        };
        if (data != null && data.Length > 0)
            Array.Copy(data, packet.Data, data.Length);

        var bytePacket = StructureHelper.StructToBytes(packet);
        ushort checkSum = 0;
        for (var i = 0; i < Marshal.SizeOf(packet) - 2; i++)
            checkSum += bytePacket[i];

        packet.CheckSum = checkSum;
        CmdPacket = packet;
        return packet;
    }
    public CommandDataPacket InitCmdDataPacket(CommandCode cmdCode, byte[] data = null, byte srcDeviceID = 0, byte dstDeviceID = 0)
    {
        CommandDataPacket packet = new()
        {
            Prefix = Convert.ToUInt16(PacketCode.CMD_DATA_PREFIX_CODE),
            SrcDeviceID = srcDeviceID,
            DstDeviceID = dstDeviceID,
            CMDCode = Convert.ToUInt16(cmdCode),
            Data = new byte[500],
            DataLen = (ushort)(data != null ? data.Length : 0)
        };
        if (data != null && data.Length > 0)
            Array.Copy(data, packet.Data, data.Length);

        var bytePacket = StructureHelper.StructToBytes(packet);
        ushort checkSum = 0;
        for (var i = 0; i < packet.DataLen + 8; i++)
            checkSum += bytePacket[i];

        packet.CheckSum = checkSum;
        CmdDataPacket = packet;
        return packet;
    }
    public ErrorCode SendCmdPacket(CommandPacket packet)
    {
        if (handle == null || handle.IsInvalid) return ErrorCode.ERR_CONNECTION;
        var bytePacket = StructureHelper.StructToBytes(packet);
        byte[] btCDB = new byte[8];
        btCDB[0] = 0xEF;
        btCDB[1] = 0x11;
        btCDB[4] = (byte)bytePacket.Length;

        SCSIPassThroughDirectWrapper sptdw = new(btCDB, bytePacket, DataDirection.SCSI_IOCTL_DATA_OUT, (uint)bytePacket.Length);
        if (!Ioctl(sptdw)) return ErrorCode.FAIL;
        //TraceLogger.SendPacketInfo($"{StructureHelper.BytesToHexStr(sptdw.GetDataBuffer(), 26)}");

        return ReceiveCmdAck(packet);
    }
    public ErrorCode SendDataPacket(CommandDataPacket packet)
    {
        if (handle == null || handle.IsInvalid) return ErrorCode.ERR_CONNECTION;
        var bytePacket = StructureHelper.StructToBytes(packet);
        //Array.Resize(ref bytePacket, 510);
        bytePacket[packet.DataLen + 8] = StructureHelper.LOWBYTE(packet.CheckSum);
        bytePacket[packet.DataLen + 9] = StructureHelper.HIGHBYTE(packet.CheckSum);
        byte[] btCDB = new byte[8];
        btCDB[0] = 0xEF;
        btCDB[1] = 0x13;
        btCDB[4] = StructureHelper.LOWBYTE((ushort)bytePacket.Length);
        btCDB[5] = StructureHelper.HIGHBYTE((ushort)bytePacket.Length);

        SCSIPassThroughDirectWrapper sptdw = new(btCDB, bytePacket, DataDirection.SCSI_IOCTL_DATA_OUT, (uint)bytePacket.Length);
        if (!Ioctl(sptdw)) return ErrorCode.FAIL;
        //TraceLogger.SendDataPacketInfo($"{StructureHelper.BytesToHexStr(sptdw.GetDataBuffer(), 26)}");

        return ReceiveDataAck(packet);
    }
    public ErrorCode ReceiveCmdAck(CommandPacket packet)
    {
        if (handle == null || handle.IsInvalid) return ErrorCode.ERR_CONNECTION;
        var cmdPacket = StructureHelper.StructToBytes(packet);

        byte[] btCDB = new byte[8];
        btCDB[0] = 0xEF;
        btCDB[1] = 0x12;

        byte[] receivedPacket;
        byte[] waitPacket = new byte[65536];
        StructureHelper.MemSet(waitPacket, 0xAF, cmdPacket.Length);
        ushort timeOut = 5;

        if (packet.CMDCode == (ushort)CommandCode.CMD_TEST_CONNECTION)
            timeOut = 3;
        if (packet.CMDCode == (ushort)CommandCode.CMD_ADJUST_SENSOR)
            timeOut = 30;

        int readCount = GetReadWaitTime(packet.CMDCode);
        int count = 0;
        SCSIPassThroughDirectWrapper sptw = new(btCDB, cmdPacket, DataDirection.SCSI_IOCTL_DATA_IN, (uint)cmdPacket.Length, timeOut);
        do
        {
            if (!Ioctl(sptw)) return ErrorCode.FAIL;
            receivedPacket = sptw.GetDataBuffer();
            Thread.Sleep(50);

            count++;
            if (count > readCount) return ErrorCode.FAIL;
        }
        while (Win32Native.InvokeMemcmp(receivedPacket, waitPacket, 8) == 0);

        //TraceLogger.ReceivedPacketInfo($"{StructureHelper.BytesToHexStr(receivedPacket, 26)}");

        return CheckReceive(packet, receivedPacket);
    }
    public ErrorCode ReceiveDataAck(CommandDataPacket packet)
    {
        if (handle == null || handle.IsInvalid) return ErrorCode.ERR_CONNECTION;
        byte[] cmdPacket = StructureHelper.StructToBytes(packet);

        byte[] btCDB = new byte[8];
        btCDB[0] = 0xEF;
        btCDB[1] = 0x15;

        byte[] receivedPacket;
        byte[] waitPacket = new byte[10];
        StructureHelper.MemSet(waitPacket, 0xAF, 10);
        ushort timeOut = 5000;

        SCSIPassThroughDirectWrapper sptw = new(btCDB, cmdPacket, DataDirection.SCSI_IOCTL_DATA_IN, 8, timeOut);
        do
        {
            if (!Ioctl(sptw)) return ErrorCode.FAIL;
            receivedPacket = sptw.GetDataBuffer();
            Thread.Sleep(40);
        }
        while (Win32Native.InvokeMemcmp(receivedPacket, waitPacket, 8) == 0);

        if (!ReceiveRawData(receivedPacket)) return ErrorCode.FAIL;

        //TraceLogger.ReceivedDataPacketInfo($"{StructureHelper.BytesToHexStr(receivedPacket, 53)}");
        return CheckDataReceive(packet, receivedPacket);
    }
    public ErrorCode ReceiveImage(ref byte[] buffer, uint dataLen, byte type)
    {
        if (handle == null || handle.IsInvalid) return ErrorCode.ERR_CONNECTION;
        byte[] btCDB = new byte[8];
        btCDB[0] = 0xEF;
        btCDB[1] = 0x16;
        btCDB[2] = type;

        //byte[] waitPacket = new byte[8];
        //StructureHelper.MemSet(waitPacket, 0xAF, 8);
        SCSIPassThroughDirectWrapper sptw = new(btCDB, buffer);
        if (dataLen < 64 * 1024)
        {
            sptw = new(btCDB, buffer, DataDirection.SCSI_IOCTL_DATA_IN, dataLen, 5);
            if (!Ioctl(sptw)) return ErrorCode.FAIL;
        }
        else if (dataLen == 256 * 288)
        {
            btCDB[2] = 0x00;
            sptw = new(btCDB, new byte[1] { buffer[0] }, DataDirection.SCSI_IOCTL_DATA_IN, dataLen / 2, 5);
            if (!Ioctl(sptw)) return ErrorCode.FAIL;

            btCDB[2] = 0x01;
            sptw = new(btCDB, new byte[1] { buffer[dataLen] }, DataDirection.SCSI_IOCTL_DATA_IN, dataLen / 2, 5);
            if (!Ioctl(sptw)) return ErrorCode.FAIL;
        }
        buffer = sptw.GetDataBuffer();
        return ErrorCode.SUCCESS;
    }
    public ErrorCode CheckReceive(CommandPacket cmdPacket, byte[] receivedByte)
    {
        var receivedPacket = (ResponsePacket)StructureHelper.BytesToStruct(receivedByte, typeof(ResponsePacket));

        if (receivedPacket.Prefix != (ushort)PacketCode.RCM_PREFIX_CODE) return ErrorCode.ERR_PREFIX_CODE;

        int calcCheckSum = 0;
        for (var i = 0; i < Marshal.SizeOf(receivedPacket) - 2; i++)
            calcCheckSum += receivedByte[i];
        //Check checkSum
        if (receivedPacket.CheckSum != calcCheckSum) return ErrorCode.ERR_CHECKSUM;
        //Check CMDCode
        if (cmdPacket.CMDCode != receivedPacket.ResponseCmdCode) return ErrorCode.FAIL;
        //if (receivedPacket.ResultCode == 0)
        //    TraceLogger.SuccessInfo("Check Success!");
        //else
        //    TraceLogger.ErrorInfo("Check Fail");
        ReceivedPacket = receivedPacket;
        return receivedPacket.ResultCode == 0
            ? ErrorCode.SUCCESS
            : ErrorCode.FAIL;
    }
    public ErrorCode CheckDataReceive(CommandDataPacket cmdPacket, byte[] receivedByte)
    {
        var receivedDataPacket = (ResponseDataPacket)StructureHelper.BytesToStruct(receivedByte, typeof(ResponseDataPacket));

        if (receivedDataPacket.Prefix != (ushort)PacketCode.RCM_DATA_PREFIX_CODE) return ErrorCode.ERR_PREFIX_CODE;

        var low = receivedDataPacket.Data[receivedDataPacket.DataLen - 2];
        var high = receivedDataPacket.Data[receivedDataPacket.DataLen - 1];
        var CheckSum = StructureHelper.MakeWord(low, high);
        int calcCheckSum = 0;
        for (var i = 0; i < receivedDataPacket.DataLen + 8; i++)
            calcCheckSum += receivedByte[i];
        //Check checkSum
        if (CheckSum != calcCheckSum) return ErrorCode.ERR_CHECKSUM;
        //Check CMDCode
        if (cmdPacket.CMDCode != receivedDataPacket.ResponseCmdCode) return ErrorCode.FAIL;
        //if (receivedDataPacket.ResultCode == 0)
        //    TraceLogger.SuccessInfo("Check Success!");
        //else
        //    TraceLogger.ErrorInfo("Check Fail");
        ReceivedDataPacket = receivedDataPacket;
        return receivedDataPacket.ResultCode == 0
            ? ErrorCode.SUCCESS
            : ErrorCode.FAIL;
    }
    private bool ReceiveRawData(byte[] buffer)
    {
        byte[] btCDB = new byte[8];
        btCDB[0] = 0xEF;
        btCDB[1] = 0x14;

        SCSIPassThroughDirectWrapper sptw = new(btCDB, buffer, DataDirection.SCSI_IOCTL_DATA_IN, (uint)buffer.Length, 5);
        if (!Ioctl(sptw)) return false;
        var raw = sptw.GetDataBuffer();
        Buffer.BlockCopy(raw, 0, buffer, 8, raw.Length - 8);
        return true;
    }
    private static ushort GetReadWaitTime(ushort cmdCode)
    {
        ushort time = cmdCode switch
        {
            (ushort)CommandCode.CMD_ADJUST_SENSOR => 0xFFFF,
            _ => 150,
        };
        return time;
    }
}
