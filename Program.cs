using TelemetryServer;

// lets Ctrl+C shut the server down cleanly
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
};

var server = new TcpServer(port: 9000);
await server.StartAsync(cts.Token);