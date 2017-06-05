namespace Unosquare.WaveShare.FingerprintModule.Sample
{
    using Swan;
    using System;
    using System.Collections.Generic;
#if NET452
    using System.IO.Ports;
#else
    
#endif
    using System.Linq;
    using System.Threading.Tasks;
    
    class Program
    {
        #region Action Options

        private static readonly Dictionary<ConsoleKey, string> ModuleActionOptions = new Dictionary<ConsoleKey, string>
        {
            // Module Control Items
            {ConsoleKey.V, "MODULE   - Get DSP Version"},
            {ConsoleKey.M, "MODULE   - Get Fingerprint Registration Mode"},
            {ConsoleKey.R, "MODULE   - Set Fingerprint Registration Mode"},
            {ConsoleKey.T, "MODULE   - Get Capture Timeout"},
            {ConsoleKey.G, "MODULE   - Set Capture Timeout"},
            {ConsoleKey.L, "MODULE   - Get Fingerprint Matching Level"},
            {ConsoleKey.K, "MODULE   - Set Fingerprint Matching Level"},
            {ConsoleKey.B, "MODULE   - Change the Baud Rate"},
            {ConsoleKey.S, "MODULE   - Sleep (Warning: Module requires reset)"},
        };

        private static readonly Dictionary<ConsoleKey, string> UsersActionOptions = new Dictionary<ConsoleKey, string>
        {
            // User Control Items
            {ConsoleKey.C, "USERS    - Get User Count"},
            {ConsoleKey.J, "USERS    - List All Users and Privileges"},
            {ConsoleKey.A, "USERS    - Register a New User with a Fingerprint"},
            {ConsoleKey.P, "USERS    - Get a User's Privilege"},
            {ConsoleKey.U, "USERS    - Get a User's Privilege and Eigenvalues"},
            {ConsoleKey.Y, "USERS    - Create a new User providing an Id, Privilege and Eigenvalues"},
            {ConsoleKey.W, "USERS    - Delete a User"},
            {ConsoleKey.Z, "USERS    - Delete all Users (Warning: This deletes the entire database)"},
        };

        private static readonly Dictionary<ConsoleKey, string> MatchingActionOptions = new Dictionary
            <ConsoleKey, string>
            {
                // Matching Items
                {ConsoleKey.F1, "MATCHING - Test if a User Id Matches an Acquired Image (1:1)"},
                {ConsoleKey.F2, "MATCHING - Get a User Id given an Acquired Image (1:N)"},
                {ConsoleKey.F3, "MATCHING - Test if an Acquired Image matches the provided Eigenvalues (1:1)"},
                {ConsoleKey.F4, "MATCHING - Test if a given User ,atches the supplied Eigenvalues (1:1)"},
                {ConsoleKey.F5, "MATCHING - Get a User Id given an array with Eigenvalues (1:N)"},
                {ConsoleKey.F6, "MATCHING - Acquire Image"},
                {ConsoleKey.F7, "MATCHING - Acquire Image Eigenvalues"},
            };


        private static readonly Dictionary<ConsoleKey, string> ActionOptions = new Dictionary<ConsoleKey, string>
        {
            // Module COntrol Items
            {ConsoleKey.Q, "MODULE   - Module Menu"},
            {ConsoleKey.W, "USERS    - Users Menu"},
            {ConsoleKey.E, "MATCHING - Matching Menu"},
        };

        #endregion

        static string PromptForSerialPort()
        {
#if NET452
            var baseChar = 65;
            var portNames = SerialPort.GetPortNames().ToDictionary(p => (ConsoleKey) baseChar++, v => v);
            var portName = string.Empty;

            if (portNames.Any() == false)
            {
                "There is not serial ports connected, please check your hardware".Error();
                return string.Empty;
            }

            if (portNames.Count == 1)
            {
                portName = portNames.First().Value;
            }
            else
            {
                var portSelection = "Select Port to Open".ReadPrompt(portNames, "Exit this program");

                if (portNames.ContainsKey(portSelection.Key))
                    portName = portNames[portSelection.Key];
                else
                    return string.Empty;
            }

            return portName;
#else
            "Select Port to Open:".Info();
            return Terminal.ReadLine();
#endif
        }

        static void Main(string[] args)
        {
            var wiringPiMode = args.Any();
            var portName = "/dev/ttyS0"; //PromptForSerialPort();
            

            if (string.IsNullOrEmpty(portName))
                return;
            
            using (var reader = new FingerprintReader())
            {
                $"Opening port '{portName}' . . .".Info();
                reader.Open(portName, BaudRate.Baud9600, true);

                var t = Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        #region Main Menu

                        ConsoleKeyInfo selectedOption;
                        var menuOption = "Select an option".ReadPrompt(ActionOptions, "Exit this program");
                        if (menuOption.Key == ConsoleKey.Q)
                        {
                            selectedOption = "Select an option".ReadPrompt(ModuleActionOptions, "Exit this prompt");
                        }
                        else if (menuOption.Key == ConsoleKey.W)
                        {
                            selectedOption = "Select an option".ReadPrompt(UsersActionOptions, "Exit this prompt");
                        }
                        else if (menuOption.Key == ConsoleKey.E)
                        {
                            selectedOption = "Select an option".ReadPrompt(MatchingActionOptions, "Exit this prompt");
                        }
                        else
                        {
                            break;
                        }

                        #endregion

                        #region Action Options

                        if (selectedOption.Key == ConsoleKey.V)
                        {
                            var result = await reader.GetDspVersionNumber();
                        }
                        else if (selectedOption.Key == ConsoleKey.M)
                        {
                            var result = await reader.GetRegistrationMode();
                        }
                        else if (selectedOption.Key == ConsoleKey.J)
                        {
                            var result = await reader.GetAllUsers();
                            if (result != null && result.IsSuccessful)
                            {
                                foreach (var kvp in result.Users)
                                {
                                    $"User: {kvp.Key,4}    Privilege: {kvp.Value,4}".Info();
                                }
                            }

                        }
                        else if (selectedOption.Key == ConsoleKey.U)
                        {
                            var userId = "Enter User Id".ReadNumber(1);
                            var result = await reader.GetUserProperties(userId);
                            $"User: {result.UserId}  Privilege: {result.UserPrivilege}  Eigenvalues: {(result.Eigenvalues != null ? result.Eigenvalues.Length : 0)} bytes"
                                .Info();
                        }
                        else if (selectedOption.Key == ConsoleKey.B)
                        {
                            var baseChar = 65;
                            var baudRates = Enum.GetNames(typeof(BaudRate))
                                .ToDictionary(p => (ConsoleKey)baseChar++, v => v);
                            var baudSelection =
                                $"Select new Baud Rate - Current Rate is {reader.SerialPort.BaudRate}".ReadPrompt(
                                    baudRates, "Exit this prompt");

                            if (baudRates.ContainsKey(baudSelection.Key))
                            {
                                var newBaudRate = (BaudRate)Enum.Parse(typeof(BaudRate), baudRates[baudSelection.Key]);
                                await reader.SetBaudRate(newBaudRate);
                            }
                        }
                        else if (selectedOption.Key == ConsoleKey.Y)
                        {
                            var userId = "Enter User Id".ReadNumber(1);
                            var privilege = "Enter Privilege".ReadNumber(1);

                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();

                            var result =
                                await reader.SetUserProperties(userId, privilege, eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.F3)
                        {
                            "Place your finger on the sensor to produce some eigenvalues".Info();
                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();
                            "Place your finger on the sensor once again to compare the eigenvalues".Info();
                            var result = await reader.MatchImageToEigenvalues(eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.F4)
                        {
                            var userId = "Enter User Id".ReadNumber(1);
                            "Place your finger on the sensor to produce some eigenvalues".Info();
                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();
                            "Place your finger on the sensor once again to compare the eigenvalues".Info();
                            var result = await reader.MatchUserToEigenvalues(userId, eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.F5)
                        {
                            "Place your finger on the sensor to produce some eigenvalues".Info();
                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();
                            var result = await reader.MatchEigenvaluesToUser(eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.P)
                        {
                            var userId = "Enter User Id".ReadNumber(1);
                            var result = await reader.GetUserPrivilege(userId);
                        }
                        else if (selectedOption.Key == ConsoleKey.L)
                        {
                            var result = await reader.GetMatchingLevel();
                        }
                        else if (selectedOption.Key == ConsoleKey.K)
                        {
                            var matchingLevel = "Enter Matching Level (0 to 9)".ReadNumber(5);
                            if (matchingLevel < 0) matchingLevel = 0;
                            if (matchingLevel > 9) matchingLevel = 9;

                            var result = await reader.SetMatchingLevel(matchingLevel);
                        }
                        else if (selectedOption.Key == ConsoleKey.G)
                        {
                            var timeout = "Enter CaptureTimeout (0 to 255)".ReadNumber(0);
                            if (timeout < 0) timeout = 0;
                            if (timeout > 255) timeout = 255;

                            var result = await reader.SetCaptureTimeout(timeout);
                        }
                        else if (selectedOption.Key == ConsoleKey.T)
                        {
                            var result = await reader.GetCaptureTimeout();
                        }
                        else if (selectedOption.Key == ConsoleKey.C)
                        {
                            var result = await reader.GetUserCount();
                        }
                        else if (selectedOption.Key == ConsoleKey.F6)
                        {
                            var result = await reader.AcquireImage();
                        }
                        else if (selectedOption.Key == ConsoleKey.F7)
                        {
                            var result = await reader.AcquireImageEigenvalues();
                        }
                        else if (selectedOption.Key == ConsoleKey.F1)
                        {
                            var userId = "Enter User Id".ReadNumber(1);
                            var result = await reader.MatchOneToOne(userId);
                        }
                        else if (selectedOption.Key == ConsoleKey.F2)
                        {
                            var result = await reader.MatchOneToN();
                        }
                        else if (selectedOption.Key == ConsoleKey.R)
                        {
                            var mode = "Enter Registration Mode - 1 disallows repeated fingerprints".ReadNumber(1);
                            var result = await reader.SetRegistrationMode(mode == 1);
                        }
                        else if (selectedOption.Key == ConsoleKey.A)
                        {
                            var userId = "Enter User Id".ReadNumber(1);
                            var privilege = "Enter Privilege".ReadNumber(1);

                            "Place your finger on the sensor".Info();
                            var fp1 = await reader.AddFingerprint(1, userId, privilege);
                            if (fp1 != null && fp1.IsSuccessful)
                            {
                                "Place your finger on the sensor again".Info();
                                var fp2 = await reader.AddFingerprint(2, userId, privilege);
                                if (fp2 != null && fp2.IsSuccessful)
                                {
                                    "Place your finger on the sensor once again".Info();
                                    var fp3 = await reader.AddFingerprint(3, userId, privilege);
                                    if (fp3 != null && fp3.IsSuccessful)
                                    {
                                        "User added successfully".Info();
                                    }
                                    else
                                    {
                                        "Failed on acquisition 3".Error();
                                    }
                                }
                                else
                                {
                                    "Failed on acquisition 2".Error();
                                }
                            }
                            else
                            {
                                "Failed on acquisition 1".Error();
                            }
                        }
                        else if (selectedOption.Key == ConsoleKey.W)
                        {
                            var userId = "Enter User Id".ReadNumber(1);
                            var result = await reader.DeleteUser(userId);
                        }
                        else if (selectedOption.Key == ConsoleKey.Z)
                        {
                            var result = await reader.DeleteAllUsers();
                        }
                        else if (selectedOption.Key == ConsoleKey.S)
                        {
                            var result = await reader.Sleep();
                        }

                        #endregion
                    }
                });

                try
                {
                    t.Unwrap().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    ex.Log(nameof(Program));
                }
            }

            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }
    }
}