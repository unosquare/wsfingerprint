[![Build status](https://ci.appveyor.com/api/projects/status/6t4myxvd36bfqwis/branch/master?svg=true)](https://ci.appveyor.com/project/geoperez/wsfingerprint/branch/master)[![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/wsfingerprint/)](https://github.com/igrigorik/ga-beacon)

# <img src="https://github.com/unosquare/wsfingerprint/raw/master/logos/wsfp-logo-32.png"></img> WaveShare Fingerprint Reader

*:star: Please star this project if you find it useful!*

Interfacing Library for .NET 4.5 (Mono) and .NET Core!

* [Product page](http://www.waveshare.com/uart-fingerprint-reader.htm)
* [Reference manual](http://www.waveshare.com/w/upload/6/65/UART-Fingerprint-Reader-UserManual.pdf)
* [Wiki](http://www.waveshare.com/wiki/UART_Fingerprint_Reader)

<img src="https://github.com/unosquare/wsfingerprint/raw/master/logos/wsfp-image.jpg">

## Specifications

| Parameter                    | Specification                         |
| ---------------------------- | ------------------------------------- |
| Processor (CPU)              | STM32F205                             |
| Sensor                       | HD optical                            |
| Memory                       | Built-in (extensible)                 |
| Anti-wearing                 | 1 million times                       |
| Anti-electrostatic           | 150KV                                 |
| Fingerprint capacity         | 1000                                  |
| False acceptance rate        | <0.001% (on security level 5)         |
| False rejection rate         | <0.1% (on security level 5)           |
| Current                      | <50ma                                 |
| Input time                   | <0.5s                                 |
| Matching time                | <0.5s                                 |
| Matching mode                | 1 : 1, 1 : N                          |
| Security level               | 1-10 (supports customization)         |
| Output formats               | User ID, Image, Feature               |
| Feature size                 | 196 Byte                              |
| Feature template size        | 512 Byte                              |
| Template rule standard       | ISO19794-2                            |
| Communication interface      | UART                                  |
| Communication baud rate      | 9600-57600bps                         |
| Power supply                 | UART, external power                  |
| Voltage level                | 3.3-7.5V                              |
| PCB dimension                | 40 * 58 * 8mm                             |
| Operating temp.              |-20℃ to 60℃                          |
| Relative humidity            | 40%RH to 85%RH (without condensation) |

## Library Features
* All documented commands are implemented (2016-11-06)
* Operations are all asynchronous
* Nice sample application included for testing
* MIT License
* .Net Framework (and Mono)
  * No dependencies
* .Net Standard
  * [SerialPortStream](https://github.com/jcurl/serialportstream): Independent implementation of System.IO.Ports.SerialPort and SerialStream for portability.

## NuGet Installation: [![NuGet version](https://badge.fury.io/nu/Unosquare.WaveShare.FingerprintReader.svg)](https://badge.fury.io/nu/Unosquare.WaveShare.FingerprintReader)

```
PM> Install-Package Unosquare.WaveShare.FingerprintReader
```

## Usage

```csharp
using (var reader = new FingerprintReader())
{
    reader.Open("COM3");
    var result = await reader.GetDspVersionNumber();
    Console.WriteLine($"Module Version: {result.Version}");
}
```


## Related fingerprint projects

| Project | Description |
|--------| ---|
|[sparkfunfingerprint](https://github.com/unosquare/sparkfunfingerprint)|SparkFun Fingerprint Reader (GT-521Fxx) - Interfacing Library for .NET 4.5 (and Mono) and .NET Core! |
|[libfprint-cs](https://github.com/unosquare/libfprint-cs)|The long-awaited C# (.NET/Mono) wrapper for the great fprint library|
