using AmSoul.FPC1020.Utility;
using System.Diagnostics;

namespace AmSoul.FPC1020;

public sealed class FPC1020
{
    private readonly USBDeviceDriver usb;
    public bool IsConnected { get; private set; }
    public DeviceInfo DeviceInfo { get; private set; }
    public DeviceParam DeviceParam { get; set; }
    public TraceLogger TraceLogger { get; set; }

    public FPC1020(TraceListener traceListener = null)
    {
        usb = traceListener == null ? new() : new(traceListener);
        DeviceInfo = new DeviceInfo();
        DeviceParam = new DeviceParam();
        TraceLogger = usb.TraceLogger;
    }
    public bool Connect(string drive = null)
    {
        string[] drives = string.IsNullOrEmpty(drive) ? Environment.GetLogicalDrives() : new string[] { drive };

        foreach (string driveStr in drives)
        {
            DeviceInfo.DriveLetter = driveStr;
            var driveType = Win32Native.GetDriveType(driveStr);
            DeviceInfo.DriveType = Enum.GetName(typeof(DriveCode), driveType);
            if (driveType != (int)DriveCode.DRIVE_REMOVABLE && driveType != (int)DriveCode.DRIVE_CDROM) continue;
            TraceLogger.Info($"Connecting device: {driveStr} , {(DriveCode)driveType}");
            if (!usb.Connect(driveStr)) continue;
            if (!RunTestConnection()) continue;
            RunGetDeviceInfo();
            RunGetModuleSN();
            RunGetParams();
            IsConnected = true;
            return true;
        }
        return false;
    }

    public void Disconnect()
    {
        usb.Disconnect();
        IsConnected = false;
        DeviceInfo = new DeviceInfo();
        DeviceParam = new DeviceParam();
        TraceLogger.Info($"Disconnect");
    }
    //public bool Execute(ScsiCommandCode code) => this.usb.Ioctl(this.Commands[code].Sptw);

    /// <summary>
    /// 连接测试（CMD_TEST_CONNECTION 0x0001）
    /// </summary>
    /// <returns></returns>
    public bool RunTestConnection()
    {
        var packet = usb.InitCmdPacket(CommandCode.CMD_TEST_CONNECTION);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"Connecting Fail! {result}");
            return false;
        }
        else
        {
            TraceLogger.SuccessInfo("Connecting Success!");
            return true;
        }
    }
    /// <summary>
    /// 设置参数（CMD_SET_PARAM 0x0002）
    /// </summary>
    /// <param name="code"></param>
    /// <param name="value"></param>
    public void RunSetParam(ParamCode code, byte value)
    {
        var param = new byte[] { (byte)code, value };
        var packet = usb.InitCmdPacket(CommandCode.CMD_SET_PARAM, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
            TraceLogger.ErrorInfo($"{result}");
        else
            TraceLogger.SuccessInfo("Set Param Success!");
    }
    /// <summary>
    /// 读取参数（CMD_GET_PARAM 0x0003）
    /// </summary>
    /// <param name="code"></param>
    public void RunGetParam(ParamCode code)
    {
        var param = new byte[] { (byte)code };
        var packet = usb.InitCmdPacket(CommandCode.CMD_GET_PARAM, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
            TraceLogger.ErrorInfo($"{result}");
        else
            TraceLogger.SuccessInfo("Get Param Success!");
    }
    /// <summary>
    /// 读取所有参数
    /// </summary>
    public void RunGetParams()
    {
        for (int i = 0; i < 5; i++)
        {
            var param = new byte[] { (byte)i };
            var packet = usb.InitCmdPacket(CommandCode.CMD_GET_PARAM, param);
            var result = usb.SendCmdPacket(packet);
            if (result != ErrorCode.SUCCESS)
            {
                TraceLogger.ErrorInfo($"{result}");
                continue;
            }
            var paramName = Enum.GetName(typeof(ParamCode), i);
            DeviceParam.GetType().GetProperty(paramName).SetValue(DeviceParam, usb.ReceivedPacket.Data[0]);
            TraceLogger.SuccessInfo($"Get Param [{paramName}] Success! Value={usb.ReceivedPacket.Data[0]}");
        }
    }
    /// <summary>
    /// 读取设备信息（CMD_DEVICE_INFO 0x0004）
    /// </summary>
    public void RunGetDeviceInfo()
    {
        CommandPacket packet = usb.InitCmdPacket(CommandCode.CMD_GET_DEVICE_INFO);
        CommandDataPacket datapacket = usb.InitCmdDataPacket(CommandCode.CMD_GET_DEVICE_INFO);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
            return;
        }
        result = usb.ReceiveDataAck(datapacket);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
            return;
        }
        System.Text.ASCIIEncoding asciiEncoding = new();
        DeviceInfo.Name = asciiEncoding.GetString(usb.ReceivedDataPacket.Data, 0, usb.ReceivedDataPacket.DataLen - 2);
        TraceLogger.SuccessInfo("Get DeviceInfo Success!");
    }
    /// <summary>
    /// 检测手指（CMD_FINGER_DETECT 0x0021）
    /// </summary>
    public bool RunFingerDetect(bool isTouched)
    {
        var code = isTouched ? 0x01 : 0x00;
        CommandPacket packet = usb.InitCmdPacket(CommandCode.CMD_FINGER_DETECT);
        do
        {
            //TraceLogger.ErrorInfo($"请放手指!!!");
            usb.SendCmdPacket(packet);
            Thread.Sleep(500);
            //if (usb.ReceivedPacket.Data[0] != 0x01)
            //    TraceLogger.ErrorInfo("没有检测到手指!");
        } while (usb.ReceivedPacket.Data[0] != code);
        //TraceLogger.SuccessInfo("Finger Detect!");
        return true;
    }
    /// <summary>
    /// 采集指纹图像（CMD_GET_IMAGE 0x0020）
    /// </summary>
    public void RunGetImage()
    {
        var packet = usb.InitCmdPacket(CommandCode.CMD_GET_IMAGE);
        ErrorCode result;
        do
        {
            //TraceLogger.ErrorInfo($"请放手指!!!");
            result = usb.SendCmdPacket(packet);
            Thread.Sleep(500);
            if (result != ErrorCode.SUCCESS)
                TraceLogger.ErrorInfo("没有采集到图像!");
        } while (result != ErrorCode.SUCCESS);
        TraceLogger.SuccessInfo("图像采集成功!");
    }
    /// <summary>
    /// 从暂存在ImageBuffer中的指纹图像产生模板（CMD_GENERATE 0x0060）
    /// </summary>
    /// <param name="rambufferId">缓存ID</param>
    public ErrorCode RunGenerate(ushort rambufferId)
    {
        if (rambufferId is < 0 or > 2) return ErrorCode.ERR_INVALID_BUFFER_ID;
        var param = new byte[2] { StructureHelper.LOWBYTE(rambufferId), StructureHelper.HIGHBYTE(rambufferId) };
        var packet = usb.InitCmdPacket(CommandCode.CMD_GENERATE, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
            TraceLogger.ErrorInfo($"{result}");
        else
            TraceLogger.SuccessInfo($"图像生成模板成功，位于RamBuffer[{rambufferId}]!");
        return result;
    }
    /// <summary>
    /// 上传指纹图像到主机（CMD_UP_IMAGE_CODE 0x0022）
    /// </summary>
    /// <param name="imageType">Image Type为0发送全图;为1则发送1/4图像</param>
    public ErrorCode RunUpImage(byte imageType, out string base64)
    {
        base64 = "";
        uint width, height, size;
        var param = new byte[] { imageType };
        var packet = usb.InitCmdPacket(CommandCode.CMD_UP_IMAGE, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
            return result;
        }
        else
        {
            byte[] rcmdata = usb.ReceivedPacket.Data;
            var data = new byte[65536];
            width = StructureHelper.MakeWord(rcmdata[0], rcmdata[1]);
            height = StructureHelper.MakeWord(rcmdata[2], rcmdata[3]);
            size = width * height;
            usb.ReceiveImage(ref data, size, imageType);
            var bmp = StructureHelper.BytesTo8bitBitmap(data, (int)width, (int)height);
            base64 = StructureHelper.BitmapToBase64(bmp);
            //Graphics g = Graphics.FromHwnd(Win32Native.InvokeGetConsoleWindow());
            //g.DrawImage(bmp, new Rectangle(300, 300, bmp.Width, bmp.Height));
            TraceLogger.SuccessInfo("上传图像成功!");
            return ErrorCode.SUCCESS;
        }
    }
    /// <summary>
    /// 将暂存在RamBuffer中的指纹模板上传到主机（CMD_UP_CHAR 0x0042）
    /// </summary>
    /// <param name="rambuffer"></param>
    public ErrorCode RunUpChar(ushort rambufferId, out byte[] data)
    {
        data = null;
        if (rambufferId is < 0 or > 2) return ErrorCode.ERR_INVALID_BUFFER_ID;
        var param = new byte[2] { StructureHelper.LOWBYTE(rambufferId), StructureHelper.HIGHBYTE(rambufferId) };
        var packet = usb.InitCmdPacket(CommandCode.CMD_UP_CHAR, param);
        var datapacket = usb.InitCmdDataPacket(CommandCode.CMD_UP_CHAR, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
            TraceLogger.ErrorInfo($"{result}");
        else
        {
            usb.ReceiveDataAck(datapacket);
            TraceLogger.SuccessInfo("上传指纹模板成功!");
            data = usb.ReceivedDataPacket.Data;
        }
        return result;
    }
    /// <summary>
    /// 下载指纹模板数据到模块指定的RamBuffer（CMD_DOWN_CHAR 0x0043）
    /// </summary>
    /// <param name="rambufferId"></param>
    /// <param name="data">data长度需为498</param>
    /// <returns></returns>
    public ErrorCode RunDownloadChar(ushort rambufferId, byte[] data)
    {
        if (rambufferId is < 0 or > 2) return ErrorCode.ERR_INVALID_BUFFER_ID;
        if (data.Length != 500) return ErrorCode.ERR_INVALID_TMPL_DATA;
        var param = new byte[2] {
            StructureHelper.LOWBYTE((ushort)data.Length),
            StructureHelper.HIGHBYTE((ushort)data.Length)
        };
        var packet = usb.InitCmdPacket(CommandCode.CMD_DOWN_CHAR, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
        }
        else
        {
            var dataparam = new byte[500];
            dataparam[0] = StructureHelper.LOWBYTE(rambufferId);
            dataparam[1] = StructureHelper.HIGHBYTE(rambufferId);
            Array.Copy(data, 0, dataparam, 2, data.Length < 498 ? data.Length : 498);

            var datapacket = usb.InitCmdDataPacket(CommandCode.CMD_DOWN_CHAR, dataparam);
            result = usb.SendDataPacket(datapacket);
            if (result != ErrorCode.SUCCESS)
            {
                TraceLogger.ErrorInfo($"{result}");
            }
            else
            {
                TraceLogger.SuccessInfo($"Download Char to Rambuffer[{rambufferId}] Success!");

            }
        }
        return result;
    }
    /// <summary>
    /// 合成指纹模板数据用于入库（CMD_MERGE 0x0061）
    /// </summary>
    /// <param name="num">合成个数为 2或3</param>
    public ErrorCode RunMerge(ushort rambufferId, byte num)
    {
        if (num is < 2 or > 3) return ErrorCode.ERR_GEN_COUNT;
        var param = new byte[3] { StructureHelper.LOWBYTE(rambufferId), StructureHelper.HIGHBYTE(rambufferId), num };
        var packet = usb.InitCmdPacket(CommandCode.CMD_MERGE, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
            TraceLogger.ErrorInfo($"{result}");
        else
            TraceLogger.SuccessInfo("合成指纹模板成功!");
        return result;
    }
    /// <summary>
    /// 指定2个RamBuffer之间的模板做比对 （CMD_MATCH 0x0062）
    /// </summary>
    /// <param name="rambufferId0"></param>
    /// <param name="rambufferId1"></param>
    public ErrorCode RunMatch(ushort rambufferId0, ushort rambufferId1)
    {
        var param = new byte[4]
        {
            StructureHelper.LOWBYTE(rambufferId0),
            StructureHelper.HIGHBYTE(rambufferId0),
            StructureHelper.LOWBYTE(rambufferId1),
            StructureHelper.HIGHBYTE(rambufferId1),
        };
        var packet = usb.InitCmdPacket(CommandCode.CMD_MATCH, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
            return ErrorCode.ERR_MERGE_FAIL;
        }
        TraceLogger.SuccessInfo("Match Success!");
        return result;
    }
    /// <summary>
    /// 设置模块序列号（CMD_SET_MODULE_SN 0x0008）
    /// </summary>
    /// <param name="sncode">序列号最大长度为16位</param>
    /// <returns></returns>
    public ErrorCode SetModuleSN(string sncode)
    {
        var len = sncode.Length > 16 ? 16 : sncode.Length;
        byte[] data = System.Text.Encoding.Default.GetBytes(sncode, 0, len);
        var param = new byte[2] {
            StructureHelper.LOWBYTE(16),
            StructureHelper.HIGHBYTE(16)
        };
        var packet = usb.InitCmdPacket(CommandCode.CMD_SET_MODULE_SN, param);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
        }
        else
        {
            var dataparam = new byte[16];
            Array.Copy(data, 0, dataparam, 0, data.Length < 16 ? data.Length : 16);

            var datapacket = usb.InitCmdDataPacket(CommandCode.CMD_SET_MODULE_SN, dataparam);
            result = usb.SendDataPacket(datapacket);
            if (result != ErrorCode.SUCCESS)
            {
                TraceLogger.ErrorInfo($"{result}");
            }
            else
            {
                TraceLogger.SuccessInfo($"Set Module SN Success!");

            }
        }
        return result;
    }
    /// <summary>
    /// 获取模块序列号（CMD_GET_MODULE_SN 0x0009）
    /// </summary>
    public void RunGetModuleSN()
    {
        var packet = usb.InitCmdPacket(CommandCode.CMD_GET_MODULE_SN);
        var datapacket = usb.InitCmdDataPacket(CommandCode.CMD_GET_MODULE_SN);
        var result = usb.SendCmdPacket(packet);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
            return;
        }
        result = usb.ReceiveDataAck(datapacket);
        if (result != ErrorCode.SUCCESS)
        {
            TraceLogger.ErrorInfo($"{result}");
            return;
        }
        System.Text.ASCIIEncoding asciiEncoding = new();
        DeviceInfo.ModuleSN = asciiEncoding.GetString(usb.ReceivedDataPacket.Data, 0, usb.ReceivedDataPacket.DataLen - 2);
        TraceLogger.SuccessInfo("Get Module SN Success!");
    }

    public byte[] RunFingerRegister()
    {
        TraceLogger.Info("请按手指采集图像...");
        if (RunFingerDetect(true))
        {
            RunGetImage();
            RunGenerate(0);
        }
        Thread.Sleep(500);
        TraceLogger.Info("请抬起手指...");
        if (RunFingerDetect(false))
        {
            TraceLogger.Info("请第2次按手指采集图像...");
            if (RunFingerDetect(true))
            {
                RunGetImage();
                RunGenerate(1);
            }
        }
        Thread.Sleep(500);
        TraceLogger.Info("请抬起手指...");
        if (RunFingerDetect(false))
        {
            TraceLogger.Info("请第3次按手指采集图像...");
            if (RunFingerDetect(true))
            {
                RunGetImage();
                RunGenerate(2);
            }
        }
        RunMerge(0, 3);
        RunUpChar(0, out byte[] imgbyte);
        TraceLogger.SuccessInfo(StructureHelper.BytesToHexStr(imgbyte));
        return imgbyte;
    }
    public string RunFingerGetImage()
    {
        TraceLogger.Info("请按手指采集图像...");
        if (RunFingerDetect(true))
        {
            RunGetImage();
        }
        RunUpImage(0, out string img);
        return img;
    }
}
