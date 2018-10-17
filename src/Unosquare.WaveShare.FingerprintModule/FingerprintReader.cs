namespace Unosquare.WaveShare.FingerprintModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using SerialPort;

    /// <summary>
    /// Our main character representing the WaveShare Fingerprint reader module.
    /// 
    /// Reference: http://www.waveshare.com/w/upload/6/65/UART-Fingerprint-Reader-UserManual.pdf
    /// WIKI: http://www.waveshare.com/wiki/UART_Fingerprint_Reader.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class FingerprintReader : IDisposable
    {
        /// <summary>
        /// The read buffer length of the serial port.
        /// </summary>
        private const int ReadBufferLength = 1024 * 16;
        
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan BaudRateProbeTimeout = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(60);
        private static readonly ManualResetEventSlim SerialPortDone = new ManualResetEventSlim(true);

        private bool _disposedValue; // To detect redundant calls
        
        /// <summary>
        /// Gets the serial port associated with this reader.
        /// </summary>
        public ISerialPort SerialPort { get; private set; }
        
        #region Open and Close Methods
        
        /// <summary>
        /// Gets an array of serial port names for the current computer.
        ///
        /// This method is just a shortcut for Microsoft and RJCP libraries,
        /// you may use your SerialPort library to enumerate the available ports.
        /// </summary>
        /// <returns>An array of serial port names for the current computer.</returns>
        public static string[] GetPortNames() =>
#if NET452
            MsSerialPort.GetPortNames();
#else
                RjcpSerialPort.GetPortNames();
#endif

        /// <summary>
        /// Opens the serial port with the specified port name.
        /// Under Windows it's something like COM3. On Linux, it's something like /dev/ttyS0.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="probeBaudRates">if set to <c>true</c> [probe baud rates].</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/>.</param>
        /// <exception cref="System.InvalidOperationException">Device is already open. Call the Close method first.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OpenAsync(string portName, BaudRate baudRate, bool probeBaudRates, CancellationToken ct = default)
        {
            if (SerialPort != null)
                throw new InvalidOperationException("Device is already open. Call the Close method first.");

            SerialPort =
#if NET452
                new MsSerialPort(portName, baudRate.ToInt())
#else
                new RjcpSerialPort(portName, baudRate.ToInt())
#endif
            {
                ReadBufferSize = ReadBufferLength,
            };

            SerialPort.Open();
            await Task.Delay(100, ct);

            if (probeBaudRates)
            {
                System.Diagnostics.Debug.WriteLine("Will probe baud rates.");
                await GetBaudRate(ct);
            }
        }

        /// <summary>
        /// Opens the serial port with the specified port name.
        /// Under Windows it's something like COM3. On Linux, it's something like /dev/ttyS0.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task OpenAsync(string portName, CancellationToken ct = default) => OpenAsync(portName, BaudRate.Baud19200, true, ct);

        /// <summary>
        /// Closes serial port communication if open.
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

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
                Close();

            _disposedValue = true;
        }

        #endregion

        #region Fingerprint Reader Protocol

        /// <summary>
        /// Gets the version number of the DSP module.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get DSP version number operation.
        ///  The result of the task contains an instance of <see cref="GetDspVersionNumberResponse"/>.
        /// </returns>
        public Task<GetDspVersionNumberResponse> GetDspVersionNumber(CancellationToken ct = default) =>
            GetResponseAsync<GetDspVersionNumberResponse>(Command.Factory.CreateGetDspVersionNumberCommand(), ct);

        /// <summary>
        /// Makes the module go to sleep and stop processing commands. The only way to bring it back
        /// online is by resetting it.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous sleep operation.
        ///  The result of the task contains an instance of <see cref="Response"/>.
        /// </returns>
        public Task<Response> Sleep(CancellationToken ct = default) =>
            GetResponseAsync<Response>(Command.Factory.CreateSleepCommand(), ct);

        /// <summary>
        /// Gets the baud rate.
        /// This method probes the serial port at different baud rates until communication is correctly established.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get baud rate operation.
        ///  The result of the task contains an instance of <see cref="GetSetBaudRateResponse"/>.
        /// </returns>
        public async Task<GetSetBaudRateResponse> GetBaudRate(CancellationToken ct = default)
        {
            if (SerialPort.IsOpen == false)
            {
                throw new InvalidOperationException(
                    $"Call the {nameof(OpenAsync)} method before attempting communication with the module");
            }

            var portName = SerialPort.PortName;
            var baudRates = Enum.GetValues(typeof(BaudRate)).Cast<BaudRate>().ToArray();
            
            var result = new GetSetBaudRateResponse(
                Command.CreateFixedLengthPayload(OperationCode.ChangeBaudRate, 0, 0, (byte)SerialPort.BaudRate.ToBaudRate()));

            var probeCommand = Command.Factory.CreateGetUserCountCommand();
            var probeResponse = await GetResponseAsync<GetUserCountResponse>(probeCommand, BaudRateProbeTimeout, ct);

            if (probeResponse != null)
                return result;

            foreach (var baudRate in baudRates)
            {
                Close();
                await OpenAsync(portName, baudRate, false, ct);

                probeResponse = await GetResponseAsync<GetUserCountResponse>(probeCommand, BaudRateProbeTimeout, ct);

                if (probeResponse != null)
                {
                    var baudRateResponse = new GetSetBaudRateResponse(
                        Command.CreateFixedLengthPayload(OperationCode.ChangeBaudRate, 0, 0, (byte)baudRate));

                    System.Diagnostics.Debug.WriteLine($"RX: {baudRateResponse}");

                    return baudRateResponse;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the baud rate of the module.
        /// This closes and re-opens the serial port.
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous set baud rate operation.
        ///  The result of the task contains an instance of <see cref="GetSetBaudRateResponse"/>.
        /// </returns>
        public async Task<GetSetBaudRateResponse> SetBaudRate(BaudRate baudRate, CancellationToken ct = default)
        {
            var currentBaudRate = await GetBaudRate(ct);
            if (currentBaudRate.BaudRate == baudRate)
            {
                return new GetSetBaudRateResponse(
                    Command.CreateFixedLengthPayload(OperationCode.ChangeBaudRate, 0, 0, (byte) baudRate));
            }

            var response = 
                await GetResponseAsync<GetSetBaudRateResponse>(Command.Factory.CreateChangeBaudRateCommand(baudRate), ct);

            if (response != null)
            {
                var portName = SerialPort.PortName;
                Close();
                await OpenAsync(portName, baudRate, false, ct);
            }

            return response;
        }

        /// <summary>
        /// Gets the registration mode which specifies if a fingerprint can be registered more than once.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get registration mode operation.
        ///  The result of the task contains an instance of <see cref="GetSetRegistrationModeResponse"/>.
        /// </returns>
        public Task<GetSetRegistrationModeResponse> GetRegistrationMode(CancellationToken ct = default) => 
            GetResponseAsync<GetSetRegistrationModeResponse>(
                Command.Factory.CreateGetSetRegistrationModeCommand(GetSetMode.Get, false), 
                ct);

        /// <summary>
        /// Sets the registration mode. Prohibit repeat disallows registration of the same fingerprint for more than 1 user.
        /// </summary>
        /// <param name="prohibitRepeat">if set to <c>true</c> [prohibit repeat].</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous set registration mode operation.
        ///  The result of the task contains an instance of <see cref="GetSetRegistrationModeResponse"/>.
        /// </returns>
        public Task<GetSetRegistrationModeResponse> SetRegistrationMode(bool prohibitRepeat, CancellationToken ct = default) =>
            GetResponseAsync<GetSetRegistrationModeResponse>(
                Command.Factory.CreateGetSetRegistrationModeCommand(GetSetMode.Set, prohibitRepeat), 
                ct);

        /// <summary>
        /// Registers a fingerprint. You have to call this method 3 times specifying the corresponding iteration 1, 2, or 3.
        /// </summary>
        /// <param name="iteration">The iteration.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userPrivilege">The user privilege.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous add fingerprint operation.
        ///  The result of the task contains an instance of <see cref="AddFingerprintResponse"/>.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// iteration
        /// or
        /// userId
        /// or
        /// userPrivilege.
        /// </exception>
        public Task<AddFingerprintResponse> AddFingerprint(int iteration, int userId, int userPrivilege, CancellationToken ct = default)
        {
            if (iteration < 0 || iteration > 3)
                throw new ArgumentException($"{nameof(iteration)} must be a number between 1 and 3");
            if (userId < 1 || userId > 4095)
                throw new ArgumentException($"{nameof(userId)} must be a number between 1 and 4095");
            if (userPrivilege < 0 || userPrivilege > 3)
                throw new ArgumentException($"{nameof(userPrivilege)} must be a number between 1 and 3");

            var command = Command.Factory.CreateAddFingerprintCommand(
                Convert.ToByte(iteration),
                Convert.ToUInt16(userId), 
                Convert.ToByte(userPrivilege));
            return GetResponseAsync<AddFingerprintResponse>(command, AcquireTimeout, ct);
        }

        /// <summary>
        /// Deletes the specified user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete user operation.
        ///  The result of the task contains an instance of <see cref="Response"/>.
        /// </returns>
        public Task<Response> DeleteUser(int userId, CancellationToken ct = default) =>
            GetResponseAsync<Response>(Command.Factory.CreateDeleteUserCommand(Convert.ToUInt16(userId)), ct);

        /// <summary>
        /// Deletes all users.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous delete all users operation.
        ///  The result of the task contains an instance of <see cref="Response"/>.
        /// </returns>
        public Task<Response> DeleteAllUsers(CancellationToken ct = default) =>
            GetResponseAsync<Response>(Command.Factory.CreateDeleteAllUsersCommand(), AcquireTimeout, ct);

        /// <summary>
        /// Gets the user count.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get user count operation.
        ///  The result of the task contains an instance of <see cref="GetUserCountResponse"/>.
        /// </returns>
        public Task<GetUserCountResponse> GetUserCount(CancellationToken ct = default) =>
            GetResponseAsync<GetUserCountResponse>(Command.Factory.CreateGetUserCountCommand(), ct);

        /// <summary>
        /// Returns a User id after acquiring an image from the sensor. Match 1:N.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous match one to n operation.
        ///  The result of the task contains an instance of <see cref="MatchOneToNResponse"/>.
        /// </returns>
        public Task<MatchOneToNResponse> MatchOneToN(CancellationToken ct = default) =>
            GetResponseAsync<MatchOneToNResponse>(Command.Factory.CreateMatchOneToNCommand(), AcquireTimeout, ct);

        /// <summary>
        /// Acquires an image from the sensor and tests if it matches the supplied user id. Match 1:1.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous match one to one operation.
        ///  The result of the task contains an instance of <see cref="Response"/>.
        /// </returns>
        public Task<Response> MatchOneToOne(int userId, CancellationToken ct = default) =>
            GetResponseAsync<Response>(Command.Factory.CreateMatchOneToOneCommand(Convert.ToUInt16(userId)), AcquireTimeout, ct);

        /// <summary>
        /// Gets the user privilege given its id.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get user privilege operation.
        ///  The result of the task contains an instance of <see cref="GetUserPrivilegeResponse"/>.
        /// </returns>
        public Task<GetUserPrivilegeResponse> GetUserPrivilege(int userId, CancellationToken ct = default) =>
            GetResponseAsync<GetUserPrivilegeResponse>(Command.Factory.CreateGetUserPrivilegeCommand(Convert.ToUInt16(userId)), ct);

        /// <summary>
        /// Gets the matching level.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get matching level operation.
        ///  The result of the task contains an instance of <see cref="GetSetMatchingLevelResponse"/>.
        /// </returns>
        public Task<GetSetMatchingLevelResponse> GetMatchingLevel(CancellationToken ct = default) =>
            GetResponseAsync<GetSetMatchingLevelResponse>(Command.Factory.CreateGetSetMatchingLevelCommand(GetSetMode.Get, 0), ct);

        /// <summary>
        /// Sets the matching level. 0 is the loosest, 9 is the strictest.
        /// </summary>
        /// <param name="matchingLevel">The matching level.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous set matching level operation.
        ///  The result of the task contains an instance of <see cref="GetSetMatchingLevelResponse"/>.
        /// </returns>
        public Task<GetSetMatchingLevelResponse> SetMatchingLevel(int matchingLevel, CancellationToken ct = default) =>
            GetResponseAsync<GetSetMatchingLevelResponse>(Command.Factory.CreateGetSetMatchingLevelCommand(GetSetMode.Set, Convert.ToByte(matchingLevel)), ct);

        /// <summary>
        /// Acquires an image from the sensor and returns the image bytes in grayscale nibbles. This operation is fairly slow.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous adquire image operation.
        ///  The result of the task contains an instance of <see cref="AcquireImageResponse"/>.
        /// </returns>
        public Task<AcquireImageResponse> AcquireImage(CancellationToken ct = default) =>
            GetResponseAsync<AcquireImageResponse>(Command.Factory.CreateAcquireImageCommand(), AcquireTimeout, ct);

        /// <summary>
        /// Acquires an image from the sensor and returns the computed eigenvalues.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous adquire image eigenvalues operation.
        ///  The result of the task contains an instance of <see cref="AcquireImageEigenvaluesResponse"/>.
        /// </returns>
        public Task<AcquireImageEigenvaluesResponse> AcquireImageEigenvalues(CancellationToken ct = default) =>
            GetResponseAsync<AcquireImageEigenvaluesResponse>(Command.Factory.CreateAcquireImageEigenvaluesCommand(), AcquireTimeout, ct);

        /// <summary>
        /// Acquires an image from the sensor and determines if the supplied eigenvalues match. Match 1:1.
        /// </summary>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous match image to eigenvalues operation.
        ///  The result of the task contains an instance of <see cref="Response"/>.
        /// </returns>
        public Task<Response> MatchImageToEigenvalues(byte[] eigenvalues, CancellationToken ct = default) =>
            GetResponseAsync<Response>(Command.Factory.CreateMatchImageToEigenvaluesCommand(eigenvalues), AcquireTimeout, ct);

        /// <summary>
        /// Provides a method to test if the given user id matches the specified eigenvalues. Match 1:1.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous match user to eigenvalues operation.
        ///  The result of the task contains an instance of <see cref="Response"/>.
        /// </returns>
        public Task<Response> MatchUserToEigenvalues(int userId, byte[] eigenvalues, CancellationToken ct = default) =>
            GetResponseAsync<Response>(Command.Factory.CreateMatchUserToEigenvaluesCommand(Convert.ToUInt16(userId), eigenvalues), ct);

        /// <summary>
        /// Finds a user id for the given eigenvalues. Match 1:N.
        /// </summary>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous match eigenvalues to user operation.
        ///  The result of the task contains an instance of <see cref="MatchEigenvaluesToUserResponse"/>.
        /// </returns>
        public Task<MatchEigenvaluesToUserResponse> MatchEigenvaluesToUser(byte[] eigenvalues, CancellationToken ct = default) =>
            GetResponseAsync<MatchEigenvaluesToUserResponse>(Command.Factory.CreateMatchEigenvaluesToUserCommand(eigenvalues), ct);

        /// <summary>
        /// Gets a user's privilege and eigenvalues.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get user properties operation.
        ///  The result of the task contains an instance of <see cref="GetUserPropertiesResponse"/>.
        /// </returns>
        public Task<GetUserPropertiesResponse> GetUserProperties(int userId, CancellationToken ct = default) =>
            GetResponseAsync<GetUserPropertiesResponse>(Command.Factory.CreateGetUserPropertiesCommand(Convert.ToUInt16(userId)), ct);

        /// <summary>
        /// Sets or overwrites a specified user with the given privilege and eigenvalues.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="privilege">The privilege.</param>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous set user properties operation.
        ///  The result of the task contains an instance of <see cref="SetUserPropertiesResponse"/>.
        /// </returns>
        public Task<SetUserPropertiesResponse> SetUserProperties(int userId, int privilege, byte[] eigenvalues, CancellationToken ct = default) =>
            GetResponseAsync<SetUserPropertiesResponse>(Command.Factory.CreateSetUserPropertiesCommand(
                Convert.ToUInt16(userId),
                Convert.ToByte(privilege),
                eigenvalues), ct);

        /// <summary>
        /// Gets the capture timeout. Timeout is between 0 to 255. 0 denotes to wait indefinitely for a capture.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get capture timeout operation.
        ///  The result of the task contains an instance of <see cref="GetSetCaptureTimeoutResponse"/>.
        /// </returns>
        public Task<GetSetCaptureTimeoutResponse> GetCaptureTimeout(CancellationToken ct = default) =>
            GetResponseAsync<GetSetCaptureTimeoutResponse>(Command.Factory.CreateGetSetCaptureTimeoutCommand(GetSetMode.Get, 0), ct);

        /// <summary>
        /// Sets the capture timeout. Timeout must be 0 to 255. 0 denotes to wait indefinitely for a capture.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous set capture timeout operation.
        ///  The result of the task contains an instance of <see cref="GetSetCaptureTimeoutResponse"/>.
        /// </returns>
        public Task<GetSetCaptureTimeoutResponse> SetCaptureTimeout(int timeout, CancellationToken ct = default) =>
            GetResponseAsync<GetSetCaptureTimeoutResponse>(Command.Factory.CreateGetSetCaptureTimeoutCommand(GetSetMode.Set, Convert.ToByte(timeout)), ct);

        /// <summary>
        /// Gets all users and their permissions. It does not retrieve User eigenvalues.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get all users operation.
        ///  The result of the task contains an instance of <see cref="GetAllUsersResponse"/>.
        /// </returns>
        public Task<GetAllUsersResponse> GetAllUsers(CancellationToken ct = default) =>
            GetResponseAsync<GetAllUsersResponse>(Command.Factory.CreateGetAllUsersCommand(), ct);

        #endregion

        #region Read and Write Methods

        /// <summary>
        /// Gets a response with the default timeout.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/>.</param>
        /// <returns>A task that represents the asynchronous get response operation.
        /// The result of the task contains an instance of a response type T.
        /// </returns>
        private Task<T> GetResponseAsync<T>(Command command, CancellationToken ct)
            where T : ResponseBase => GetResponseAsync<T>(command, DefaultTimeout, ct);

        /// <summary>
        /// Given a command, gets a response object asynchronously.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="responseTimeout">The response timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous get response operation.
        /// The result of the task contains an instance of a response type T.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Open.</exception>
        private async Task<T> GetResponseAsync<T>(Command command, TimeSpan responseTimeout, CancellationToken ct)
            where T : ResponseBase
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(OpenAsync)} method before attempting communication");

            var startTime = DateTime.UtcNow;

            var discardedBytes = await FlushReadBufferAsync(ct);
#if DEBUG
            if (discardedBytes.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"RX: Discarded {discardedBytes.Length} bytes: {BitConverter.ToString(discardedBytes).Replace("-", " ")}");
            }
#endif

            await WriteAsync(command.Payload, ct);

            System.Diagnostics.Debug.WriteLine($"TX: {command}");

            var responseBytes = await ReadAsync(responseTimeout, ct);
            if (responseBytes == null || responseBytes.Length <= 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"RX: No response received after {responseTimeout.TotalMilliseconds} ms");

                return null;
            }

            var response = Activator.CreateInstance(typeof(T), responseBytes) as T;

            System.Diagnostics.Debug.WriteLine($"RX: {response}");
            System.Diagnostics.Debug.WriteLine(
                $"Request-Response cycle took {DateTime.UtcNow.Subtract(startTime).TotalMilliseconds} ms");

            return response;
        }

        /// <summary>
        /// Writes data to the serial port asynchronously.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="System.InvalidOperationException">Open.</exception>
        private async Task WriteAsync(byte[] payload, CancellationToken ct)
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(OpenAsync)} method before attempting communication");

            SerialPortDone.Wait(ct);
            SerialPortDone.Reset();

            try
            {
                await SerialPort.WriteAsync(payload, 0, payload.Length, ct);
                await SerialPort.FlushAsync(ct);
            }
            finally
            {
                SerialPortDone.Set();
            }
        }

        /// <summary>
        /// Flushes the serial port read data discarding all bytes in the read buffer.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        private async Task<byte[]> FlushReadBufferAsync(CancellationToken ct)
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                return new byte[] { };

            SerialPortDone.Wait(ct);
            SerialPortDone.Reset();

            try
            {
                var count = 0;
                var buffer = new byte[SerialPort.ReadBufferSize];
                var offset = 0;
                while (SerialPort.BytesToRead > 0)
                {
                    count += await SerialPort.ReadAsync(buffer, offset, buffer.Length, ct);
                    offset += count;
                }

                return buffer.Take(count).ToArray();
            }
            finally
            {
                SerialPortDone.Set();
            }
        }

        /// <summary>
        /// Reads data from the serial port asynchronously with the default timeout.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private Task<byte[]> ReadAsync(CancellationToken ct) => ReadAsync(DefaultTimeout, ct);

        /// <summary>
        /// Reads bytes from the serial port.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="System.InvalidOperationException">Open.</exception>
        private async Task<byte[]> ReadAsync(TimeSpan timeout, CancellationToken ct)
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(OpenAsync)} method before attempting communication");

            SerialPortDone.Wait(ct);
            SerialPortDone.Reset();

            try
            {
                var response = new List<byte>(1024 * 10);
                var expectedBytes = 8;
                var iteration = 0;
                var isVariableLengthResponse = false;
                var largePacketDelayMilliseconds = 0;
                const int largePacketSize = 500;

                var startTime = DateTime.UtcNow;
                var buffer = new byte[SerialPort.ReadBufferSize];

                while (SerialPort.IsOpen && response.Count < expectedBytes && ct.IsCancellationRequested == false)
                {
                    if (SerialPort.BytesToRead > 0)
                    {
                        var readBytes =
                            await SerialPort.ReadAsync(buffer, 0, buffer.Length, ct);

                        response.AddRange(buffer.Skip(0).Take(readBytes));
                        var remainingBytes = expectedBytes - response.Count;
                        startTime = DateTime.UtcNow;

                        // for larger data packets we want to give it a nicer breather
                        if (isVariableLengthResponse && response.Count < expectedBytes &&
                            expectedBytes > largePacketSize)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"RX: Received {readBytes} bytes. Length: {response.Count} of {expectedBytes}; {remainingBytes} remaining - Delay: {largePacketDelayMilliseconds} ms");

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
                                var headerByteCount = new[] {response[2], response[3]}.BigEndianArrayToUInt16();
                                if (headerByteCount > 0)
                                {
                                    expectedBytes = 8 + 3 + headerByteCount;
                                    largePacketDelayMilliseconds =
                                        (int) Math.Max((double) expectedBytes / SerialPort.BaudRate * 1000d, 100d);

                                    System.Diagnostics.Debug.WriteLine(
                                        $"RX: Expected Bytes: {expectedBytes}. Large Packet delay: {largePacketDelayMilliseconds} ms");
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
                        System.Diagnostics.Debug.WriteLine(
                            $"RX: Did not receive enough bytes. Received: {response.Count}  Expected: {expectedBytes}");
                        System.Diagnostics.Debug.WriteLine(
                            $"RX: {BitConverter.ToString(response.ToArray()).Replace("-", " ")}");

                        return null;
                    }
                }

                return response.ToArray();
            }
            finally
            {
                SerialPortDone.Set();
            }
        }

        #endregion
    }
}