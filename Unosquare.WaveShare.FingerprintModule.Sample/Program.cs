namespace Unosquare.WaveShare.FingerprintModule.Sample
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class Program
    {
        #region Action Options

        static private readonly Dictionary<ConsoleKey, string> ActionOptions = new Dictionary<ConsoleKey, string>
        {
            // Module COntrol Items
            { ConsoleKey.V, "MODULE   - Get DSP Version" },
            { ConsoleKey.M, "MODULE   - Get Fingerprint Registration Mode" },
            { ConsoleKey.R, "MODULE   - Set Fingerprint Registration Mode" },
            { ConsoleKey.T, "MODULE   - Get Capture Timeout" },
            { ConsoleKey.G, "MODULE   - Set Capture Timeout" },
            { ConsoleKey.L, "MODULE   - Get Fingerprint Matching Level" },
            { ConsoleKey.K, "MODULE   - Set Fingerprint Matching Level" },
            { ConsoleKey.S, "MODULE   - Sleep (Warning: Module requires reset)" },

            // User Control Items
            { ConsoleKey.C, "USERS    - Get User Count" },
            { ConsoleKey.J, "USERS    - List All Users and Privileges" },
            { ConsoleKey.A, "USERS    - Register a New User with a Fingerprint" },
            { ConsoleKey.P, "USERS    - Get a User's Privilege" },
            { ConsoleKey.U, "USERS    - Get a User's Privilege and Eigenvalues" },
            { ConsoleKey.Y, "USERS    - Create a new User providing an Id, Privilege and Eigenvalues" },
            { ConsoleKey.W, "USERS    - Delete a User" },
            { ConsoleKey.Z, "USERS    - Delete all Users (Warning: This deletes the entire database)" },

            // Matching Items
            { ConsoleKey.F1, "MATCHING - Test if a User Id Matches an Acquired Image (1:1)" },
            { ConsoleKey.F2, "MATCHING - Get a User Id given an Acquired Image (1:N)" },
            { ConsoleKey.F3, "MATCHING - Test if an Acquired Image matches the provided Eigenvalues (1:1)" },
            { ConsoleKey.F4, "MATCHING - Test if a given User ,atches the supplied Eigenvalues (1:1)" },
            { ConsoleKey.F5, "MATCHING - Get a User Id given an array with Eigenvalues (1:N)" },
            { ConsoleKey.F6, "MATCHING - Acquire Image" },
            { ConsoleKey.F7, "MATCHING - Acquire Image Eigenvalues" },
        };

        #endregion

        static void Main(string[] args)
        {
            using (var reader = new FingerprintReader())
            {
                reader.Open("COM3");
                var t = Task.Factory.StartNew(async () =>
                {
                    var changeBaudResult = await reader.SetBaudRate(BaudRate.Baud115200);

                    while (true)
                    {
                        var selectedOption = Log.ReadPrompt("Select an option", ActionOptions, "Exit this program");

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
                                    Log.Info($"User: {kvp.Key,4}    Privilege: {kvp.Value,4}");
                                }
                            }

                        }
                        else if (selectedOption.Key == ConsoleKey.U)
                        {
                            var userId = Log.ReadNumber("Enter User Id", 1);
                            var result = await reader.GetUserProperties(userId);
                            Log.Info($"User: {result.UserId}  Privilege: {result.UserPrivilege}  Eigenvalues: {(result.Eigenvalues != null ? result.Eigenvalues.Length : 0)} bytes");
                        }
                        else if (selectedOption.Key == ConsoleKey.Y)
                        {
                            var userId = Log.ReadNumber("Enter User Id", 1);
                            var privilege = Log.ReadNumber("Enter Privilege", 1);

                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();

                            var result = await reader.SetUserProperties(userId, privilege, eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.F3)
                        {
                            Log.Info("Place your finger on the sensor to produce some eigenvalues");
                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();
                            Log.Info("Place your finger on the sensor once again to compare the eigenvalues");
                            var result = await reader.MatchImageToEigenvalues(eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.F4)
                        {
                            var userId = Log.ReadNumber("Enter User Id", 1);
                            Log.Info("Place your finger on the sensor to produce some eigenvalues");
                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();
                            Log.Info("Place your finger on the sensor once again to compare the eigenvalues");
                            var result = await reader.MatchUserToEigenvalues(userId, eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.F5)
                        {
                            Log.Info("Place your finger on the sensor to produce some eigenvalues");
                            var eigenvaluesResult = await reader.AcquireImageEigenvalues();
                            var result = await reader.MatchEigenvaluesToUser(eigenvaluesResult.Eigenvalues);
                        }
                        else if (selectedOption.Key == ConsoleKey.P)
                        {
                            var userId = Log.ReadNumber("Enter User Id", 1);
                            var result = await reader.GetUserPrivilege(userId);
                        }
                        else if (selectedOption.Key == ConsoleKey.L)
                        {
                            var result = await reader.GetMatchingLevel();
                        }
                        else if (selectedOption.Key == ConsoleKey.K)
                        {
                            var matchingLevel = Log.ReadNumber("Enter Matching Level (0 to 9)", 5);
                            if (matchingLevel < 0) matchingLevel = 0;
                            if (matchingLevel > 9) matchingLevel = 9;

                            var result = await reader.SetMatchingLevel(matchingLevel);
                        }
                        else if (selectedOption.Key == ConsoleKey.G)
                        {
                            var timeout = Log.ReadNumber("Enter CaptureTimeout (0 to 255)", 0);
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
                            var userId = Log.ReadNumber("Enter User Id", 1);
                            var result = await reader.MatchOneToOne(userId);
                        }
                        else if (selectedOption.Key == ConsoleKey.F2)
                        {
                            var result = await reader.MatchOneToN();
                        }
                        else if (selectedOption.Key == ConsoleKey.R)
                        {
                            var mode = Log.ReadNumber("Enter Registration Mode - 1 disallows repeated fingerprints", 1);
                            var result = await reader.SetRegistrationMode(mode == 1);
                        }
                        else if (selectedOption.Key == ConsoleKey.A)
                        {
                            var userId = Log.ReadNumber("Enter User Id", 1);
                            var privilege = Log.ReadNumber("Enter Privilege", 1);

                            Log.Info("Place your finger on the sensor");
                            var fp1 = await reader.AddFingerprint(1, userId, privilege);
                            if (fp1 != null && fp1.IsSuccessful)
                            {
                                Log.Info("Place your finger on the sensor again");
                                var fp2 = await reader.AddFingerprint(2, userId, privilege);
                                if (fp2 != null && fp2.IsSuccessful)
                                {
                                    Log.Info("Place your finger on the sensor once again");
                                    var fp3 = await reader.AddFingerprint(3, userId, privilege);
                                    if (fp3 != null && fp3.IsSuccessful)
                                    {
                                        Log.Info("User added successfully");
                                    }
                                    else
                                    {
                                        Log.Error("Failed on acquisition 3");
                                    }
                                }
                                else
                                {
                                    Log.Error("Failed on acquisition 2");
                                }
                            }
                            else
                            {
                                Log.Error("Failed on acquisition 1");
                            }
                        }
                        else if (selectedOption.Key == ConsoleKey.W)
                        {
                            var userId = Log.ReadNumber("Enter User Id", 1);
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
                        else
                        {
                            break;
                        }
                    }


                });

                t.Unwrap().GetAwaiter().GetResult();
            }
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);

        }
    }
}
