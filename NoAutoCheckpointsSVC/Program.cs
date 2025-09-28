using NoAutoCheckpointsSVC;
using System.Diagnostics;
using System.Reflection;

var builder = Host.CreateDefaultBuilder(args);

#if DEBUG
builder.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<Worker>();
    services.AddSingleton<nacSVC>();
});
#else
bool installService = args.Contains("--installService", StringComparer.OrdinalIgnoreCase);
bool uninstallService = args.Contains("--uninstallService", StringComparer.OrdinalIgnoreCase);
bool startService = args.Contains("--startService", StringComparer.OrdinalIgnoreCase);

var quotedPath = $"\"{Assembly.GetExecutingAssembly().Location.Replace("NoAutoCheckpointsSVC.dll", "NoAutoCheckpointsSVC.exe")}\"";

if (installService && uninstallService)
{
    Console.Error.WriteLine("Error: Only one of --installService or --uninstallService can be used.");
    return;
}

if (installService)
{
    Console.WriteLine("Installing service...");
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "sc.exe",
        Arguments = $@"create NoAutoCheckpointsSVC binPath= {quotedPath} start= auto",
        Verb = "runas",
        UseShellExecute = true
    });
    process?.WaitForExit();
    Console.WriteLine("Service installed");
    if (startService)
    {
        Console.WriteLine("Starting service...");
        process = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $@"start NoAutoCheckpointsSVC",
            Verb = "runas",
            UseShellExecute = true
        });
        process?.WaitForExit();
        Console.WriteLine("Service started");
    }
    return;
}

if (uninstallService)
{
    Console.WriteLine("Stopping service...");
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "sc.exe",
        Arguments = @"stop NoAutoCheckpointsSVC",
        Verb = "runas",
        UseShellExecute = true
    });
    process?.WaitForExit();
    Console.WriteLine("Uninstall service...");
    process = Process.Start(new ProcessStartInfo
    {
        FileName = "sc.exe",
        Arguments = @"delete NoAutoCheckpointsSVC",
        Verb = "runas",
        UseShellExecute = true
    });
    process?.WaitForExit();
    Console.WriteLine("Service uninstalled");
    return;
}

builder.UseWindowsService(options =>
{
    options.ServiceName = "NoAutoCheckpointsSVC";
})
.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<Worker>();
    services.AddSingleton<NacSVC>();
});
#endif

var host = builder.Build();
host.Run();
