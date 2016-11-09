namespace Unosquare.WaveShare.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a command or request to the Fingerprint reader
    /// </summary>
    /// <seealso cref="Unosquare.WaveShare.FingerprintModule" />
    public partial class Command : MessageBase
    {
        #region Mappings

        internal static readonly Dictionary<OperationCode, MessageLengthCategory> CommandLengthCategories = new Dictionary<OperationCode, MessageLengthCategory>
        {
            { OperationCode.SleepModule, MessageLengthCategory.Fixed },
            { OperationCode.GetSetRegistrationMode, MessageLengthCategory.Fixed },
            { OperationCode.AddFingerprint01, MessageLengthCategory.Fixed },
            { OperationCode.AddFingerprint02, MessageLengthCategory.Fixed },
            { OperationCode.AddFingerprint03, MessageLengthCategory.Fixed },
            { OperationCode.DeleteUser, MessageLengthCategory.Fixed },
            { OperationCode.DeleteAllUsers, MessageLengthCategory.Fixed },
            { OperationCode.GetUserCount, MessageLengthCategory.Fixed },
            { OperationCode.MatchOnteToOne, MessageLengthCategory.Fixed },
            { OperationCode.MatchOneToN, MessageLengthCategory.Fixed },
            { OperationCode.GetUserPrivilege, MessageLengthCategory.Fixed },
            { OperationCode.GetDspVersionNumber, MessageLengthCategory.Fixed },
            { OperationCode.GetSetMatchingLevel, MessageLengthCategory.Fixed },
            { OperationCode.AcquireImage, MessageLengthCategory.Fixed },
            { OperationCode.AcquireImageEigenvalues, MessageLengthCategory.Fixed },
            { OperationCode.GetUserProperties, MessageLengthCategory.Fixed },
            { OperationCode.GetAllUsers, MessageLengthCategory.Fixed },
            { OperationCode.GetSetCaptureTimeout, MessageLengthCategory.Fixed },

            { OperationCode.MatchImageToEigenvalues, MessageLengthCategory.Variable },
            { OperationCode.MatchUserToEigenvalues, MessageLengthCategory.Variable },
            { OperationCode.MatchEigenvaluesToUser, MessageLengthCategory.Variable },
            { OperationCode.SetUserProperties, MessageLengthCategory.Variable },
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        protected Command(OperationCode commandCode)
            : base(MessageType.Request, CommandLengthCategories[commandCode], commandCode)
        {
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates the fixed length payload.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <param name="b3">The b3.</param>
        /// <param name="b4">The b4.</param>
        /// <param name="b5">The b5.</param>
        /// <param name="b6">The b6.</param>
        /// <returns></returns>
        internal static byte[] CreateFixedLengthPayload(OperationCode commandCode, byte b3 = 0, byte b4 = 0, byte b5 = 0, byte b6 = 0)
        {
            var payload = new byte[8] { PayloadDelimiter, (byte)commandCode, b3, b4, b5, b6, 0, PayloadDelimiter, };
            payload[6] = payload.ComputeChecksum();
            return payload;
        }

        /// <summary>
        /// Creates the variable length payload.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="privilege">The privilege.</param>
        /// <param name="eigenvalues">The eigenvalues.</param>
        /// <returns></returns>
        internal static byte[] CreateVariableLengthPayload(OperationCode commandCode, ushort userId, byte privilege, byte[] eigenvalues)
        {
            var length = (Convert.ToUInt16(eigenvalues.Length + 3)).ToBigEndianArray();
            var headerPacket = CreateFixedLengthPayload(commandCode, length[0], length[1]);

            var userIdBytes = (Convert.ToUInt16(userId)).ToBigEndianArray(); 
            var dataPacket = new byte[4 + eigenvalues.Length + 2];
            dataPacket[0] = MessageBase.PayloadDelimiter;
            dataPacket[1] = userIdBytes[0];
            dataPacket[2] = userIdBytes[1];
            dataPacket[3] = privilege;
            Array.Copy(eigenvalues, 0, dataPacket, 4, eigenvalues.Length);
            dataPacket[dataPacket.Length - 2] = dataPacket.ComputeChecksum(1, dataPacket.Length - 3);
            dataPacket[dataPacket.Length - 1] = MessageBase.PayloadDelimiter;

            var fullPayload = new byte[headerPacket.Length + dataPacket.Length];
            Array.Copy(headerPacket, fullPayload, headerPacket.Length);
            Array.Copy(dataPacket, 0, fullPayload, headerPacket.Length, dataPacket.Length);

            return fullPayload;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var contents = GetPayloadString();
            return $"{MessageType,-8} {LengthCategory,10} SZ: {Payload.Length,4} - {OperationCode,10}  - {contents}";
        }

        #endregion

    }

}
