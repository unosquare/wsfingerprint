[![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/wsfingerprint/)](https://github.com/igrigorik/ga-beacon)

# WaveShare Fingerprint Reader - Interfacing Library for .NET

* Wiki: http://www.waveshare.com/wiki/UART_Fingerprint_Reader
* Reference Manual: http://www.waveshare.com/w/upload/6/65/UART-Fingerprint-Reader-UserManual.pdf
* Product Page: http://www.waveshare.com/uart-fingerprint-reader.htm

## Features
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

## Missing Stuff
* ~~The module can work at 115200 BAUD rate but am am yet to identify the command to change the default 19200~~
