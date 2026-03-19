using TelemetryServer;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
};

// Start the server in the background
var server = new TcpServer(port: 9000);
var serverTask = server.StartAsync(cts.Token);

// Give the server half a second to start before devices connect
await Task.Delay(500);

// Spin up 30 simulated devices — each runs independently
Console.WriteLine("[SIM] Starting 30 simulated devices...");

var simulators = Enumerable.Range(1, 30)
    .Select(i => new DeviceSimulator(i, "127.0.0.1", 9000))
    .ToList();

var simulatorTasks = simulators
    .Select(sim => sim.StartAsync(cts.Token))
    .ToList();

// Wait for everything to finish (Ctrl+C triggers the shutdown)
await Task.WhenAll([serverTask, ..simulatorTasks]);