using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace AmSoul.FPC1020.Utility;

internal static class Win32Native
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetDriveType(string driveinfo);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr SecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);
    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr SecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("Kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("Kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("Kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("Kernel32.dll")]
    public static extern uint GetLastError();

    [DllImport("msvcrt.dll")]
    public static extern IntPtr memcmp(
        byte[] b1,
        byte[] b2,
        IntPtr count);

    /// <summary>
    /// 用memcmp比较字节数组
    /// </summary>
    /// <param name="b1">字节数组1</param>
    /// <param name="b2">字节数组2</param>
    /// <returns>如果两个数组相同，返回0；如果数组1小于数组2，返回小于0的值；如果数组1大于数组2，返回大于0的值。</returns>
    public static int InvokeMemcmp(byte[] b1, byte[] b2, int count)
    {
        IntPtr retval = memcmp(b1, b2, new IntPtr(count));
        return retval.ToInt32();
    }
}
