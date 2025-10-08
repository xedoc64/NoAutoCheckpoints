using Serilog;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using System.Diagnostics;

namespace NoAutoCheckpointsSVC
{
    public class NacSVC
    {
        private static EventLogWatcher? watcher = null;
        private static readonly string eventLogPath = "Microsoft-Windows-Hyper-V-VMMS-Admin";
        private static readonly string eventLogQuery = "*[System[(EventID=13002 or EventID=18304)]]";
        private static naclib.VM? vm = null;

        public NacSVC()
        {
            SetupStaticLogger();
            Log.Debug("Service started");
            Log.Debug("OS: {0}", Environment.OSVersion);
            string appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(appVersion))
            {
                Log.Debug("App version: {0}", appVersion);
            }

            // check required permissions
            naclib.PermissionCheck permissionCheck = new();

            if (permissionCheck.permission == naclib.PermissionCheck.PermissionType.PermissionFailed)
            {
                Log.Fatal("User is not in Group \"Hyper-V Administrators\" or application is not running elevated");
                throw new InvalidOperationException("Permission check failed.");
            }
        }

        public void Run()
        {
            // subscribe to the event log
            if (SubscribeToEventLog(eventLogPath, eventLogQuery))
            {
                Log.Debug("Event log subscribed.");
            }
            else
            {
                Log.Fatal("Could not subscribe to event log.");
                throw new InvalidOperationException("Event log subscription failed.");
            }
        }

        /// <summary>
        /// Called when service receive a stop request
        /// </summary>
        public void Stop()
        {
            Log.Debug("Service stop requested");
            // Dispose the watchcer
            DisposeWatcher(watcher);
            Log.Information("Service stopped");
        }

        /// <summary>
        /// Setup the SeriLog logger
        /// </summary>
        private static void SetupStaticLogger()
        {
            string SettingsFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\appsettings.json";
            if (!File.Exists(SettingsFile))
            {
                string source = "NoAutoCheckpointsSVC";
                string logName = "Application";

                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, logName);
                }

                EventLog.WriteEntry(source, $"{SettingsFile} not found. Logging will not work.", EventLogEntryType.Warning);
            }
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            Log.Debug("Logger created");
        }

        /// <summary>
        /// Subscribes to the Windows event log
        /// </summary>
        /// <param name="logPath"></param>
        /// <param name="logQuery"></param>
        /// <returns>true on success</returns>
        private static bool SubscribeToEventLog(string logPath, string logQuery)
        {
            try
            {
                EventLogQuery eventLogQuery = new(logPath, PathType.LogName, logQuery);

                watcher = new EventLogWatcher(eventLogQuery);

                // Make the watcher listen to the EventRecordWritten
                // events.  When this event happens, the callback method
                // (EventLogEventRead) is called.
                watcher.EventRecordWritten +=
                    new EventHandler<EventRecordWrittenEventArgs>(
                        EventLogEventRead);
                Log.Debug("Event watcher registered");

                // Activate the subscription
                EnableWatcher(watcher);
                return true;
            }
            catch (EventLogReadingException e)
            {
                // Stop listening to events
                DisableWatcher(watcher);

                watcher?.Dispose();

                Log.Fatal("Event watcher could not be created. Error message: {0}", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Will be called, if logQuery triggered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">event log entry</param>
        private static void EventLogEventRead(object? sender, EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord != null)
            {
                // disable the watcher to prevent that the watcher is fired twice
                DisableWatcher(watcher);
                string[] xPathRefs = ["Event/UserData/VmlEventLog/VmId"];
                IEnumerable<String> xPathEnum = xPathRefs;

                EventLogPropertySelector logPropertyContext = new(xPathEnum);

                IList<object> logEventProps = ((EventLogRecord)e.EventRecord).GetPropertyValues(logPropertyContext);

                vm = new naclib.VM((string)logEventProps[0]);
                Log.Information("New VM detected. ID: {0}", logEventProps[0]);
                Log.Information("AutoSnaphot: {0}", vm.AutoCheckpointsEnabled);
                string setVM = vm.SetAutoCheckpoints();
                if (string.IsNullOrEmpty(setVM))
                {
                    Log.Information("Disable automatic checkpoints successfull");
                }
                else
                {
                    Log.Error("Disable automatic checkpoints failed. Error: {0}", setVM);
                }
                EnableWatcher(watcher);
            }
        }

        /// <summary>
        /// Disable and dispose the watcher
        /// </summary>
        /// <param name="watcher">EventLogWatcher object</param>
        private static void DisposeWatcher(EventLogWatcher? watcher)
        {
            if (watcher != null)
            {
                Log.Debug("Event watcher disabled");
                watcher.Enabled = false;
                watcher.Dispose();
            }
        }

        /// <summary>
        /// Disable the watcher
        /// </summary>
        /// <param name="watcher">EventLogWatcher object</param>
        private static void DisableWatcher(EventLogWatcher? watcher)
        {
            if (watcher != null)
            {
                Log.Debug("Event watcher disabled");
                watcher.Enabled = false;
            }
        }

        /// <summary>
        /// Enable the watcher
        /// </summary>
        /// <param name="watcher">EventLogWatcher object</param>
        private static void EnableWatcher(EventLogWatcher? watcher)
        {
            if (watcher != null)
            {
                Log.Debug("Event watcher enabled");
                watcher.Enabled = true;
            }
        }
    }
}
