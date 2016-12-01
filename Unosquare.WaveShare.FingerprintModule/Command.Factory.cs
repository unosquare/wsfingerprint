namespace Unosquare.WaveShare.FingerprintModule
{
    using System;

    partial class Command
    {
        /// <summary>
        /// Contains all command factory methods
        /// </summary>
        internal static class Factory
        {
            /// <summary>
            /// Creates the sleep command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateSleepCommand()
            {
                var command = new Command(OperationCode.SleepModule);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the get set registration mode command.
            /// </summary>
            /// <param name="mode">The mode.</param>
            /// <param name="prohibitRepeatMode">if set to <c>true</c> [prohibit repeat mode].</param>
            /// <returns></returns>
            internal static Command CreateGetSetRegistrationModeCommand(GetSetMode mode, bool prohibitRepeatMode)
            {
                var command = new Command(OperationCode.GetSetRegistrationMode);
                command.Payload = CreateFixedLengthPayload(command.OperationCode, 0,
                    mode == GetSetMode.Set ? Convert.ToByte(prohibitRepeatMode) : (byte)0, (byte)mode);
                return command;
            }

            /// <summary>
            /// Creates the add fingerprint command.
            /// </summary>
            /// <param name="iteration">The iteration.</param>
            /// <param name="userId">The user identifier.</param>
            /// <param name="privilege">The privilege.</param>
            /// <returns></returns>
            internal static Command CreateAddFingerprintCommand(byte iteration, ushort userId, byte privilege)
            {
                // iteration must be between 1 and 3
                // userId must be between 1 and 4095
                // privilege must be between 1 and 3

                var code = OperationCode.AddFingerprint01;
                if (iteration == 2) code = OperationCode.AddFingerprint02;
                else if (iteration == 3) code = OperationCode.AddFingerprint03;

                var command = new Command(code);
                var userIdBytes = (userId).ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1], privilege);
                return command;
            }

            /// <summary>
            /// Creates the delete user command.
            /// </summary>
            /// <param name="userId">The user identifier.</param>
            /// <returns></returns>
            internal static Command CreateDeleteUserCommand(ushort userId)
            {
                var command = new Command(OperationCode.DeleteUser);
                var userIdBytes = (userId).ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            /// <summary>
            /// Creates the delete all users command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateDeleteAllUsersCommand()
            {
                var command = new Command(OperationCode.DeleteAllUsers);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the get user count command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateGetUserCountCommand()
            {
                var command = new Command(OperationCode.GetUserCount);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the match one to one command.
            /// </summary>
            /// <param name="userId">The user identifier.</param>
            /// <returns></returns>
            internal static Command CreateMatchOneToOneCommand(ushort userId)
            {
                var command = new Command(OperationCode.MatchOnteToOne);
                var userIdBytes = (userId).ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            /// <summary>
            /// Creates the match one to n command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateMatchOneToNCommand()
            {
                var command = new Command(OperationCode.MatchOneToN);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the get user privilege command.
            /// </summary>
            /// <param name="userId">The user identifier.</param>
            /// <returns></returns>
            internal static Command CreateGetUserPrivilegeCommand(ushort userId)
            {
                var command = new Command(OperationCode.GetUserPrivilege);
                var userIdBytes = (userId).ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            /// <summary>
            /// Creates the get DSP version number command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateGetDspVersionNumberCommand()
            {
                var command = new Command(OperationCode.GetDspVersionNumber);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the get set matching level command.
            /// </summary>
            /// <param name="mode">The mode.</param>
            /// <param name="level">The level.</param>
            /// <returns></returns>
            internal static Command CreateGetSetMatchingLevelCommand(GetSetMode mode, byte level)
            {
                // level must be 0 to 9
                var command = new Command(OperationCode.GetSetMatchingLevel);
                command.Payload = CreateFixedLengthPayload(command.OperationCode, 0,
                    mode == GetSetMode.Set ? level : (byte)0, (byte)mode);
                return command;
            }

            /// <summary>
            /// Creates the acquire image command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateAcquireImageCommand()
            {
                var command = new Command(OperationCode.AcquireImage);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the acquire image eigenvalues command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateAcquireImageEigenvaluesCommand()
            {
                var command = new Command(OperationCode.AcquireImageEigenvalues);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the get user properties command.
            /// </summary>
            /// <param name="userId">The user identifier.</param>
            /// <returns></returns>
            internal static Command CreateGetUserPropertiesCommand(ushort userId)
            {
                var command = new Command(OperationCode.GetUserProperties);
                var userIdBytes = (userId).ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            /// <summary>
            /// Creates the get all users command.
            /// </summary>
            /// <returns></returns>
            internal static Command CreateGetAllUsersCommand()
            {
                var command = new Command(OperationCode.GetAllUsers);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            /// <summary>
            /// Creates the get set capture timeout command.
            /// </summary>
            /// <param name="mode">The mode.</param>
            /// <param name="timeout">The timeout.</param>
            /// <returns></returns>
            internal static Command CreateGetSetCaptureTimeoutCommand(GetSetMode mode, byte timeout)
            {
                // level must be 0 to 255
                var command = new Command(OperationCode.GetSetCaptureTimeout);
                command.Payload = CreateFixedLengthPayload(command.OperationCode, 0,
                    mode == GetSetMode.Set ? timeout : (byte)0, (byte)mode);
                return command;
            }

            /// <summary>
            /// Creates the match image to eigenvalues command.
            /// </summary>
            /// <param name="eigenvalues">The eigenvalues.</param>
            /// <returns></returns>
            internal static Command CreateMatchImageToEigenvaluesCommand(byte[] eigenvalues)
            {
                var command = new Command(OperationCode.MatchImageToEigenvalues);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, 0, 0, eigenvalues);
                return command;
            }

            /// <summary>
            /// Creates the match user to eigenvalues command.
            /// </summary>
            /// <param name="userId">The user identifier.</param>
            /// <param name="eigenvalues">The eigenvalues.</param>
            /// <returns></returns>
            internal static Command CreateMatchUserToEigenvaluesCommand(ushort userId, byte[] eigenvalues)
            {
                var command = new Command(OperationCode.MatchUserToEigenvalues);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, userId, 0, eigenvalues);
                return command;
            }

            /// <summary>
            /// Creates the match eigenvalues to user command.
            /// </summary>
            /// <param name="eigenvalues">The eigenvalues.</param>
            /// <returns></returns>
            internal static Command CreateMatchEigenvaluesToUserCommand(byte[] eigenvalues)
            {
                var command = new Command(OperationCode.MatchEigenvaluesToUser);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, 0, 0, eigenvalues);
                return command;
            }

            /// <summary>
            /// Creates the set user properties command.
            /// </summary>
            /// <param name="userId">The user identifier.</param>
            /// <param name="privilege">The privilege.</param>
            /// <param name="eigenvalues">The eigenvalues.</param>
            /// <returns></returns>
            internal static Command CreateSetUserPropertiesCommand(ushort userId, byte privilege, byte[] eigenvalues)
            {
                var command = new Command(OperationCode.SetUserProperties);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, userId, privilege, eigenvalues);
                return command;
            }

            /// <summary>
            /// Creates the change baud rate command.
            /// </summary>
            /// <param name="newBaudRate">The new baud rate.</param>
            /// <returns></returns>
            internal static Command CreateChangeBaudRateCommand(BaudRate newBaudRate)
            {
                var command = new Command(OperationCode.ChangeBaudRate);
                var baudRateByte = (byte)newBaudRate;
                command.Payload = CreateFixedLengthPayload(command.OperationCode, 0, 0, baudRateByte, 0);
                return command;
            }

        }
    }
}
