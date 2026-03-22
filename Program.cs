using TelemetryServer;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
};

// Connection string — tells Npgsql where to find PostgreSQL
const string connectionString =
    "Host=localhost; Database=telemetry_db; Username=mueedhussain; Password=";

var repository = new TelemetryRepository(connectionString);
var server     = new TcpServer(port: 9000, repository);
var serverTask = server.StartAsync(cts.Token);

await Task.Delay(500);

Console.WriteLine("[SIM] Starting 30 simulated devices...");
var simulators = Enumerable.Range(1, 30)
    .Select(i => new DeviceSimulator(i, "127.0.0.1", 9000))
    .ToList();

var simulatorTasks = simulators
    .Select(sim => sim.StartAsync(cts.Token))
    .ToList();

await Task.WhenAll([serverTask, ..simulatorTasks]);

