using Serilog;
using TelemetryServer;

// ── Configure Serilog ──────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/telemetry-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// ── Graceful shutdown ──────────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log.Information("Shutdown requested — stopping server...");
    cts.Cancel();
};

// ── Run everything ─────────────────────────────────────────────
try
{
    const string connectionString =
        "Host=localhost; Database=telemetry_db; Username=mueedhussain; Password=";

    var repository = new TelemetryRepository(connectionString);
    var server     = new TcpServer(port: 9000, repository);
    var serverTask = server.StartAsync(cts.Token);

    await Task.Delay(500);

    Log.Information("Starting {DeviceCount} simulated devices...", 30);

    var simulators = Enumerable.Range(1, 30)
        .Select(i => new DeviceSimulator(i, "127.0.0.1", 9000))
        .ToList();

    var simulatorTasks = simulators
        .Select(sim => sim.StartAsync(cts.Token))
        .ToList();

    await Task.WhenAll([serverTask, ..simulatorTasks]);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    // Always flush logs before the app exits — nothing gets lost
    Log.CloseAndFlush();
}