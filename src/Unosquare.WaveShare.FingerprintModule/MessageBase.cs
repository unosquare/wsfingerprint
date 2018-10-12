namespace Unosquare.WaveShare.FingerprintModule
{
    using System;
    using System.Linq;

    /// <summary>
    /// Represents a Message (either a command or a response).
    /// </summary>
    public abstract class MessageBase
    {
        /// <summary>
        /// The payload delimiter - All messages start and end with this byte constant.
        /// </summary>
        internal const byte PayloadDelimiter = 0xF5;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBase" /> class.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="lengthCategory">The length category.</param>
        /// <param name="opCode">The op code.</param>
        protected MessageBase(MessageType messageType, MessageLengthCategory lengthCategory, OperationCode opCode)
        {
            LengthCategory = lengthCategory;
            MessageType = messageType;
            OperationCode = opCode;
        }

        /// <summary>
        /// Gets the payload in a HEX string representation.
        /// </summary>
        /// <returns></returns>
        internal string GetPayloadString()
        {
            byte[] visiblePayload = null;
            var contents = "(Empty)";

            if (Payload != null)
            {
                visiblePayload = new byte[Math.Min(Payload.Length, 8)];

                Array.Copy(Payload, visiblePayload, visiblePayload.Length);
                contents = BitConverter.ToString(Payload.Skip(0).Take(Math.Min(9, Payload.Length)).ToArray()).Replace("-", " ");

                if (Payload.Length > 8)
                    contents = contents + " (...)";
            }

            return contents;
        }

        /// <summary>
        /// Gets the length category of this message (8-byte or variable).
        /// </summary>
        public MessageLengthCategory LengthCategory { get; protected set; }

        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        public MessageType MessageType { get; protected set; }

        /// <summary>
        /// Gets the command or operation code.
        /// </summary>
        public OperationCode OperationCode { get; private set; }

        /// <summary>
        /// Gets the byte payload. That is, the byte array contents of this message.
        /// </summary>
        public byte[] Payload { get; protected set; }

        /// <summary>
        /// Gets the checksum byte of the payload.
        /// </summary>
        public byte Checksum { get { return (Payload != null && Payload.Length >= 8) ? Payload[6] : (byte)0; } }
    }

}
