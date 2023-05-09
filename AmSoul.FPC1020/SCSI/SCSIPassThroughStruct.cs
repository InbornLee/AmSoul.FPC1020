using System.Runtime.InteropServices;

namespace AmSoul.FPC1020.SCSI;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SCSIPassThroughDirect
{
    public ushort Length;
    public byte SCSIStatus;
    public byte PathId;
    public byte TargetId;
    public byte Lun;
    public byte CdbLength;
    public byte SenseInfoLength;
    public byte DataIn;
    public uint DataTransferLength;
    public uint TimeOutValue;
    public IntPtr DataBufferOffset;
    public uint SenseInfoOffset;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] Cdb;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SCSIPassThroughDirectWithBuffers
{
    public SCSIPassThroughDirect Spt;
    public uint Filler;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65536)]
    public byte[] Buffer;
}
