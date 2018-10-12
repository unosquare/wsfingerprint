namespace Unosquare.WaveShare.FingerprintModule
{
    using System;

    partial class Command
    {
        /// <summary>
        /// Contains all command factory methods.
        /// </summary>
        internal static class Factory
        {
            internal static Command CreateSleepCommand()
            {
                var command = new Command(OperationCode.SleepModule);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateGetSetRegistrationModeCommand(GetSetMode mode, bool prohibitRepeatMode)
            {
                var command = new Command(OperationCode.GetSetRegistrationMode);
                command.Payload = CreateFixedLengthPayload(
                    command.OperationCode,
                    0,
                    mode == GetSetMode.Set ? Convert.ToByte(prohibitRepeatMode) : (byte)0,
                    (byte)mode);
                return command;
            }

            internal static Command CreateAddFingerprintCommand(byte iteration, ushort userId, byte privilege)
            {
                // iteration must be between 1 and 3
                // userId must be between 1 and 4095
                // privilege must be between 1 and 3
                var code = OperationCode.AddFingerprint01;
                if (iteration == 2) code = OperationCode.AddFingerprint02;
                else if (iteration == 3) code = OperationCode.AddFingerprint03;

                var command = new Command(code);
                var userIdBytes = userId.ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1], privilege);
                return command;
            }

            internal static Command CreateDeleteUserCommand(ushort userId)
            {
                var command = new Command(OperationCode.DeleteUser);
                var userIdBytes = userId.ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            internal static Command CreateDeleteAllUsersCommand()
            {
                var command = new Command(OperationCode.DeleteAllUsers);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateGetUserCountCommand()
            {
                var command = new Command(OperationCode.GetUserCount);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateMatchOneToOneCommand(ushort userId)
            {
                var command = new Command(OperationCode.MatchOnteToOne);
                var userIdBytes = userId.ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            internal static Command CreateMatchOneToNCommand()
            {
                var command = new Command(OperationCode.MatchOneToN);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateGetUserPrivilegeCommand(ushort userId)
            {
                var command = new Command(OperationCode.GetUserPrivilege);
                var userIdBytes = userId.ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            internal static Command CreateGetDspVersionNumberCommand()
            {
                var command = new Command(OperationCode.GetDspVersionNumber);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateGetSetMatchingLevelCommand(GetSetMode mode, byte level)
            {
                // level must be 0 to 9
                var command = new Command(OperationCode.GetSetMatchingLevel);
                command.Payload = CreateFixedLengthPayload(
                    command.OperationCode,
                    0,
                    mode == GetSetMode.Set ? level : (byte)0,
                    (byte)mode);

                return command;
            }

            internal static Command CreateAcquireImageCommand()
            {
                var command = new Command(OperationCode.AcquireImage);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateAcquireImageEigenvaluesCommand()
            {
                var command = new Command(OperationCode.AcquireImageEigenvalues);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateGetUserPropertiesCommand(ushort userId)
            {
                var command = new Command(OperationCode.GetUserProperties);
                var userIdBytes = userId.ToBigEndianArray();
                command.Payload = CreateFixedLengthPayload(command.OperationCode, userIdBytes[0], userIdBytes[1]);
                return command;
            }

            internal static Command CreateGetAllUsersCommand()
            {
                var command = new Command(OperationCode.GetAllUsers);
                command.Payload = CreateFixedLengthPayload(command.OperationCode);
                return command;
            }

            internal static Command CreateGetSetCaptureTimeoutCommand(GetSetMode mode, byte timeout)
            {
                // level must be 0 to 255
                var command = new Command(OperationCode.GetSetCaptureTimeout);
                command.Payload = CreateFixedLengthPayload(
                    command.OperationCode,
                    0,
                    mode == GetSetMode.Set ? timeout : (byte)0,
                    (byte)mode);

                return command;
            }

            internal static Command CreateMatchImageToEigenvaluesCommand(byte[] eigenvalues)
            {
                var command = new Command(OperationCode.MatchImageToEigenvalues);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, 0, 0, eigenvalues);
                return command;
            }

            internal static Command CreateMatchUserToEigenvaluesCommand(ushort userId, byte[] eigenvalues)
            {
                var command = new Command(OperationCode.MatchUserToEigenvalues);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, userId, 0, eigenvalues);
                return command;
            }

            internal static Command CreateMatchEigenvaluesToUserCommand(byte[] eigenvalues)
            {
                var command = new Command(OperationCode.MatchEigenvaluesToUser);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, 0, 0, eigenvalues);
                return command;
            }

            internal static Command CreateSetUserPropertiesCommand(ushort userId, byte privilege, byte[] eigenvalues)
            {
                var command = new Command(OperationCode.SetUserProperties);
                command.Payload = CreateVariableLengthPayload(command.OperationCode, userId, privilege, eigenvalues);
                return command;
            }

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
