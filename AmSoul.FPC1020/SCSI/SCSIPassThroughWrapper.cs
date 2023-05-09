using System.Runtime.InteropServices;

namespace AmSoul.FPC1020.SCSI;

public enum DataDirection : byte
{
    SCSI_IOCTL_DATA_OUT = 0,
    SCSI_IOCTL_DATA_IN = 1,
    SCSI_IOCTL_DATA_UNSPECIFIED = 2
}
public class SCSIPassThroughDirectWrapper
{
    private const byte CDB6GENERIC_LENGTH = 6;
    public SCSIPassThroughDirectWithBuffers sptdwb;

    public SCSIPassThroughDirectWrapper(byte[] cdb = null, byte[] data = null,
        DataDirection dataDirection = DataDirection.SCSI_IOCTL_DATA_UNSPECIFIED,
        uint dataTransferLength = 0,
        uint timeOut = 5)
    {
        sptdwb = new SCSIPassThroughDirectWithBuffers();
        sptdwb.Spt.Cdb = new byte[16];

        SetCdb(cdb);

        sptdwb.Spt.Length = (ushort)Marshal.SizeOf(sptdwb.Spt);
        sptdwb.Spt.PathId = 0;
        sptdwb.Spt.TargetId = 1;
        sptdwb.Spt.Lun = 0;
        sptdwb.Spt.CdbLength = CDB6GENERIC_LENGTH;
        sptdwb.Spt.TimeOutValue = timeOut;

        sptdwb.Spt.DataIn = (byte)dataDirection;
        SetDataLength(dataTransferLength);

        sptdwb.Spt.DataBufferOffset = new IntPtr(Marshal.SizeOf(this.sptdwb) - 65536);

        sptdwb.Spt.SenseInfoLength = 0;
        //sptdwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<ScsiPassThroughDirectWithBuffers>("ucSenseBuf").ToInt32();
        sptdwb.Spt.SenseInfoOffset = 48;

        sptdwb.Buffer = new byte[65536];
        Array.Copy(data, 0, sptdwb.Buffer, 0, data.Length);
    }

    public byte[] GetCdb() => sptdwb.Spt.Cdb;
    public byte[] GetCdb(int start, int length) => sptdwb.Spt.Cdb.ToList().GetRange(start, length).ToArray();
    public void SetCdb(byte[] cdb) => SetCdb(cdb, 0, 0, cdb.Length);
    public void SetCdb(byte[] cdb, int startSrc, int startDst, int length) => Array.Copy(cdb, startSrc, sptdwb.Spt.Cdb, startDst, length);
    public byte[] GetDataBuffer() => sptdwb.Buffer;
    public byte[] GetDataBuffer(int start, int count) => sptdwb.Buffer.ToList().GetRange(start, count).ToArray();
    public void SetDataLength(uint dataLength) => sptdwb.Spt.DataTransferLength = dataLength;
}
