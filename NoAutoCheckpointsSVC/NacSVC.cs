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

            naclib.PermissionCheck permissionCheck = new();

            if (permissionCheck.permission == naclib.PermissionCheck.PermissionType.PermissionFailed)
            {
                Log.Fatal("User is not in Group \"Hyper-V Administrators\" or application is not running elevated");
                throw new InvalidOperationException("Permission check failed.");
            }
        }

        public void Run()
        {
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

        public void Stop()
        {
            Log.Debug("Service stop requested");
            DisposeWatcher(watcher);
            Log.Information("Service stopped");
        }

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
                Log.Information("AutoSnaphot: {0}", vm.AutoSnapshotEnabled);
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

        private static void DisposeWatcher(EventLogWatcher? watcher)
        {
            if (watcher != null)
            {
                Log.Debug("Event watcher disabled");
                watcher.Enabled = false;
                watcher.Dispose();
            }
        }

        private static void DisableWatcher(EventLogWatcher? watcher)
        {
            if (watcher != null)
            {
                Log.Debug("Event watcher disabled");
                watcher.Enabled = false;
            }
        }

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
