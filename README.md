# AmSoul.FPC1020
.Net library for FPC1020 fingerprint sensor

[![license](https://img.shields.io/badge/license-MIT-green.svg)](./LICENSE)

## Introduction
This library is a .Net7 library of FPC1020 .
## Pins
![fpc1020]

| NAME | I/O | DESCRIPTION |
| ------ | ------ | ------ |
| V-TOUCH | POWER | Power for finger detected funtion, 5V or 3.3V |
| TOUCH | OUTPUT | Output high (3.3V) when finger detected, otherwise output low |
| VCC | POWER | 5V power input |
| UART_TX | OUTPUT | Transmitter of TTL serial |
| UART_RX | INPUT | Receiver of TTL serial |
| GND | POWER | Power GND |

## Example
```CSharp
using AmSoul.FPC1020;
using Microsoft.Web.WebView2.Core;
public MainWindow(){
  await webView.EnsureCoreWebView2Async(null);
  webView.Source = new Uri(textBox1.Text);
  LoggerListener listener = new(webView.CoreWebView2);
  webView.CoreWebView2.AddHostObjectToScript("fpc1020", new FPC1020(listener));
}

public class LoggerListener : TraceListener
{
    private readonly CoreWebView2 _coreWebView2;
    public LoggerListener(CoreWebView2 coreWebView2)
    {
        _coreWebView2 = coreWebView2;
    }
    public override void Write(string message)
    {
        _coreWebView2.PostWebMessageAsString(message);
    }
    public override void WriteLine(string message)
    {
        _coreWebView2.PostWebMessageAsString(message);
    }
}

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class FPC1020Object
{
    private FPC1020 Device;
    public FPC1020Object(FPC1020 device)
    {
        Device = device;
    }
    public void ShowMessageArg(string arg)
    {
        MessageBox.Show($"Params {arg}");
    }
    public string GetData(string arg)
    {
        return $"调用取值{arg}";
    }
    public void Connect()
    {
        Device.Connect();
    }
    public void Disconnect()
    {
        Device.Disconnect();
    }
    public DeviceInfo GetDeviceInfo()
    {
        return Device.DeviceInfo;
    }
}
private void MessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
{
    string msg = args.TryGetWebMessageAsString();
    switch (msg)
    {
        case "connect":
            FPCTest();
            break;
        case "getDeviceInfo":
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(fpc1020.DeviceInfo, typeof(DeviceInfo)));
            break;
        case "getDeviceParam":
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(fpc1020.DeviceParam, typeof(DeviceParam)));
            break;
        case "disconnect":
            fpc1020.Disconnect();
            break;
        default:
            break;
    }
}
```
## Setup

- Env
  Need .Net7 SDK & Git

- Clone code

```bash
git clone https://github.com/InbornLee/AmSoul.FPC1020.git
```

[fpc1020]: https://raw.githubusercontent.com/sreckod/pyFPC1020/master/images/fpc1020_pins.jpg
