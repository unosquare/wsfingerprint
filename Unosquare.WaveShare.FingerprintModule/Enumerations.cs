namespace Unosquare.WaveShare.FingerprintModule
{
    /// <summary>
    /// Message Type - Command (Request) or Response
    /// </summary>
    public enum MessageType
    {
        Request,
        Response
    }

    /// <summary>
    /// Message Length - Fixed, 8-byte or Variable-length
    /// </summary>
    public enum MessageLengthCategory
    {
        Fixed,
        Variable
    }

    /// <summary>
    /// Message Response Code
    /// </summary>
    public enum MessageResponseCode : byte
    {
        Ok = 0x00,
        Failed = 0x01,
        DbFull = 0x04,
        NoSuchUser = 0x05,
        UserExists = 0x06,
        FpExists = 0x07,
        TimedOut = 0x08,
    }

    /// <summary>
    /// Operation Codes
    /// </summary>
    public enum OperationCode : byte
    {
        Invalid = 0x00,
        SleepModule = 0x2C,
        GetSetRegistrationMode = 0x2D,
        AddFingerprint01 = 0x01,
        AddFingerprint02 = 0x02,
        AddFingerprint03 = 0x03,
        DeleteUser = 0x04,
        DeleteAllUsers = 0x05,
        GetUserCount = 0x09,
        MatchOnteToOne = 0x0B,
        MatchOneToN = 0x0C,
        GetUserPrivilege = 0x0A,
        GetDspVersionNumber = 0x26,
        GetSetMatchingLevel = 0x28,
        AcquireImage = 0x24,
        AcquireImageEigenvalues = 0x23,
        GetUserProperties = 0x31,
        GetAllUsers = 0x2B,
        GetSetCaptureTimeout = 0x2E,

        MatchImageToEigenvalues = 0x44,
        MatchUserToEigenvalues = 0x42,
        MatchEigenvaluesToUser = 0x43,
        SetUserProperties = 0x41,
    }

    /// <summary>
    /// The mode of the operation. Getter or Setter
    /// </summary>
    internal enum GetSetMode : byte
    {
        Set = 0,
        Get = 1,
    }

}
