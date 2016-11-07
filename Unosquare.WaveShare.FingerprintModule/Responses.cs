﻿namespace Unosquare.WaveShare.FingerprintModule
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    #region Response Message Classes

    /// <summary>
    /// Represents the most basic response there is. That is, an 8-byte payload with a result code.
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class BasicResponse : ResponseBase { public BasicResponse(byte[] payload) : base(payload) { } }

    /// <summary>
    /// Get DSP Version Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetDspVersionNumberResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetDspVersionNumberResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GetDspVersionNumberResponse(byte[] payload) : base(payload)
        {
            Version = Encoding.ASCII.GetString(GetBareDataPacket());
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + " Ver.: " + Version;
        }
    }

    /// <summary>
    /// Get or Set Registration Mode Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetSetRegistrationModeResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSetRegistrationModeResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GetSetRegistrationModeResponse(byte[] payload) : base(payload)
        {
            ProhibitRepeat = payload[3] == 1 ? true : false;
        }

        /// <summary>
        /// Gets a value indicating whether fingerprints must be unique.
        /// In other words, no 2 users have the same fingerprint
        /// </summary>
        /// <value>
        ///   <c>true</c> if [prohibit repeat]; otherwise, <c>false</c>.
        /// </value>
        public bool ProhibitRepeat { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + " Prohibit Repeat: " + ProhibitRepeat;
        }
    }

    /// <summary>
    /// Add Fingerprint Response. This includes repsonses for all 3 iterations 1, 2, and 3
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class AddFingerprintResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddFingerprintResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public AddFingerprintResponse(byte[] payload) : base(payload)
        {
            Iteration = payload[1];
        }

        /// <summary>
        /// Gets the iteration of the fingerprint acquisition
        /// 
        /// </summary>
        public int Iteration { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + " Iteration: " + Iteration;
        }
    }

    /// <summary>
    /// Get User Count Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetUserCountResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetUserCountResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GetUserCountResponse(byte[] payload) : base(payload)
        {
            UserCount = (new byte[] { payload[2], payload[3] }).BigEndianArrayToUInt16();
        }

        /// <summary>
        /// Gets the number of users registered in the module's database.
        /// </summary>
        public int UserCount { get; private set; }
        public override string ToString()
        {
            return base.ToString() + $" User Count: {UserCount}";
        }
    }

    /// <summary>
    /// Match 1 to N Response. Contains the User Id and the corresponding privilege
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class MatchOneToNResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatchOneToNResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public MatchOneToNResponse(byte[] payload) : base(payload)
        {
            UserId = (new byte[] { payload[2], payload[3] }).BigEndianArrayToUInt16();
            UserPrivilege = payload[4];
        }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Gets the user privilege.
        /// </summary>
        public int UserPrivilege { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the match was successful
        /// </summary>
        public override bool IsSuccessful
        {
            get
            {
                return UserId > 0 && ResponseCode != MessageResponseCode.TimedOut && ResponseCode != MessageResponseCode.NoSuchUser;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" Success: {IsSuccessful} User: {UserId}, Priv: {UserPrivilege}";
        }
    }

    /// <summary>
    /// Get User Privilege Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetUserPrivilegeResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetUserPrivilegeResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GetUserPrivilegeResponse(byte[] payload) : base(payload)
        {
            UserPrivilege = payload[4];
        }

        /// <summary>
        /// Gets or sets the user privilege.
        /// </summary>
        public int UserPrivilege { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the response is successful
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is successful; otherwise, <c>false</c>.
        /// </value>
        public override bool IsSuccessful
        {
            get
            {
                return ResponseCode != MessageResponseCode.NoSuchUser;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" Privilege: {(IsSuccessful ? UserPrivilege.ToString() : "No such user")}";
        }
    }

    /// <summary>
    /// Get or Set Matching Level Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetSetMatchingLevelResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSetMatchingLevelResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GetSetMatchingLevelResponse(byte[] payload) : base(payload)
        {
            MatchingLevel = payload[3];
        }

        /// <summary>
        /// Gets the matching level.
        /// </summary>
        public int MatchingLevel { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" Matching Level: {MatchingLevel}";
        }
    }

    /// <summary>
    /// Acquire Image Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class AcquireImageResponse : ResponseBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="AcquireImageResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public AcquireImageResponse(byte[] payload) : base(payload)
        {
            if (IsSuccessful)
            {
                ImageRawData = GetBareDataPacket();
            }

        }

        /// <summary>
        /// Gets the image raw data.
        /// </summary>
        public byte[] ImageRawData { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" Image Size: {(IsSuccessful ? 0 : ImageRawData.Length)}";
        }

    }

    /// <summary>
    /// Acquire Image Eigenvalues Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class AcquireImageEigenvaluesResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AcquireImageEigenvaluesResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public AcquireImageEigenvaluesResponse(byte[] payload) : base(payload)
        {
            if (IsSuccessful)
            {
                var bareDataPacket = GetBareDataPacket();
                Eigenvalues = bareDataPacket.Skip(3).Take(bareDataPacket.Length - 3).ToArray();
            }
        }

        /// <summary>
        /// Gets the eigenvalues.
        /// </summary>
        public byte[] Eigenvalues { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" Eigenvalues Size: {(IsSuccessful ? 0 : Eigenvalues.Length)}";
        }
    }

    /// <summary>
    /// Match Eigenvalues to User Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class MatchEigenvaluesToUserResponse : ResponseBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchEigenvaluesToUserResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public MatchEigenvaluesToUserResponse(byte[] payload)
            : base(payload)
        {
            if (IsSuccessful)
            {
                UserId = (new byte[] { payload[2], payload[3] }).BigEndianArrayToUInt16();
            }
        }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" Matched User Id: {UserId}";
        }
    }

    /// <summary>
    /// Get User Properties Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetUserPropertiesResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetUserPropertiesResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GetUserPropertiesResponse(byte[] payload) : base(payload)
        {
            if (IsSuccessful)
            {
                var dataPacket = GetBareDataPacket();
                Eigenvalues = dataPacket.Skip(3).Take(dataPacket.Length - 3).ToArray();
                UserId = (new byte[] { dataPacket[0], dataPacket[1] }).BigEndianArrayToUInt16();
                UserPrivilege = dataPacket[2];
            }
        }

        /// <summary>
        /// Gets the eigenvalues.
        /// </summary>
        public byte[] Eigenvalues { get; private set; }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Gets the user privilege.
        /// </summary>
        public int UserPrivilege { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" User Id: {UserId}  Privilege: {UserPrivilege}  Eigenvalues: {Eigenvalues?.Length}";
        }
    }

    /// <summary>
    /// Set User Properties Response - Contains the User Id
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class SetUserPropertiesResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetUserPropertiesResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public SetUserPropertiesResponse(byte[] payload) : base(payload)
        {
            UserId = (new byte[] { payload[2], payload[3] }).BigEndianArrayToUInt16();
        }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" User: {UserId}";
        }
    }

    /// <summary>
    /// Get or Set Capture Timeout Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetSetCaptureTimeoutResponse : ResponseBase
    {
        public GetSetCaptureTimeoutResponse(byte[] payload) : base(payload)
        {
            CaptureTimeout = payload[3];
        }

        /// <summary>
        /// Gets the capture timeout.
        /// </summary>
        public int CaptureTimeout { get; private set; }

        public override string ToString()
        {
            return base.ToString() + $" Capture Timeout: {CaptureTimeout}";
        }
    }

    /// <summary>
    /// Get All Users Response
    /// </summary>
    /// <seealso cref="Unosquare.WaveSharePrintReader.Driver.ResponseBase" />
    public sealed class GetAllUsersResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllUsersResponse"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GetAllUsersResponse(byte[] payload) : base(payload)
        {
            Users = new Dictionary<int, int>();
            if (IsSuccessful == false) return;


            var dataPacket = GetBareDataPacket();
            var userCount = (new byte[] { dataPacket[0], dataPacket[1] }).BigEndianArrayToUInt16();
            if (userCount == 0) return;
            for (int offset = 2; offset < dataPacket.Length; offset += 3)
            {
                var userId = (new byte[] { dataPacket[offset], dataPacket[offset + 1] }).BigEndianArrayToUInt16();
                var privilege = dataPacket[offset + 2];
                Users[userId] = privilege;
            }
        }

        /// <summary>
        /// Gets the users. The Keys are the user Ids. The values are their privileges.
        /// </summary>
        public Dictionary<int, int> Users { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + $" Users: {Users?.Count}";
        }
    }

    #endregion
}