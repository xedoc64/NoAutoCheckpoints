using Microsoft.Extensions.Configuration;
using Serilog;
using System.Diagnostics.Eventing.Reader;


class Program
{
    private static EventLogWatcher? watcher = null;
    private static string eventLogPath = "Microsoft-Windows-Hyper-V-VMMS-Admin";
    private static string eventLogQuery = "*[System[(EventID=13002 or EventID=18304)]]";
    private static naclib.VM? vm = null;


    static void Main(string[] args)
    {
        SetupStaticLogger();

        Log.Information("NoAutoCheckpointsCLI started");

        naclib.PermissionCheck permissionCheck = new naclib.PermissionCheck();

        if (permissionCheck.permission == naclib.PermissionCheck.PermissionType.PermissionFailed)
        {
            Log.Error("User is not in Group \"Hyper-V Administrators\" or application is not running elevated");
            Environment.Exit(2);
        }

        if (subscribeToEventLog(eventLogPath, eventLogQuery))
        {
            Log.Information("Event log subscribed. Press a key to end the program");
            Task.Run(() => Console.ReadKey()).Wait();
            DisableWatcher(watcher);
            Environment.Exit(0);
        }
        else
        {
            Log.Error("Could not subscribe to event log. Program will close");
            Environment.Exit(1);
        }
    }

    private static void SetupStaticLogger()
    {
        bool isPortable = File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.dat"));

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        if (isPortable)
        {
            var portableLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "portable-log-.txt");
            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration) // Load base config
            .WriteTo.File(
                path: portableLogPath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true
            )
            .CreateLogger();
        }
        else
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }        
    }

    private static bool subscribeToEventLog(string logPath, string logQuery)
    {
        try
        {
            EventLogQuery eventLogQuery = new EventLogQuery(logPath, PathType.LogName, logQuery);

            watcher = new EventLogWatcher(eventLogQuery);

            // Make the watcher listen to the EventRecordWritten
            // events.  When this event happens, the callback method
            // (EventLogEventRead) is called.
            watcher.EventRecordWritten +=
                new EventHandler<EventRecordWrittenEventArgs>(
                    EventLogEventRead);

            // Activate the subscription
            watcher.Enabled = true;
            return true;
        }
        catch (EventLogReadingException e)
        {
            Log.Fatal("Error on subscribe to the event log: {0}", e.Message);

            if (watcher != null)
            {
                // Stop listening to events
                watcher.Enabled = false;
                watcher.Dispose();
            }
            return false;
        }
    }

    private static void EventLogEventRead(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventRecord != null)
        {
            // disable the watcher to prevent that the watcher is fired twice
            DisableWatcher(watcher);
            string[] xPathRefs = new string[1];
            xPathRefs[0] = "Event/UserData/VmlEventLog/VmId";

            IEnumerable<String> xPathEnum = xPathRefs;

            EventLogPropertySelector logPropertyContext = new EventLogPropertySelector(xPathEnum);

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

    private static void DisableWatcher(EventLogWatcher? watcher)
    {
        if (watcher != null)
        {
            Log.Debug("watcher disabled");
            watcher.Enabled = false;
            watcher.Dispose();
        }
    }

    private static void EnableWatcher(EventLogWatcher? watcher)
    {
        if (watcher != null)
        {
            Log.Debug("watcher enabled");
            watcher.Enabled = true;
            watcher.Dispose();
        }
    }
}