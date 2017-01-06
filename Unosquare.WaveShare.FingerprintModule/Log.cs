namespace Unosquare.WaveShare.FingerprintModule
{
    using System;

    /// <summary>
    /// Static log service
    /// TODO: Remove for SWAN?
    /// </summary>
    static internal class Log
    {
        static private readonly object SyncLock = new object();

        static Log()
        {
            //Console.OutputEncoding = Encoding.GetEncoding(437);
        }

        /// <summary>
        /// Writes the specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="charCode">The character code.</param>
        /// <param name="count">The count.</param>
        /// <param name="newLine">if set to <c>true</c> [new line].</param>
        static public void Write(ConsoleColor color, byte charCode, int count, bool newLine)
        {
            lock (SyncLock)
            {
                var priorColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                var bytes = new byte[count];
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = charCode;
                }
                var buffer = Console.OutputEncoding.GetChars(bytes);
                Console.Write(buffer);
                if (newLine) Console.WriteLine();
                Console.ForegroundColor = priorColor;
            }

        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="text">The text.</param>
        static public void WriteLine(ConsoleColor color, string text)
        {
            lock (SyncLock)
            {
                var priorColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = priorColor;
            }
        }

        /// <summary>
        /// Writes the specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="text">The text.</param>
        static public void Write(ConsoleColor color, string text)
        {
            lock (SyncLock)
            {
                var priorColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(text);
                Console.ForegroundColor = priorColor;
            }
        }

        static public void Debug(string text)
        {
            WriteLine(ConsoleColor.Gray, $" {DateTime.Now:HH:mm:ss} DBG >> {text}");
        }

        static public void Trace(string text)
        {
            WriteLine(ConsoleColor.DarkGray, $" {DateTime.Now:HH:mm:ss} TRC >> {text}");
        }

        static public void Warn(string text)
        {
            WriteLine(ConsoleColor.Yellow, $" {DateTime.Now:HH:mm:ss} WRN >> {text}");
        }

        static public void Info(string text)
        {
            WriteLine(ConsoleColor.Cyan, $" {DateTime.Now:HH:mm:ss} INF >> {text}");
        }

        static public void Error(string text)
        {
            WriteLine(ConsoleColor.Red, $" {DateTime.Now:HH:mm:ss} ERR >> {text}");
        }
    }
}