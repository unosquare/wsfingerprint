namespace Unosquare.WaveShare.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Static log service
    /// </summary>
    static internal class Log
    {
        static private readonly object SyncLock = new object();

        /// <summary>
        /// Prints the current code page.
        /// </summary>
        static public void PrintCurrentCodePage()
        {
            Console.WriteLine("Output Encoding: " + Console.OutputEncoding.ToString());
            for (byte b = 0; b < byte.MaxValue; b++)
            {
                char c = Console.OutputEncoding.GetChars(new byte[] { b })[0];
                switch (b)
                {
                    case 8: // Backspace
                    case 9: // Tab
                    case 10: // Line feed
                    case 13: // Carriage return
                        c = '.';
                        break;
                }

                Console.Write("{0:000} {1}   ", b, c);

                // 7 is a beep -- Console.Beep() also works
                if (b == 7) Console.Write(" ");

                if ((b + 1) % 8 == 0)
                    Console.WriteLine();
            }
            Console.WriteLine();
        }

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

        /// <summary>
        /// Represents a Table to print in console
        /// </summary>
        static public class Table
        {
            /// <summary>
            /// Gets or sets the color of the border.
            /// </summary>
            /// <value>
            /// The color of the border.
            /// </value>
            static public ConsoleColor BorderColor { get; set; } = ConsoleColor.DarkGreen;

            static public void Vertical()
            {
                Log.Write(BorderColor, 179, 1, false);
            }

            static public void RightTee()
            {
                Log.Write(BorderColor, 180, 1, false);
            }

            static public void TopRight()
            {
                Log.Write(BorderColor, 191, 1, false);
            }

            static public void BottomLeft()
            {
                Log.Write(BorderColor, 192, 1, false);
            }

            static public void BottomTee()
            {
                Log.Write(BorderColor, 193, 1, false);
            }

            static public void TopTee()
            {
                Log.Write(BorderColor, 194, 1, false);
            }

            static public void LeftTee()
            {
                Log.Write(BorderColor, 195, 1, false);
            }

            static public void Horizontal(int length)
            {
                Log.Write(BorderColor, 196, length, false);
            }

            static public void Tee()
            {
                Log.Write(BorderColor, 197, 1, false);
            }

            static public void BottomRight()
            {
                Log.Write(BorderColor, 217, 1, false);
            }

            static public void TopLeft()
            {
                Log.Write(BorderColor, 218, 1, false);
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

        /// <summary>
        /// Reads the key.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="preventEcho">if set to <c>true</c> [prevent echo].</param>
        /// <returns></returns>
        static public ConsoleKeyInfo ReadKey(string prompt, bool preventEcho)
        {
            Write(ConsoleColor.White, $" {DateTime.Now:HH:mm:ss} USR << {prompt} ");
            var input = Console.ReadKey(true);
            var echo = preventEcho ? string.Empty : input.Key.ToString();
            Console.WriteLine(echo);
            return input;
        }

        /// <summary>
        /// Reads the number.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="defaultNumber">The default number.</param>
        /// <returns></returns>
        static public int ReadNumber(string prompt, int defaultNumber)
        {
            Write(ConsoleColor.White, $" {DateTime.Now:HH:mm:ss} USR << {prompt} (default is {defaultNumber}): ");
            var input = Console.ReadLine();
            var parsedInt = defaultNumber;
            if (int.TryParse(input, out parsedInt) == false)
            {
                parsedInt = defaultNumber;
            }

            return parsedInt;
        }

        /// <summary>
        /// Reads the prompt.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="options">The options.</param>
        /// <param name="anyKeyOption">Any key option.</param>
        /// <returns></returns>
        static public ConsoleKeyInfo ReadPrompt(string title, Dictionary<ConsoleKey, string> options, string anyKeyOption)
        {
            var textColor = ConsoleColor.White;
            var lineLength = Console.BufferWidth;
            var lineAlign = -(lineLength - 2);
            var textFormat = "{0," + lineAlign.ToString() + "}";

            { // Top border
                Table.TopLeft();
                Table.Horizontal(-lineAlign);
                Table.TopRight();
            }

            { // Title
                Table.Vertical();
                var titleText = string.Format(textFormat,
                    string.IsNullOrWhiteSpace(title) ?
                        $" Select an option from the list below." :
                        $" {title}");
                Write(textColor, titleText);
                Table.Vertical();
            }

            { // Title Bottom
                Table.LeftTee();
                Table.Horizontal(lineLength - 2);
                Table.RightTee();
            }

            // Options
            foreach (var kvp in options)
            {
                Table.Vertical();
                Write(textColor, string.Format(textFormat,
                    $"    {"[ " + kvp.Key.ToString() + " ]",-10}  {kvp.Value}"));
                Table.Vertical();
            }

            // Any Key Options
            if (string.IsNullOrWhiteSpace(anyKeyOption) == false)
            {
                Table.Vertical();
                Write(ConsoleColor.Gray, string.Format(textFormat, " "));
                Table.Vertical();

                Table.Vertical();
                Write(ConsoleColor.Gray, string.Format(textFormat,
                    $"    {" ",-10}  {anyKeyOption}"));
                Table.Vertical();
            }

            var inputLeft = 12;
            var inputTop = Console.CursorTop - 1;

            { // Input
                Table.LeftTee();
                Table.Horizontal(lineLength - 2);
                Table.RightTee();

                Table.Vertical();
                Write(ConsoleColor.Green, string.Format(textFormat,
                    $" Option: "));
                inputTop = Console.CursorTop;
                Table.Vertical();

                Table.BottomLeft();
                Table.Horizontal(lineLength - 2);
                Table.BottomRight();
            }

            var currentTop = Console.CursorTop;
            var currentLeft = Console.CursorLeft;

            Console.SetCursorPosition(inputLeft, inputTop);
            var userInput = Console.ReadKey(true);
            Write(ConsoleColor.Gray, userInput.Key.ToString());

            Console.SetCursorPosition(currentLeft, currentTop);
            return userInput;
        }

    }
}
