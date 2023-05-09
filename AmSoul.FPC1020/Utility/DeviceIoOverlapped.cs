using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AmSoul.FPC1020.Utility;

internal class DeviceIoOverlapped
{
    private IntPtr mPtrOverlapped = IntPtr.Zero;

    private int mFieldOffset_InternalLow = 0;
    private int mFieldOffset_InternalHigh = 0;
    private int mFieldOffset_OffsetLow = 0;
    private int mFieldOffset_OffsetHigh = 0;
    private int mFieldOffset_EventHandle = 0;

    public DeviceIoOverlapped()
    {
        // Globally allocate the memory for the overlapped structure
        mPtrOverlapped = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeOverlapped)));

        // Find the structural starting positions in the NativeOverlapped structure.
        mFieldOffset_InternalLow = Marshal.OffsetOf(typeof(NativeOverlapped), "InternalLow").ToInt32();
        mFieldOffset_InternalHigh = Marshal.OffsetOf(typeof(NativeOverlapped), "InternalHigh").ToInt32();
        mFieldOffset_OffsetLow = Marshal.OffsetOf(typeof(NativeOverlapped), "OffsetLow").ToInt32();
        mFieldOffset_OffsetHigh = Marshal.OffsetOf(typeof(NativeOverlapped), "OffsetHigh").ToInt32();
        mFieldOffset_EventHandle = Marshal.OffsetOf(typeof(NativeOverlapped), "EventHandle").ToInt32();
    }

    public IntPtr InternalLow
    {
        get => Marshal.ReadIntPtr(mPtrOverlapped, mFieldOffset_InternalLow);
        set => Marshal.WriteIntPtr(mPtrOverlapped, mFieldOffset_InternalLow, value);
    }

    public IntPtr InternalHigh
    {
        get => Marshal.ReadIntPtr(mPtrOverlapped, mFieldOffset_InternalHigh);
        set => Marshal.WriteIntPtr(mPtrOverlapped, mFieldOffset_InternalHigh, value);
    }

    public int OffsetLow
    {
        get => Marshal.ReadInt32(mPtrOverlapped, mFieldOffset_OffsetLow);
        set => Marshal.WriteInt32(mPtrOverlapped, mFieldOffset_OffsetLow, value);
    }

    public int OffsetHigh
    {
        get => Marshal.ReadInt32(mPtrOverlapped, mFieldOffset_OffsetHigh);
        set => Marshal.WriteInt32(mPtrOverlapped, mFieldOffset_OffsetHigh, value);
    }

    /// <summary>
    /// The overlapped event wait handle.
    /// </summary>
    public IntPtr EventHandle
    {
        get => Marshal.ReadIntPtr(mPtrOverlapped, mFieldOffset_EventHandle);
        set => Marshal.WriteIntPtr(mPtrOverlapped, mFieldOffset_EventHandle, value);
    }

    /// <summary>
    /// Pass this into the DeviceIoControl and GetOverlappedResult APIs
    /// </summary>
    public IntPtr GlobalOverlapped => mPtrOverlapped;

    /// <summary>
    /// Set the overlapped wait handle and clear out the rest of the structure.
    /// </summary>
    /// <param name="hEventOverlapped"></param>
    public void ClearAndSetEvent(IntPtr hEventOverlapped)
    {
        EventHandle = hEventOverlapped;
        InternalLow = IntPtr.Zero;
        InternalHigh = IntPtr.Zero;
        OffsetLow = 0;
        OffsetHigh = 0;
    }

    // Clean up the globally allocated memory.
    ~DeviceIoOverlapped()
    {
        if (mPtrOverlapped != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(mPtrOverlapped);
            mPtrOverlapped = IntPtr.Zero;
        }
    }
}
