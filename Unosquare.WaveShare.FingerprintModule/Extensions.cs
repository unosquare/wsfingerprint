﻿namespace Unosquare.WaveShare.FingerprintModule
{
    using System;

    /// <summary>
    /// Extension methods
    /// </summary>
    internal static class Extensions
    {

        /// <summary>
        /// Converts an unsigned 16-bit integer to a Big Endian array of bytes
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns></returns>
        internal static byte[] ToBigEndianArray(this ushort number)
        {
            var result = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(result);
            }

            return result;
        }

        /// <summary>
        /// Converts a big endian array to an unsigned 16-bit integer
        /// </summary>
        /// <param name="bigEndianArray">The big endian array.</param>
        /// <returns></returns>
        internal static ushort BigEndianArrayToUInt16(this byte[] bigEndianArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bigEndianArray);
            }

            return BitConverter.ToUInt16(bigEndianArray, 0);
        }

        /// <summary>
        /// Computes the checksum byte of the given payload by XORing bytes 2 to 6
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">payload</exception>
        internal static byte ComputeChecksum(this byte[] payload, int startIndex = 1, int endIndex = 5)
        {
            if (payload == null || payload.Length < endIndex + 1)
                throw new ArgumentException($"'{nameof(payload)}' hast to be at least {endIndex + 1} bytes long.");

            byte checksum = payload[startIndex];
            for (var i = startIndex + 1; i <= endIndex; i++)
            {
                checksum = (byte)(checksum ^ payload[i]);
            }

            return checksum;
        }
    }
}