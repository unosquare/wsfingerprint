#if !NET452
//
//
// This class has several problems:
//
//   * No buffering, the specification requires that there is buffering, this
//     matters because a few methods expose strings and chars and the reading
//     is encoding sensitive.   This means that when we do a read of a byte
//     sequence that can not be turned into a full string by the current encoding
//     we should keep a buffer with this data, and read from it on the next
//     iteration.
//
//   * Calls to read_serial from the unmanaged C do not check for errors,
//     like EINTR, that should be retried
//
//   * Calls to the encoder that do not consume all bytes because of partial
//     reads 
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Unosquare.IO.Ports
{
    /// <summary>
    /// Represents a Serial Port component
    /// </summary>
    public class SerialPort
    {
        public const int InfiniteTimeout = -1;
        private const int DefaultReadBufferSize = 4096;
        private const int DefaultWriteBufferSize = 2048;
        private const int DefaultBaudRate = 9600;
        private const int DefaultDataBits = 8;
        private const Parity DefaultParity = Parity.None;
        private const StopBits DefaultStopBits = StopBits.One;

        private bool _isOpen;
        private int _baudRate;
        private Parity _parity;
        private StopBits _stopBits;
        private Handshake _handshake;
        private int _dataBits;
        private bool _dtrEnable = false;
        private bool _rtsEnable = false;
        private SerialPortStream _stream;
        private Encoding _encoding = Encoding.ASCII;
        private string _newLine = Environment.NewLine;
        private string _portName;
        private int _readTimeout = InfiniteTimeout;
        private int _writeTimeout = InfiniteTimeout;
        private int _readBufferSize = DefaultReadBufferSize;
        private int _writeBufferSize = DefaultWriteBufferSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPort" /> class.
        /// </summary>
        public SerialPort() :
            this(GetDefaultPortName(), DefaultBaudRate, DefaultParity, DefaultDataBits, DefaultStopBits)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPort" /> class.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        public SerialPort(string portName) :
            this(portName, DefaultBaudRate, DefaultParity, DefaultDataBits, DefaultStopBits)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPort" /> class.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="parity">The parity.</param>
        /// <param name="dataBits">The data bits.</param>
        /// <param name="stopBits">The stop bits.</param>
        public SerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            if (Swan.Runtime.OS == Swan.OperatingSystem.Windows)
                throw new InvalidOperationException(
                    "This class is only supported by UNIX OS. Use native NET Framework class");

            _portName = portName;
            _baudRate = baudRate;
            _dataBits = dataBits;
            _stopBits = stopBits;
            this._parity = parity;
        }

        private static string GetDefaultPortName()
        {
            var ports = GetPortNames();
            return ports.Length > 0 ? ports[0] : "ttyS0";
        }

        /// <summary>
        /// Gets the base stream.
        /// </summary>
        /// <value>
        /// The base stream.
        /// </value>
        public Stream BaseStream
        {
            get
            {
                CheckOpen();
                return (Stream) _stream;
            }
        }

        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        /// <value>
        /// The baud rate.
        /// </value>
        public int BaudRate
        {
            get { return _baudRate; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
#if !WIRINGPI
                if (is_open)
                    stream.SetAttributes(value, parity, data_bits, stop_bits, handshake);
#endif
                _baudRate = value;
            }
        }

        /// <summary>
        /// Gets the bytes to read.
        /// </summary>
        /// <value>
        /// The bytes to read.
        /// </value>
        public int BytesToRead
        {
            get
            {
                CheckOpen();
                return _stream.BytesToRead;
            }
        }

#if !WIRINGPI
        public int BytesToWrite
        {
            get
            {
                CheckOpen();
                return stream.BytesToWrite;
            }
        }
        public int DataBits
        {
            get { return data_bits; }
            set
            {
                if (value < 5 || value > 8)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (is_open)
                    stream.SetAttributes(baud_rate, parity, value, stop_bits, handshake);

                data_bits = value;
            }
        }
#endif

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding
        {
            get { return _encoding; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _encoding = value;
            }
        }

#if !WIRINGPI
        public Handshake Handshake
        {
            get { return handshake; }
            set
            {
                if (value < Handshake.None || value > Handshake.RequestToSendXOnXOff)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (is_open)
                    stream.SetAttributes(baud_rate, parity, data_bits, stop_bits, value);

                handshake = value;
            }
        }  
#endif

        /// <summary>
        /// Gets a value indicating whether this instance is open.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is open; otherwise, <c>false</c>.
        /// </value>
        public bool IsOpen => _isOpen;

        /// <summary>
        /// Gets or sets the new line.
        /// </summary>
        /// <value>
        /// The new line.
        /// </value>
        public string NewLine
        {
            get { return _newLine; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length == 0)
                    throw new ArgumentException("NewLine cannot be null or empty.", nameof(value));

                _newLine = value;
            }
        }

        /// <summary>
        /// Gets or sets the parity.
        /// </summary>
        /// <value>
        /// The parity.
        /// </value>
        public Parity Parity
        {
            get { return _parity; }
            set
            {
                if (value < Parity.None || value > Parity.Space)
                    throw new ArgumentOutOfRangeException(nameof(value));
#if !WIRINGPI
                if (is_open)
                    stream.SetAttributes(baud_rate, value, data_bits, stop_bits, handshake);
#endif
                _parity = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the port.
        /// </summary>
        /// <value>
        /// The name of the port.
        /// </value>
        public string PortName
        {
            get { return _portName; }
            set
            {
                if (_isOpen)
                    throw new InvalidOperationException("Port name cannot be set while port is open.");
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length == 0 || value.StartsWith("\\\\"))
                    throw new ArgumentException("value");

                _portName = value;
            }
        }

        /// <summary>
        /// Gets or sets the size of the read buffer.
        /// </summary>
        /// <value>
        /// The size of the read buffer.
        /// </value>
        public int ReadBufferSize
        {
            get { return _readBufferSize; }
            set
            {
                if (_isOpen)
                    throw new InvalidOperationException();
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (value <= DefaultReadBufferSize)
                    return;

                _readBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the read timeout.
        /// </summary>
        /// <value>
        /// The read timeout.
        /// </value>
        public int ReadTimeout
        {
            get { return _readTimeout; }
            set
            {
                if (value < 0 && value != InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (_isOpen)
                    _stream.ReadTimeout = value;

                _readTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the stop bits.
        /// </summary>
        /// <value>
        /// The stop bits.
        /// </value>
        public StopBits StopBits
        {
            get { return _stopBits; }
            set
            {
                if (value < StopBits.One || value > StopBits.OnePointFive)
                    throw new ArgumentOutOfRangeException(nameof(value));
#if !WIRINGPI
                if (is_open)
                    stream.SetAttributes(baud_rate, parity, data_bits, value, handshake);
#endif
                _stopBits = value;
            }
        }

        /// <summary>
        /// Gets or sets the size of the write buffer.
        /// </summary>
        /// <value>
        /// The size of the write buffer.
        /// </value>
        public int WriteBufferSize
        {
            get { return _writeBufferSize; }
            set
            {
                if (_isOpen)
                    throw new InvalidOperationException();
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (value <= DefaultWriteBufferSize)
                    return;

                _writeBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the write timeout.
        /// </summary>
        /// <value>
        /// The write timeout.
        /// </value>
        public int WriteTimeout
        {
            get { return _writeTimeout; }
            set
            {
                if (value < 0 && value != InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (_isOpen)
                    _stream.WriteTimeout = value;

                _writeTimeout = value;
            }
        }

        // methods

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected void Dispose(bool disposing)
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            // Do not close the base stream when the finalizer is run; the managed code can still hold a reference to it.
            if (disposing)
                _stream.Close();
            _stream = null;
        }

#if !WIRINGPI
        public void DiscardInBuffer()
        {
            CheckOpen();
            stream.DiscardInBuffer();
        }

        public void DiscardOutBuffer()
        {
            CheckOpen();
            stream.DiscardOutBuffer();
        }
#endif

        /// <summary>
        /// Gets the port names.
        /// </summary>
        /// <returns></returns>
        public static string[] GetPortNames()
        {
            var serialPorts = new List<string>();

            var ttys = Directory.GetFiles("/dev/", "tty*");
            var linuxStyle = false;

            //
            // Probe for Linux-styled devices: /dev/ttyS* or /dev/ttyUSB*
            // 
            foreach (var dev in ttys)
            {
                if (dev.StartsWith("/dev/ttyS") || dev.StartsWith("/dev/ttyUSB") || dev.StartsWith("/dev/ttyACM"))
                {
                    linuxStyle = true;
                    break;
                }
            }

            foreach (var dev in ttys)
            {
                if (linuxStyle)
                {
                    if (dev.StartsWith("/dev/ttyS") || dev.StartsWith("/dev/ttyUSB") ||
                        dev.StartsWith("/dev/ttyACM"))
                        serialPorts.Add(dev);
                }
                else
                {
                    if (dev != "/dev/tty" && dev.StartsWith("/dev/tty") && !dev.StartsWith("/dev/ttyC"))
                        serialPorts.Add(dev);
                }
            }

            return serialPorts.ToArray();
        }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        public void Open()
        {
            if (_isOpen)
                throw new InvalidOperationException("Port is already open");

            _stream = new SerialPortStream(_portName, _baudRate, _dataBits, _parity, _stopBits, _dtrEnable,
                _rtsEnable, _handshake, _readTimeout, _writeTimeout, _readBufferSize, _writeBufferSize);

            _isOpen = true;
        }

        /// <summary>
        /// Reads the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            CheckOpen();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("offset or count less than zero.");

            if (buffer.Length - offset < count)
                throw new ArgumentException("offset+count",
                    "The size of the buffer is less than offset + count.");

            return _stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Reads the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public int Read(char[] buffer, int offset, int count)
        {
            CheckOpen();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException(nameof(buffer));

            if (buffer.Length - offset < count)
                throw new ArgumentException("offset+count",
                    "The size of the buffer is less than offset + count.");

            int c, i;
            for (i = 0; i < count && (c = ReadChar()) != -1; i++)
                buffer[offset + i] = (char) c;

            return i;
        }

        internal int read_byte()
        {
            var buff = new byte[1];
            if (_stream.Read(buff, 0, 1) > 0)
                return buff[0];

            return -1;
        }

        /// <summary>
        /// Reads the byte.
        /// </summary>
        /// <returns></returns>
        public int ReadByte()
        {
            CheckOpen();
            return read_byte();
        }

        /// <summary>
        /// Reads the character.
        /// </summary>
        /// <returns></returns>
        public int ReadChar()
        {
            CheckOpen();

            var buffer = new byte[16];
            var i = 0;

            do
            {
                var b = read_byte();
                if (b == -1)
                    return -1;
                buffer[i++] = (byte) b;
                var c = _encoding.GetChars(buffer, 0, 1);
                if (c.Length > 0)
                    return (int) c[0];
            } while (i < buffer.Length);

            return -1;
        }

#if !WIRINGPI
        public string ReadExisting()
        {
            CheckOpen();

            int count = BytesToRead;
            byte[] bytes = new byte[count];

            int n = stream.Read(bytes, 0, count);
            return new String(encoding.GetChars(bytes, 0, n));
        }
#endif

        /// <summary>
        /// Reads the line.
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            return ReadTo(_newLine);
        }

        /// <summary>
        /// Reads to.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string ReadTo(string value)
        {
            CheckOpen();
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length == 0)
                throw new ArgumentException("value");

            // Turn into byte array, so we can compare
            var byteValue = _encoding.GetBytes(value);
            var current = 0;
            var seen = new List<byte>();

            while (true)
            {
                var n = read_byte();
                if (n == -1)
                    break;
                seen.Add((byte) n);
                if (n == byteValue[current])
                {
                    current++;
                    if (current == byteValue.Length)
                        return _encoding.GetString(seen.ToArray(), 0, seen.Count - byteValue.Length);
                }
                else
                {
                    current = (byteValue[0] == n) ? 1 : 0;
                }
            }
            return _encoding.GetString(seen.ToArray());
        }

        /// <summary>
        /// Writes the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Write(string text)
        {
            CheckOpen();
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var buffer = _encoding.GetBytes(text);
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            CheckOpen();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException();

            if (buffer.Length - offset < count)
                throw new ArgumentException("offset+count",
                    nameof(buffer));

            _stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public void Write(char[] buffer, int offset, int count)
        {
            CheckOpen();
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException();

            if (buffer.Length - offset < count)
                throw new ArgumentException("offset+count",
                    "The size of the buffer is less than offset + count.");

            var bytes = _encoding.GetBytes(buffer, offset, count);
            _stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="text">The text.</param>
        public void WriteLine(string text)
        {
            Write(text + _newLine);
        }

        private void CheckOpen()
        {
            if (!_isOpen)
                throw new InvalidOperationException("Specified port is not open.");
        }
    }
}

#endif