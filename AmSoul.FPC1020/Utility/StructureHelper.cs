using System.Drawing;
using System.Runtime.InteropServices;

namespace AmSoul.FPC1020.Utility;

public class StructureHelper
{
    /// <summary>
    /// 结构体类型转换为byte[]
    /// </summary>
    /// <param name="structObj"></param>
    /// <returns></returns>
    public static byte[] StructToBytes(object structObj)
    {
        int size = Marshal.SizeOf(structObj);
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(structObj, buffer, false);
            byte[] bytes = new byte[size];
            Marshal.Copy(buffer, bytes, 0, size);
            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
    /// <summary>
    /// byte[]转换为结构体类型
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object BytesToStruct(byte[] bytes, Type type)
    {
        int size = Marshal.SizeOf(type);
        if (size > bytes.Length) return null;
        //分配结构体内存空间
        IntPtr structPtr = Marshal.AllocHGlobal(size);
        //将byte数组拷贝到分配好的内存空间
        Marshal.Copy(bytes, 0, structPtr, size);
        //将内存空间转换为目标结构体
        object obj = Marshal.PtrToStructure(structPtr, type);
        //释放内存空间
        Marshal.FreeHGlobal(structPtr);
        return obj;
    }
    /// <summary>
    /// byte[]转换为Intptr
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static IntPtr BytesToIntptr(byte[] bytes)
    {
        int size = bytes.Length;
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, 0, buffer, size);
            return buffer;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
    /// <summary> 
    /// 字节数组转换为16进制字符串 
    /// </summary> 
    /// <param name="bytes"></param> 
    /// <returns></returns> 
    public static string BytesToHexStr(byte[] bytes, int length = 0)
    {
        string returnStr = "";
        int datalength = length == 0 ? bytes.Length : length;
        if (bytes != null)
        {
            for (int i = 0; i < datalength; i++)
            {
                returnStr += string.Format("{0:X2} ", bytes[i]);
            }
        }
        return returnStr;
    }
    /// <summary>
    /// 16进制的字符串转换为byte[]
    /// </summary>
    /// <param name="hexString"></param>
    /// <returns></returns>
    public static byte[] HexStrToBytes(string hexString)
    {
        hexString = hexString.Replace(" ", "");
        if ((hexString.Length % 2) != 0)
            hexString += " ";
        byte[] returnBytes = new byte[hexString.Length / 2];
        for (int i = 0; i < returnBytes.Length; i++)
            returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        return returnBytes;
    }
    /// <summary>
    /// 字节数组转换为8bit Bitmap
    /// </summary>
    /// <param name="rawValues"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static Bitmap BytesTo8bitBitmap(byte[] rawValues, int width, int height)
    {
        Bitmap bmp = new(width, height, PixelFormat.Format8bppIndexed);
        BitmapData bmpData = bmp.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format8bppIndexed);

        //获取图像参数
        int stride = bmpData.Stride;  // 扫描线的宽度
        int offset = stride - width;  // 显示宽度与扫描线宽度的间隙
        IntPtr iptr = bmpData.Scan0;  // 获取bmpData的内存起始位置
        int scanBytes = stride * height;  // 用stride宽度，表示这是内存区域的大小

        //把原始显示大小字节数组转换为内存中实际存放的字节数组
        int posScan = 0, posReal = 0;  // 分别设置两个位置指针，指向源数组和目标数组
        byte[] pixelValues = new byte[scanBytes];  //为目标数组分配内存

        for (int x = 0; x < height; x++)
        {
            //下面的循环节是模拟行扫描
            for (int y = 0; y < width; y++)
            {
                pixelValues[posScan++] = rawValues[posReal++];
            }
            posScan += offset;  //行扫描结束，要将目标位置指针移过“间隙”
        }

        //将内存字节数组复制到BitmapData中
        Marshal.Copy(pixelValues, 0, iptr, scanBytes);
        bmp.UnlockBits(bmpData);  // 解锁内存区域

        //修改生成位图的索引表，从伪彩修改为灰度
        ColorPalette tempPalette;
        using (Bitmap tempBmp = new(1, 1, format: PixelFormat.Format8bppIndexed))
        {
            tempPalette = tempBmp.Palette;
        }
        for (int i = 0; i < 256; i++)
        {
            tempPalette.Entries[i] = Color.FromArgb(i, i, i);
        }
        bmp.Palette = tempPalette;
        return bmp;
    }
    /// <summary>
    /// MemSet
    /// </summary>
    /// <param name="array"></param>
    /// <param name="value"></param>
    /// <param name="setLen"></param>
    public static void MemSet(byte[] array, byte value, int setLen = 0)
    {
        if (array != null)
        {
            int block = 32, index = 0;
            int length = Math.Min(block, array.Length);
            if (setLen != 0)
                length = setLen;

            //Fill the initial array
            while (index < length)
            {
                array[index++] = value;
            }

            //length = array.Length;
            //while (index < length)
            //{
            //    Buffer.BlockCopy(array, 0, array, index, Math.Min(block, length - index));
            //    index += block;
            //    block *= 2;
            //}
        }
        else
        {
            throw new ArgumentNullException(nameof(array));
        }
    }
    public static uint MakeWord(byte low, byte high)
    {
        return ((uint)high << 8) | low;
    }
    public static byte LOWBYTE(ushort value)
    {
        return (byte)(value & 0xFF);
    }
    public static byte HIGHBYTE(ushort value)
    {
        return (byte)(value >> 8);
    }
    public static string BitmapToBase64(Bitmap bmp)
    {
        try
        {
            using MemoryStream ms = new();
            bmp.Save(ms, format: ImageFormat.Jpeg);
            byte[] arr = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();
            string strbaser64 = Convert.ToBase64String(arr);
            return strbaser64;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
