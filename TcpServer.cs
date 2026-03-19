using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TelemetryServer;

public class TcpServer
{
    private readonly int _port;
    private TcpListener? _listener;

    // Tracks how many devices are currently connected
    private int _connectedCount = 0;

    public TcpServer(int port)
    {
        _port = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        Console.WriteLine($"Server listening on port {_port}...");

        // Loop forever — keep accepting new devices
        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait for next device to connect
            TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);

            // Give each device its OWN task — like assigning a doctor to each patient
            // The _ means "fire and forget" — don't wait for it, just let it run
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken));
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        // Count this new connection
        int count = Interlocked.Increment(ref _connectedCount);
        string clientAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

        Console.WriteLine($"[+] Device connected: {clientAddress} | Total connected: {count}");

        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0) break; // device disconnected

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                // Try to parse it as JSON, otherwise just print it raw
                try
                {
                    var reading = JsonSerializer.Deserialize<TelemetryReading>(message);
                    Console.WriteLine($"[DATA] Device: {reading?.DeviceId} | " +
                                      $"Value: {reading?.Value} {reading?.Unit} | " +
                                      $"Time: {reading?.Timestamp:HH:mm:ss}");
                }
                catch
                {
                    // Not JSON yet — just print the raw message
                    Console.WriteLine($"[RAW] {clientAddress}: {message}");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"[ERROR] {clientAddress}: {ex.Message}");
        }
        finally
        {
            // Always clean up, even if something crashes
            int remaining = Interlocked.Decrement(ref _connectedCount);
            Console.WriteLine($"[-] Device disconnected: {clientAddress} | Total connected: {remaining}");
            client.Dispose();
        }
    }
}