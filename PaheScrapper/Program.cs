using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PaheScrapper.Helpers;
using PaheScrapper.Properties;

namespace PaheScrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            handler = ConsoleEventCallback;
            SetConsoleCtrlHandler(handler, true);

            ScrapperWeb.ReleaseGarbageScrape();

            if (args.Length == 0)
                FullScrape();
            else
            {
                FullScrape(args[0]);
            }
        }

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
                    _manager = ScrapperManager.Deserialize(f.ReadToEnd());
                    f.Close();
                }
                else if (command == "resync")
                {
                    f = File.OpenText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename));
                    _manager = ScrapperManager.Deserialize(f.ReadToEnd());
                    _manager.ResetState();
                    f.Close();
                }
                else if (command == "loop")
                {
                    f = File.OpenText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename));
                    _manager = ScrapperManager.Deserialize(f.ReadToEnd());
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
                    _manager = ScrapperManager.Deserialize(f.ReadToEnd());
                    f.Close();
                    ConsoleHelper.LogCommandHandled($"Current State = {_manager.State.ToString()}");
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
                            ConsoleHelper.LogStorage("Normal Save Scrapper State");

                            using (var file =
                                File.CreateText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename)))
                            {
                                file.Write(_manager.Serialize());
                                file.Close();
                            }

                            int backupTriggerCount = Configuration.Default.FailsafeThershold *
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
                        },
                        (state) =>
                        {
                            ConsoleHelper.LogStorage("Emergency Save Scrapper State");

                            using (var file =
                                File.CreateText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename)))
                            {
                                file.Write(_manager.Serialize());
                                file.Close();
                            }

                            if (state == ScrapperState.Sora)
                            {
                                ScrapperWeb.ReleaseActiveScrape();
                                int scrapperInstance = Configuration.Default.WebDriveInstances;
                                ScrapperWeb.InitializeActiveScrape(scrapperInstance);
                            }

                            ConsoleHelper.LogCritical($"Re-Run the Scarpper for [{retry}] time");
                        });
                }
                catch (Exception ex)
                {
                    ConsoleHelper.LogError(ex.Message);
                    Console.ReadLine();
                    return;
                }

                using (var file = File.CreateText(Path.Combine(_folderPath, Configuration.Default.ManagerStateFilename)))
                {
                    file.Write(_manager.Serialize());
                    file.Close();
                }

                /*Output File*/
                using (var file = File.CreateText(Path.Combine(_folderPath, Configuration.Default.OutputScrapeFilename)))
                {
                    file.Write(_manager.Context.Serialize());
                    file.Close();
                }

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
