namespace Unosquare.WaveShare.FingerprintModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A base class representing response messages
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.MessageBase" />
    public abstract class ResponseBase : MessageBase
    {

        #region Mappings

        internal static readonly Dictionary<OperationCode, MessageLengthCategory> ResponseLengthCategories = new Dictionary<OperationCode, MessageLengthCategory>
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
            { OperationCode.GetDspVersionNumber, MessageLengthCategory.Variable },
            { OperationCode.GetSetMatchingLevel, MessageLengthCategory.Fixed },
            { OperationCode.AcquireImage, MessageLengthCategory.Variable },
            { OperationCode.AcquireImageEigenvalues, MessageLengthCategory.Variable },
            { OperationCode.GetUserProperties, MessageLengthCategory.Variable },
            { OperationCode.GetAllUsers, MessageLengthCategory.Variable },
            { OperationCode.GetSetCaptureTimeout, MessageLengthCategory.Fixed },

            { OperationCode.MatchImageToEigenvalues, MessageLengthCategory.Fixed },
            { OperationCode.MatchUserToEigenvalues, MessageLengthCategory.Fixed },
            { OperationCode.MatchEigenvaluesToUser, MessageLengthCategory.Fixed },
            { OperationCode.SetUserProperties, MessageLengthCategory.Fixed },
        };

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBase"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public ResponseBase(byte[] payload)
            : base(MessageType.Response, ResponseLengthCategories[(OperationCode)payload[1]], (OperationCode)payload[1])
        {
            Payload = payload;

            ResponseCode = (MessageResponseCode)payload[4];
            DataPacketLength = -1;
            if (LengthCategory == MessageLengthCategory.Variable)
            {
                DataPacketLength = (new byte[] { payload[2], payload[3] }).BigEndianArrayToUInt16();
            }


            if (payload.Length > 8)
            {
                var dataPacket = new byte[payload.Length - 8];
                Array.Copy(payload, 8, dataPacket, 0, dataPacket.Length);
                DataPacket = dataPacket;
            }
        }

        public MessageResponseCode ResponseCode { get; protected set; }

        public virtual bool IsSuccessful { get { return ResponseCode == MessageResponseCode.Ok; } }

        public int DataPacketLength { get; protected set; }

        public byte[] DataPacket { get; protected set; }

        public byte[] GetBareDataPacket()
        {
            if (DataPacket == null || DataPacket.Length <= 3) return new byte[0];

            return DataPacket.Skip(1).Take(DataPacket.Length - 3).ToArray();
        }

        public override string ToString()
        {
            var contents = GetPayloadString();
            return $"{MessageType,-8} {LengthCategory,10} SZ: {Payload.Length,4} - {OperationCode,10}  - {(IsSuccessful ? MessageResponseCode.Ok : ResponseCode),10}, ({DataPacketLength}b)";
        }

    }

}
