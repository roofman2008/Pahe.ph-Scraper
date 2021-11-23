using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using PaheScrapper.Helpers;
using PaheScrapper.Properties;

namespace PaheScrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleHelper.LogInfo("Pahe Scraper - Alpha 2.3");

            if (args.Length == 3 && args[0] == "-d")
            {
                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string encodedPath = args[1];
                string decodedPath = args[2];

                string encodedText = File.OpenText(Path.Combine(folderPath, encodedPath)).ReadToEnd();
                string decodedText = StringCompressor.DecompressString(encodedText);

                using (var file = File.CreateText(Path.Combine(folderPath, decodedPath)))
                {
                    file.WriteAsync(decodedText).Wait();
                    file.Close();
                }

                return;
            }

            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_MAXIMIZE, MF_BYCOMMAND);
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_SIZE, MF_BYCOMMAND);

            handler = ConsoleEventCallback;
            SetConsoleCtrlHandler(handler, true);
          
            ScrapperWeb.ReleaseGarbageScrape();

            /*Fix SSL/TLS Problem*/
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (args.Length == 0)
                FullScrape();
            else
            {
                FullScrape(args[0]);
            }
        }

        #region Handle Release Console
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                ConsoleHelper.LogCommandHandled("Console window closing, Releasing Chrome....");
                ScrapperWeb.ReleaseGarbageScrape();
            }
            return false;
        }
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        #endregion

        #region Prevent Closing Console
        private const int MF_BYCOMMAND = 0x00000000;
        private const int SC_CLOSE = 0xF060;           //close button's code in Windows API
        private const int SC_MINIMIZE = 0xF020;        //for minimize button on forms
        private const int SC_MAXIMIZE = 0xF030;        //for maximize button on forms
        private const int MF_ENABLED = 0x00000000;     //enabled button status
        private const int MF_GRAYED = 0x1;             //disabled button status (enabled = false)
        private const int MF_DISABLED = 0x00000002;    //disabled button status
        private const int SC_SIZE = 0xF000;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr HWNDValue, bool isRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern int EnableMenuItem(IntPtr tMenu, int targetItem, int targetStatus);
        #endregion

        static void FullScrape(string command = null)
        {
            bool isLoop = false;
            int retry = 0;
            ScrapperManager _manager = null;
            string _folderPath = AppDomain.CurrentDomain.BaseDirectory;
            var failsafeDir = Directory.CreateDirectory(Path.Combine(_folderPath, "Failsafe"));
            StreamReader f =null;
            recommand:
            if (command == null)
                command = ConsoleHelper.LogInput("Enter Command [Continue, Resync, New, Loop, State]: ")?.ToLower()
                    .TrimStart().TrimEnd();
            re_execute:
            try
            {
                if (command == "continue")
                {
                    f = File.OpenText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename));
                    _manager = ScrapperManager.Deserialize(StringCompressor.DecompressString(f.ReadToEnd()));
                    f.Close();
                }
                else if (command == "resync")
                {
                    f = File.OpenText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename));
                    _manager = ScrapperManager.Deserialize(StringCompressor.DecompressString(f.ReadToEnd()));
                    _manager.ResetState();
                    f.Close();
                }
                else if (command == "loop")
                {
                    f = File.OpenText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename));
                    _manager = ScrapperManager.Deserialize(StringCompressor.DecompressString(f.ReadToEnd()));
                    f.Close();
                    isLoop = true;
                }
                else if (command == "new")
                {
                    _manager = new ScrapperManager();
                }
                else if (command == "state")
                {
                    f = File.OpenText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename));
                    _manager = ScrapperManager.Deserialize(StringCompressor.DecompressString(f.ReadToEnd()));
                    f.Close();
                    ConsoleHelper.LogCommandHandled($"Current State = {_manager.State}");
                    _manager = null;
                    command = null;
                    goto recommand;
                }
                else if (command == null)
                {
                    Environment.Exit(0);
                }
                else
                {
                    command = null;
                    goto recommand;
                }

            }
            catch (Exception ex)
            {
                ConsoleHelper.LogError(ex.Message);

                if (ex.Source == "Newtonsoft.Json" ||
                    ex.Message == "Object reference not set to an instance of an object.")
                {
                    f.Close();
                    var lastFailFile = failsafeDir.GetFiles("*", SearchOption.AllDirectories).OrderByDescending(l => l.CreationTime).FirstOrDefault();

                    if (lastFailFile != null)
                    {
                        lastFailFile.CopyTo(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename), true);
                        ConsoleHelper.LogBranch("Restore Last Failsafe State");
                        goto re_execute;
                    }
                }
             
                Console.ReadLine();
                return;
            }

            int backupCounter = 0;
          
            do
            {
                try
                {
                    _manager.Scrape((state) =>
                        {
                            ConsoleHelper.LogCritical("Don't Kill The Process or Manager State File Will Corrupt While Saving !!!");

                            EnableMenuItem(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_DISABLED);

                            ConsoleHelper.LogStorage("Normal Save Scrapper State");

                            using (var file = File.CreateText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename)))
                            {
                                file.WriteAsync(StringCompressor.CompressString(_manager.Serialize())).Wait();
                                file.Close();
                            }

                            int backupTriggerCount = Configuration.Default.FailsafeStateThershold *
                                                     (state == ScrapperState.Sora
                                                         ? Configuration.Default.WebDriveSaveStateThershold
                                                         : Configuration.Default.HTMLSaveStateThershold);

                            if (backupCounter < backupTriggerCount)
                            {
                                backupCounter++;
                            }
                            else
                            {
                                ConsoleHelper.LogBranch("Failsafe Scrapper State");

                                var fileName = DateTime.Now.ToFileTime() + "_" +
                                               Configuration.Default.ManagerStateFilename;
                                backupCounter = 0;
                                var nowFailsafeDir =
                                    Directory.CreateDirectory(Path.Combine(failsafeDir.FullName,
                                        DateTime.Now.ToShortDateString().Replace("/", "-")));
                                File.Copy(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename),
                                    Path.Combine(nowFailsafeDir.FullName, fileName));

                                ConsoleHelper.LogInfo(
                                    $"Failsafe File: {Path.Combine(nowFailsafeDir.FullName, fileName)}");
                            }

                            EnableMenuItem(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_ENABLED);

                            ConsoleHelper.LogInfo("You Can Kill The Process Now.");
                        },
                        (state) =>
                        {
                            ConsoleHelper.LogCritical("Don't Kill The Process or Manager State File Will Corrupt While Saving !!!");

                            EnableMenuItem(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_DISABLED);

                            ConsoleHelper.LogStorage("Emergency Save Scraper State");

                            using (var file =
                                File.CreateText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename)))
                            {
                                file.WriteAsync(StringCompressor.CompressString(_manager.Serialize())).Wait();
                                file.Close();
                            }

                            ConsoleHelper.LogCritical($"Re-Run the Scarper for [{retry}] time");

                            EnableMenuItem(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_ENABLED);
                            ConsoleHelper.LogInfo("You Can Kill The Process Now.");
                        });
                }
                catch (Exception ex)
                {
                    ConsoleHelper.LogError(ex.Message);
                    Console.ReadLine();
                    return;
                }

                ConsoleHelper.LogCritical("Don't Kill The Process or Manager & Output Files Will Corrupt While Saving !!!");
                EnableMenuItem(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_DISABLED);

                ConsoleHelper.LogStorage("Final Save Scraper State");
                using (var file = File.CreateText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename)))
                {
                    file.WriteAsync(StringCompressor.CompressString(_manager.Serialize())).Wait();
                    file.Close();
                }

                /*Output File*/
                ConsoleHelper.LogStorage("Save Scrape Output File");
                using (var file = File.CreateText(Path.Combine(_folderPath, Configuration.Default.OutputScrapeFilename)))
                {
                    file.WriteAsync(_manager.Context.Serialize()).Wait();
                    file.Close();
                }

                EnableMenuItem(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_ENABLED);
                ConsoleHelper.LogInfo("You Can Kill The Process Now.");

                ScrapperWeb.ReleaseActiveScrape();

                ConsoleHelper.LogCommandHandled("Completed ..............");

                if (isLoop)
                {
                    _manager.ResetState();
                    var loopDir = Directory.CreateDirectory(Path.Combine(_folderPath, "Loop"));
                    var nowLoopDir =
                        Directory.CreateDirectory(Path.Combine(loopDir.FullName, DateTime.Now.ToShortDateString().Replace("/", "-")));
                    File.Copy(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename),
                              Path.Combine(nowLoopDir.FullName, Configuration.Default.ManagerStateFilename));
                    ConsoleHelper.LogInfo("Backup The Results To: " + nowLoopDir.FullName);
                    ConsoleHelper.LogBranch("Re-Loop The Process");
                }

            } while (isLoop);

            Console.ReadLine();
        }
    }
}
