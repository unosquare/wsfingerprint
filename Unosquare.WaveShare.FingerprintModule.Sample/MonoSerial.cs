using RJCP.IO.Ports;
using RJCP.IO.Ports.Native;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unosquare.Swan;

namespace Unosquare.WaveShare.FingerprintModule.Sample
{
    public class MonoSerial : INativeSerial
    {
        [DllImport("MonoPosixHelper", SetLastError = true)]
        static extern int open_serial(string portName);

        [DllImport("MonoPosixHelper", SetLastError = true)]
        static extern int close_serial(int fd);

        [DllImport("MonoPosixHelper", SetLastError = true)]
        static extern int read_serial(int fd, byte[] buffer, int offset, int count);

        [DllImport("MonoPosixHelper", SetLastError = true)]
        static extern int write_serial(int fd, byte[] buffer, int offset, int count, int timeout);

        [DllImport("MonoPosixHelper", SetLastError = true)]
        static extern bool set_attributes(int fd, int baudRate, MonoParity parity, int dataBits, MonoStopBits stopBits, MonoHandshake handshake);

        public enum MonoHandshake
        {
            None,
            XOnXOff,
            RequestToSend,
            RequestToSendXOnXOff
        }

        public enum MonoParity
        {
            None,
            Odd,
            Even,
            Mark,
            Space
        }

        public enum MonoStopBits
        {
            None,
            One,
            Two,
            OnePointFive
        }


        public string Version { get; } = "1.0";

        public string PortName { get; set; }

        public string[] GetPortNames()
        {
            return new[] { PortName };
        }

        public PortDescription[] GetPortDescriptions()
        {
            return new[] { new PortDescription(PortName, PortName) };
        }

        public int BaudRate { get; set; } = 9600;

        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public bool DiscardNull { get; set; }
        public byte ParityReplace { get; set; }
        public bool TxContinueOnXOff { get; set; }
        public int XOffLimit { get; set; }
        public int XOnLimit { get; set; }
        public bool BreakState { get; set; }
        public int DriverInQueue { get; set; }
        public int DriverOutQueue { get; set; }
        public int BytesToRead { get; }
        public int BytesToWrite { get; }
        public bool CDHolding { get; }
        public bool CtsHolding { get; }
        public bool DsrHolding { get; }
        public bool RingHolding { get; }
        public bool DtrEnable { get; set; }
        public bool RtsEnable { get; set; }
        public Handshake Handshake { get; set; }
        public bool IsOpen => SerialIdentifier != -1;

        public void DiscardInBuffer()
        {
            if (!IsOpen) throw new InvalidOperationException("Port not open");

            // TODO: Complete
        }

        public void DiscardOutBuffer()
        {
            if (!IsOpen) throw new InvalidOperationException("Port not open");

            // TODO: Complete
        }

        public void GetPortSettings()
        {
            // do nothing
        }

        public void SetPortSettings()
        {
            // do nothing
        }

        public void Open()
        {
            $"Opening port with MONO {PortName} - {BaudRate}".Info();

            SerialIdentifier = open_serial(PortName);

            var result = set_attributes(SerialIdentifier, BaudRate, MonoParity.None, 8, MonoStopBits.One, MonoHandshake.None);

            $"Port open result: {SerialIdentifier} - {result}".Info();

            if (IsOpen == false)
                throw new InvalidOperationException("Unable to open port");
        }

        public void Close()
        {
            if (IsOpen == false)
                throw new InvalidOperationException();

            $"Closing port with WiringPi {PortName} - {BaudRate} - {SerialIdentifier}".Info();

            var closeResult = close_serial(SerialIdentifier);

            $"Port close result: {closeResult}".Info();

            SerialIdentifier = -1;
        }

        public SerialBuffer CreateSerialBuffer(int readBuffer, int writeBuffer)
        {
            return new SerialBuffer(readBuffer, writeBuffer, false);
        }

        private SerialBuffer m_Buffer;
        private Thread m_MonitorThread;
        private volatile bool m_IsRunning;
        private ManualResetEvent m_StopRunning = new ManualResetEvent(false);

        public void StartMonitor(SerialBuffer buffer, string name)
        {
            $"StartMonitor".Info();
            if (m_IsDisposed) throw new ObjectDisposedException(nameof(MonoSerial));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (!IsOpen) throw new InvalidOperationException("Serial Port not open");

            m_Buffer = buffer;
            m_StopRunning.Reset();

            try
            {
                m_IsRunning = true;
                m_MonitorThread = new Thread(ReadWriteThread)
                {
                    Name = "NSerMon",
                    IsBackground = true
                };
                m_MonitorThread.Start();
            }
            catch
            {
                m_IsRunning = false;
                throw;
            }
        }
        
        private void ReadWriteThread()
        {
            WaitHandle[] handles = {
                m_StopRunning,
                m_Buffer.Serial.ReadBufferNotFull,
                m_Buffer.Serial.WriteBufferNotEmpty
            };

            m_Buffer.WriteEvent += (s, e) =>
            {
                // Do nothing?
            };

            while (m_IsRunning && IsOpen)
            {
                var handle = WaitHandle.WaitAny(handles, -1);

                if (handle == 0)
                {
                    m_IsRunning = false;
                    continue;
                }

                // These are not in the switch statement to ensure that we can actually
                // read/write simultaneously.
                if (m_Buffer.Serial.ReadBufferNotFull.WaitOne(0))
                {
                    var rresult = read_serial(SerialIdentifier, m_Buffer.Serial.ReadBuffer.Array, 0, m_Buffer.Serial.ReadBuffer.Array.Length);
                    
                    //$"Reading {m_Buffer.Serial.ReadBuffer.Array.Length} - {rresult}".Info();

                    if (rresult > 0)
                    {
                        m_Buffer.Serial.ReadBufferProduce(rresult);
                        OnDataReceived(this, new SerialDataReceivedEventArgs(SerialData.Chars));
                    }
                }

                if (m_Buffer.Serial.WriteBufferNotEmpty.WaitOne(0))
                {
                    $"Doing handler write {m_Buffer.Serial.WriteBuffer.WriteLength} - {m_Buffer.Serial.WriteBuffer.End}".Info();

                    var wresult = write_serial(SerialIdentifier, m_Buffer.Serial.WriteBuffer.Array, m_Buffer.Serial.WriteBuffer.End,
                        m_Buffer.Serial.WriteBuffer.WriteLength, -1);

                    $"Writing {wresult}".Info();

                    if (wresult > 0)
                    {
                        m_Buffer.Serial.WriteBufferConsume(wresult);
                        m_Buffer.Serial.TxEmptyEvent();
                    }
                }
            }

            // Clear the write buffer. Anything that's still in the driver serial buffer will continue to write. The I/O was canceled
            // so no need to purge the actual driver itself.
            m_Buffer.Serial.Purge();

            // We must notify the stream that any blocking waits should abort.
            m_Buffer.Serial.DeviceDead();
        }

        public bool IsRunning => m_IsRunning;

        /// <summary>
        /// Occurs when data is received, or the EOF character is detected by the driver.
        /// </summary>
        public event EventHandler<SerialDataReceivedEventArgs> DataReceived;

        protected virtual void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            DataReceived?.Invoke(sender, args);
        }

        /// <summary>
        /// Occurs when an error condition is detected.
        /// </summary>
        public event EventHandler<SerialErrorReceivedEventArgs> ErrorReceived;

        protected virtual void OnCommError(object sender, SerialErrorReceivedEventArgs args)
        {
            ErrorReceived?.Invoke(sender, args);
        }

        /// <summary>
        /// Occurs when modem pin changes are detected.
        /// </summary>
        public event EventHandler<SerialPinChangedEventArgs> PinChanged;

        protected virtual void OnPinChanged(object sender, SerialPinChangedEventArgs args)
        {
            PinChanged?.Invoke(sender, args);
        }

        internal int SerialIdentifier { get; set; } = -1;

        #region IDisposable Support

        private bool m_IsDisposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_IsDisposed) return;

            if (disposing)
            {
                if (IsOpen) Close();
                m_StopRunning.Dispose();
                m_StopRunning = null;
                m_IsDisposed = true;
            }
        }

        #endregion
    }
}
