namespace Unosquare.WaveShare.FingerprintModule
{
    using Swan;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
#if NET452
    using System.IO.Ports;
#else
    using Unosquare.IO.Ports;
#endif

    /// <summary>
    /// Our main character representing the WaveShare Fingerprint reader module
    /// Reference: http://www.waveshare.com/w/upload/6/65/UART-Fingerprint-Reader-UserManual.pdf
    /// WIKI: http://www.waveshare.com/wiki/UART_Fingerprint_Reader
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class FingerprintReader : IDisposable
    {
        /// <summary>
        /// The mode how is running the reader
        /// </summary>
        public const string Mode =
#if WIRINGPI
            "WiringPi";
#else
            "Normal";
#endif

#region Private Declarations

        /// <summary>
        /// The read buffer length of the serial port
        /// </summary>
        private const int ReadBufferLength = 1024 * 16;

        /// <summary>
        /// The default timeout
        /// </summary>
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The baud rate probe timeout
        /// </summary>
        private static readonly TimeSpan BaudRateProbeTimeout = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// The image acquisition timeout
        /// </summary>
        private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(60);

        private bool IsDisposing;
        private static readonly ManualResetEventSlim SerialPortDone = new ManualResetEventSlim(true);
#if DEBUG
        private const bool IsDebugBuild = true;
#else
        private const bool IsDebugBuild = false;
#endif

#endregion

#region Properties
        
        /// <summary>
        /// Gets the serial port associated with this reader.
        /// </summary>
        public SerialPort SerialPort { get; protected set; }

#endregion

#region Open and Close Methods
        /// <summary>
        /// Opens the serial port with the specified port name.
        /// Under Windows it's something like COM3. On Linux, it's something like
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baud">The baud.</param>
        /// <param name="probeBaudRates">if set to <c>true</c> [probe baud rates].</param>
        /// <exception cref="System.InvalidOperationException">Device is already open. Call the Close method first.</exception>
        public void Open(string portName, BaudRate baud, bool probeBaudRates)
        {
            if (SerialPort != null)
                throw new InvalidOperationException("Device is already open. Call the Close method first.");

            SerialPort = new SerialPort(portName, baud.ToInt(), Parity.None, 8, StopBits.One)
            {
                ReadBufferSize = ReadBufferLength
            };

            SerialPort.Open();

            if (probeBaudRates)
            {
                "Will probe baud rates.".Trace();
                var baudRateResponse = GetBaudRate().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Opens the serial port with the specified port name.
        /// Under Windows it's something like COM3. On Linux, it's something like
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        public void Open(string portName)
        {
            Open(portName, BaudRate.Baud19200, true);
        }

        /// <summary>
        /// Closes serial port communication if open
        /// </summary>
        private void Close()
        {
            if (SerialPort == null)
                return;

            try
            {
                if (SerialPort.IsOpen)
                    SerialPort.Close();
            }
            finally
            {
                SerialPort.Dispose();
                SerialPort = null;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposing) return;
            IsDisposing = true;
            Close();
        }

#endregion

#region Fingerprint Reader Protocol

        /// <summary>
        /// Gets the version number of the DSP module.
        /// </summary>
        /// <returns></returns>
        public async Task<GetDspVersionNumberResponse> GetDspVersionNumber()
        {
            var command = Command.Factory.CreateGetDspVersionNumberCommand();
            return await GetResponseAsync<GetDspVersionNumberResponse>(command);
        }

        /// <summary>
        /// Makes the module go to sleep and stop processing commands. The only way to bring it back
        /// online is by resetting it.
        /// </summary>
        /// <returns></returns>
        public async Task<Response> Sleep()
        {
            var command = Command.Factory.CreateSleepCommand();
            return await GetResponseAsync<Response>(command);
        }

        /// <summary>
        /// Gets the baud rate.
        /// This method probes the serial port at different baud rates until communication is correctly established.
        /// </summary>
        /// <returns></returns>
        public async Task<GetSetBaudRateResponse> GetBaudRate()
        {
            if (SerialPort.IsOpen == false)
                throw new InvalidOperationException(
                    $"Call the {nameof(Open)} method before attempting communication with the module");

            var portName = SerialPort.PortName;
            var baudRates = Enum.GetValues(typeof(BaudRate)).Cast<BaudRate>().ToArray();

            var resultPayload = Command.CreateFixedLengthPayload(OperationCode.ChangeBaudRate, 0, 0,
                (byte) SerialPort.BaudRate.ToBaudRate());

            var result = new GetSetBaudRateResponse(resultPayload);
            
            var probeCommand = Command.Factory.CreateGetUserCountCommand();
            var probeResponse = await GetResponseAsync<GetUserCountResponse>(probeCommand, BaudRateProbeTimeout);

            if (probeResponse != null)
                return result;

            foreach (var baudRate in baudRates)
            {
                Close();
                Open(portName, baudRate, false);

                probeResponse = await GetResponseAsync<GetUserCountResponse>(probeCommand, BaudRateProbeTimeout);

                if (probeResponse != null)
                {
                    resultPayload = Command.CreateFixedLengthPayload(OperationCode.ChangeBaudRate, 0, 0, (byte) baudRate);
                    var baudRateResponse = new GetSetBaudRateResponse(resultPayload);

                    if (IsDebugBuild)
                        $"RX: {baudRateResponse}".Info();

                    return baudRateResponse;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the baud rate of the module.
        /// This closes and re-opens the serial port
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        /// <returns></returns>
        public async Task<GetSetBaudRateResponse> SetBaudRate(BaudRate baudRate)
        {
            var currentBaudRate = await GetBaudRate();
            if (currentBaudRate.BaudRate == baudRate)
            {
                return new GetSetBaudRateResponse(
                    Command.CreateFixedLengthPayload(OperationCode.ChangeBaudRate, 0, 0, (byte) baudRate, 0));
            }

            var command = Command.Factory.CreateChangeBaudRateCommand(baudRate);
            var response = await GetResponseAsync<GetSetBaudRateResponse>(command);
            if (response != null)
            {
                var portName = SerialPort.PortName;
                Close();
                Open(portName, baudRate, false);
            }

            return response;
        }

        /// <summary>
        /// Gets the registration mode which specifies if a fingerprint can be registered more than once.
        /// </summary>
        /// <returns></returns>
        public async Task<GetSetRegistrationModeResponse> GetRegistrationMode()
        {
            var command = Command.Factory.CreateGetSetRegistrationModeCommand(GetSetMode.Get, false);
            return await GetResponseAsync<GetSetRegistrationModeResponse>(command);
        }

        /// <summary>
        /// Sets the registration mode. Prohibit repeat disallows registration of the same fingerprint for more than 1 user.
        /// </summary>
        /// <param name="prohibitRepeat">if set to <c>true</c> [prohibit repeat].</param>
        /// <returns></returns>
        public async Task<GetSetRegistrationModeResponse> SetRegistrationMode(bool prohibitRepeat)
        {
            var command = Command.Factory.CreateGetSetRegistrationModeCommand(GetSetMode.Set, prohibitRepeat);
            return await GetResponseAsync<GetSetRegistrationModeResponse>(command);
        }

        /// <summary>
        /// Registers a fingerprint. You have to call this method 3 times specifying the corresponding iteration 1, 2, or 3
        /// </summary>
        /// <param name="iteration">The iteration.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userPrivilege">The user privilege.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// iteration
        /// or
        /// userId
        /// or
        /// userPrivilege
        /// </exception>
        public async Task<AddFingerprintResponse> AddFingerprint(int iteration, int userId, int userPrivilege)
        {
            if (iteration < 0 || iteration > 3)
                throw new ArgumentException($"{nameof(iteration)} must be a number between 1 and 3");
            if (userId < 1 || userId > 4095)
                throw new ArgumentException($"{nameof(userId)} must be a number between 1 and 4095");
            if (userPrivilege < 0 || userPrivilege > 3)
                throw new ArgumentException($"{nameof(userPrivilege)} must be a number between 1 and 3");

            var command = Command.Factory.CreateAddFingerprintCommand(Convert.ToByte(iteration),
                Convert.ToUInt16(userId), Convert.ToByte(userPrivilege));
            return await GetResponseAsync<AddFingerprintResponse>(command, AcquireTimeout);
        }

        /// <summary>
        /// Deletes the specified user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public async Task<Response> DeleteUser(int userId)
        {
            var command = Command.Factory.CreateDeleteUserCommand(Convert.ToUInt16(userId));
            return await GetResponseAsync<Response>(command);
        }

        /// <summary>
        /// Deletes all users.
        /// </summary>
        /// <returns></returns>
        public async Task<Response> DeleteAllUsers()
        {
            var command = Command.Factory.CreateDeleteAllUsersCommand();
            return await GetResponseAsync<Response>(command, AcquireTimeout);
        }

        /// <summary>
        /// Gets the user count.
        /// </summary>
        /// <returns></returns>
        public async Task<GetUserCountResponse> GetUserCount()
        {
            var command = Command.Factory.CreateGetUserCountCommand();
            return await GetResponseAsync<GetUserCountResponse>(command);
        }

        /// <summary>
        /// Returns a User id after acquiring an image from the sensor. Match 1:N
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        public async Task<MatchOneToNResponse> MatchOneToN(CancellationToken ct = default(CancellationToken))
        {
            var command = Command.Factory.CreateMatchOneToNCommand();
            return await GetResponseAsync<MatchOneToNResponse>(command, AcquireTimeout, ct);
        }

        /// <summary>
        /// Acquires an image from the sensor and tests if it matches the supplied user id. Match 1:1
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Response> MatchOneToOne(int userId, CancellationToken ct = default(CancellationToken))
        {
            var command = Command.Factory.CreateMatchOneToOneCommand(Convert.ToUInt16(userId));
            return await GetResponseAsync<Response>(command, AcquireTimeout, ct);
        }

        /// <summary>
        /// Gets the user privilege given its id.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public async Task<GetUserPrivilegeResponse> GetUserPrivilege(int userId)
        {
            var command = Command.Factory.CreateGetUserPrivilegeCommand(Convert.ToUInt16(userId));
            return await GetResponseAsync<GetUserPrivilegeResponse>(command);
        }

        /// <summary>
        /// Gets the matching level.
        /// </summary>
        /// <returns></returns>
        public async Task<GetSetMatchingLevelResponse> GetMatchingLevel()
        {
            var command = Command.Factory.CreateGetSetMatchingLevelCommand(GetSetMode.Get, 0);
            return await GetResponseAsync<GetSetMatchingLevelResponse>(command);
        }

        /// <summary>
        /// Sets the matching level. 0 is the loosest, 9 is the strictest
        /// </summary>
        /// <param name="matchingLevel">The matching level.</param>
        /// <returns></returns>
        public async Task<GetSetMatchingLevelResponse> SetMatchingLevel(int matchingLevel)
        {
            var command = Command.Factory.CreateGetSetMatchingLevelCommand(GetSetMode.Set, Convert.ToByte(matchingLevel));
            return await GetResponseAsync<GetSetMatchingLevelResponse>(command);
        }

        /// <summary>
        /// Acquires an image from the sensor and returns the image bytes in grayscale nibbles. This operation is fairly slow.
        /// </summary>
        /// <returns></returns>
        public async Task<AcquireImageResponse> AcquireImage(CancellationToken ct = default(CancellationToken))
        {
            var command = Command.Factory.CreateAcquireImageCommand();
            return await GetResponseAsync<AcquireImageResponse>(command, AcquireTimeout, ct);
        }

        /// <summary>
        /// Acquires an image from the sensor and returns the computed eigenvalues.
        /// </summary>
        /// <returns></returns>
        public async Task<AcquireImageEigenvaluesResponse> AcquireImageEigenvalues()
        {
            var command = Command.Factory.CreateAcquireImageEigenvaluesCommand();
            return await GetResponseAsync<AcquireImageEigenvaluesResponse>(command, AcquireTimeout);
        }

        /// <summary>
        /// Acquires an image from the sensor and determines if the supplied eigenvalues match. Match 1:1
        /// </summary>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <returns></returns>
        public async Task<Response> MatchImageToEigenvalues(byte[] eigenvalues)
        {
            var command = Command.Factory.CreateMatchImageToEigenvaluesCommand(eigenvalues);
            return await GetResponseAsync<Response>(command, AcquireTimeout);
        }

        /// <summary>
        /// Provides a method to test if the given user id matches the specified eigenvalues. Match 1:1
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <returns></returns>
        public async Task<Response> MatchUserToEigenvalues(int userId, byte[] eigenvalues)
        {
            var command = Command.Factory.CreateMatchUserToEigenvaluesCommand(Convert.ToUInt16(userId), eigenvalues);
            return await GetResponseAsync<Response>(command);
        }

        /// <summary>
        /// Finds a user id for the given eigenvalues. Match 1:N
        /// </summary>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <returns></returns>
        public async Task<MatchEigenvaluesToUserResponse> MatchEigenvaluesToUser(byte[] eigenvalues)
        {
            var command = Command.Factory.CreateMatchEigenvaluesToUserCommand(eigenvalues);
            return await GetResponseAsync<MatchEigenvaluesToUserResponse>(command);
        }

        /// <summary>
        /// Gets a user's privilege and eigenvalues.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public async Task<GetUserPropertiesResponse> GetUserProperties(int userId)
        {
            var command = Command.Factory.CreateGetUserPropertiesCommand(Convert.ToUInt16(userId));
            return await GetResponseAsync<GetUserPropertiesResponse>(command);
        }

        /// <summary>
        /// Sets or overwrites a specified user with the given privilege and eigenvalues
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="privilege">The privilege.</param>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <returns></returns>
        public async Task<SetUserPropertiesResponse> SetUserProperties(int userId, int privilege, byte[] eigenvalues)
        {
            var command = Command.Factory.CreateSetUserPropertiesCommand(Convert.ToUInt16(userId),
                Convert.ToByte(privilege), eigenvalues);
            return await GetResponseAsync<SetUserPropertiesResponse>(command);
        }

        /// <summary>
        /// Gets the capture timeout. Timeout is between 0 to 255. 0 denotes to wait indefinitely for a capture
        /// </summary>
        /// <returns></returns>
        public async Task<GetSetCaptureTimeoutResponse> GetCaptureTimeout()
        {
            var command = Command.Factory.CreateGetSetCaptureTimeoutCommand(GetSetMode.Get, 0);
            return await GetResponseAsync<GetSetCaptureTimeoutResponse>(command);
        }

        /// <summary>
        /// Sets the capture timeout. Timeout must be 0 to 255. 0 denotes to wait indefinitely for a capture
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public async Task<GetSetCaptureTimeoutResponse> SetCaptureTimeout(int timeout)
        {
            var command = Command.Factory.CreateGetSetCaptureTimeoutCommand(GetSetMode.Set, Convert.ToByte(timeout));
            return await GetResponseAsync<GetSetCaptureTimeoutResponse>(command);
        }

        /// <summary>
        /// Gets all users and their permissions. It does not retrieve User eigenvalues.
        /// </summary>
        /// <returns></returns>
        public async Task<GetAllUsersResponse> GetAllUsers()
        {
            var command = Command.Factory.CreateGetAllUsersCommand();
            return await GetResponseAsync<GetAllUsersResponse>(command);
        }

#endregion

#region Read and Write Methods

        /// <summary>
        /// Given a command, gets a response object asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">The command.</param>
        /// <param name="responseTimeout">The response timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Open</exception>
        private async Task<T> GetResponseAsync<T>(Command command, TimeSpan responseTimeout,
            CancellationToken ct = default(CancellationToken))
            where T : ResponseBase
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            var startTime = DateTime.UtcNow;

            var discardedBytes = await FlushReadBufferAsync(ct);
            if (discardedBytes.Length > 0 && IsDebugBuild)
            {
                $"RX: Discarded {discardedBytes.Length} bytes: {BitConverter.ToString(discardedBytes).Replace("-", " ")}"
                    .Trace();
            }

            await WriteAsync(command.Payload);

            if (IsDebugBuild)
                $"TX: {command}".Debug();

            var responseBytes = await ReadAsync(responseTimeout, ct);
            if (responseBytes == null || responseBytes.Length <= 0)
            {
                if (IsDebugBuild)
                    $"RX: No response received after {responseTimeout.TotalMilliseconds} ms".Error();

                return null;
            }

            var response = Activator.CreateInstance(typeof(T), responseBytes) as T;
            if (IsDebugBuild)
            {
                $"RX: {response}".Info();
                $"Request-Response cycle took {DateTime.UtcNow.Subtract(startTime).TotalMilliseconds} ms".Trace();
            }

            return response;
        }

        /// <summary>
        /// Gets a response with the default timeout
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        private async Task<T> GetResponseAsync<T>(Command command)
            where T : ResponseBase
        {
            return await GetResponseAsync<T>(command, DefaultTimeout);
        }

        /// <summary>
        /// Writes data to the serial port asynchronously
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task WriteAsync(byte[] payload)
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            SerialPortDone.Wait();
            SerialPortDone.Reset();

            try
            {
                await SerialPort.BaseStream.WriteAsync(payload, 0, payload.Length);
                await SerialPort.BaseStream.FlushAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SerialPortDone.Set();
            }
        }

        /// <summary>
        /// Reads data from the serial port asynchronously with the default timeout
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> ReadAsync()
        {
            return await ReadAsync(DefaultTimeout);
        }

        /// <summary>
        /// Flushes the serial port read data discarding all bytes in the read buffer
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        private async Task<byte[]> FlushReadBufferAsync(CancellationToken ct = default(CancellationToken))
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                return new byte[] {};

            SerialPortDone.Wait();
            SerialPortDone.Reset();

            try
            {
                var count = 0;
                var buffer = new byte[SerialPort.ReadBufferSize];
                var offset = 0;
                while (SerialPort.BytesToRead > 0)
                {
                    count +=
                        await SerialPort.BaseStream.ReadAsync(buffer, offset, buffer.Length, ct);
                    offset += count;
                }

                return buffer.Skip(0).Take(count).ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SerialPortDone.Set();
            }
        }

        /// <summary>
        /// Reads bytes from the serial port.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Open</exception>
        public async Task<byte[]> ReadAsync(TimeSpan timeout, CancellationToken ct = default(CancellationToken))
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            SerialPortDone.Wait();
            SerialPortDone.Reset();

            try
            {
                var startTime = DateTime.UtcNow;
                var response = new List<byte>(1024 * 10);
                var expectedBytes = 8;
                var remainingBytes = expectedBytes;
                var iteration = 0;
                var isVariableLengthResponse = false;
                var largePacketDelayMilliseconds = 0;
                const int largePacketSize = 500;

                startTime = DateTime.UtcNow;
                var buffer = new byte[SerialPort.ReadBufferSize];

                while (SerialPort.IsOpen && response.Count < expectedBytes && ct.IsCancellationRequested == false)
                {
                    if (SerialPort.BytesToRead > 0)
                    {
                        var readBytes =
                            await SerialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, ct);

                        response.AddRange(buffer.Skip(0).Take(readBytes));
                        remainingBytes = expectedBytes - response.Count;
                        startTime = DateTime.UtcNow;

                        // for larger data packets we want to give it a nicer breather
                        if (isVariableLengthResponse && response.Count < expectedBytes &&
                            expectedBytes > largePacketSize)
                        {
                            if (IsDebugBuild)
                                $"RX: Received {readBytes} bytes. Length: {response.Count} of {expectedBytes}; {remainingBytes} remaining - Delay: {largePacketDelayMilliseconds} ms"
                                    .Trace();

                            await Task.Delay(largePacketDelayMilliseconds, ct);
                        }

                        if (response.Count >= 4 && iteration == 0)
                        {
                            iteration++;

                            isVariableLengthResponse =
                                ResponseBase.ResponseLengthCategories[(OperationCode) response[1]] ==
                                MessageLengthCategory.Variable;
                            if (isVariableLengthResponse)
                            {
                                var headerByteCount = (new byte[] {response[2], response[3]}).BigEndianArrayToUInt16();
                                if (headerByteCount > 0)
                                {
                                    expectedBytes = 8 + 3 + headerByteCount;
                                    largePacketDelayMilliseconds =
                                        (int) Math.Max((double) expectedBytes / SerialPort.BaudRate * 1000d, 100d);

                                    if (IsDebugBuild)
                                        $"RX: Expected Bytes: {expectedBytes}. Large Packet delay: {largePacketDelayMilliseconds} ms"
                                            .Trace();
                                }
                                else
                                {
                                    expectedBytes = 8;
                                    isVariableLengthResponse = false;
                                }
                            }

                        }
                    }
                    else
                    {
                        await Task.Delay(10, ct);
                    }

                    if (DateTime.UtcNow.Subtract(startTime) > timeout)
                    {
                        if (IsDebugBuild)
                        {
                            $"RX: Did not receive enough bytes. Received: {response.Count}  Expected: {expectedBytes}"
                                .Error();
                            $"RX: {BitConverter.ToString(response.ToArray()).Replace("-", " ")}".Error();
                        }

                        return null;
                    }

                }

                return response.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SerialPortDone.Set();
            }
        }

#endregion
    }
}