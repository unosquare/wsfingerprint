[![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/wsfingerprint/)](https://github.com/igrigorik/ga-beacon)

# WaveShare Fingerprint Reader - Interfacing Library for .NET

* Wiki: http://www.waveshare.com/wiki/UART_Fingerprint_Reader
* Reference Manual: http://www.waveshare.com/w/upload/6/65/UART-Fingerprint-Reader-UserManual.pdf
* Product Page: http://www.waveshare.com/uart-fingerprint-reader.htm

<img src="http://www.waveshare.com/media/catalog/product/cache/1/image/560x560/9df78eab33525d08d6e5fb8d27136e95/u/a/uart-fingerprint-reader_l_1_2.jpg">

## Specifications

<table class="tabSty-1" width="600">
<tbody>
<tr><th><font size="3">Parameter</font></th><th><font size="3">Specification</font></th></tr>
<tr>
<td><font size="3">Processor (CPU)&nbsp;</font></td>
<td><font size="3">STM32F205</font></td>
</tr>
<tr>
<td><font size="3">Sensor</font></td>
<td><font size="3">HD optical</font></td>
</tr>
<tr>
<td><font size="3">Memory</font></td>
<td><font size="3">Built-in (extensible)</font></td>
</tr>
<tr>
<td><font size="3">Anti-wearing</font></td>
<td><font size="3">1 million times</font></td>
</tr>
<tr>
<td><font size="3">Anti-electrostatic</font></td>
<td><font size="3">150KV&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">Fingerprint capacity</font></td>
<td><font size="3">1000</font></td>
</tr>
<tr>
<td><font size="3">False acceptance rate</font></td>
<td><font size="3">&lt;0.001% (on security level 5)&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">False rejection rate</font></td>
<td><font size="3">&lt;0.1%&nbsp;(on security level 5)&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">Current</font></td>
<td><font size="3">&lt;50ma&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">Input time</font></td>
<td><font size="3">&lt;0.5s</font></td>
</tr>
<tr>
<td><font size="3">Matching time</font></td>
<td><font size="3">&lt;0.5s</font></td>
</tr>
<tr>
<td><font size="3">Matching mode</font></td>
<td><font size="3">1 : 1&nbsp;<br>1 : N&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">Security level</font></td>
<td><font size="3">1-10 (supports customization)</font></td>
</tr>
<tr>
<td><font size="3">Output formats</font></td>
<td><font size="3">User ID, Image, Feature</font></td>
</tr>
<tr>
<td><font size="3">Feature size</font></td>
<td><font size="3">196 Byte</font></td>
</tr>
<tr>
<td><font size="3">Feature template size&nbsp;</font></td>
<td><font size="3">512 Byte&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">Template rule standard</font></td>
<td><font size="3">ISO19794-2&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">Communication interface</font></td>
<td><font size="3">UART</font></td>
</tr>
<tr>
<td><font size="3">Communication baud rate</font></td>
<td><font size="3">9600-57600bps</font></td>
</tr>
<tr>
<td><font size="3">Power supply</font></td>
<td><font size="3">UART, external power</font></td>
</tr>
<tr>
<td><font size="3">Voltage level</font></td>
<td><font size="3">3.3-7.5V&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">PCB dimension</font></td>
<td><font size="3">40*58*8mm&nbsp;&nbsp;</font></td>
</tr>
<tr>
<td><font size="3">Operating temp.</font></td>
<td><font size="3">-20℃&nbsp;to 60℃</font></td>
</tr>
<tr>
<td><font size="3">Relative humidity</font></td>
<td><font size="3">40%RH to 85%RH (without condensation)</font></td>
</tr>
</tbody>
</table>

## Library Features
* All documented commands are implemented (2016-11-06)
* Operations are all asynchronous
* No dependencies
* Nice sample application included for testing
* MIT License

## NuGet Installation:

```
PM> Install-Package Unosquare.WaveShare.FingerprintReader
```

## Usage

```csharp
using (var reader = new FingerprintReader())
{
    reader.open("COM3");
    var result = await reader.GetDspVersionNumber();
    Console.WriteLine($"Module Version: {result.Version}");
}
```

