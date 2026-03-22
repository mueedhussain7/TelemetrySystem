using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TelemetryServer;

public class TcpServer
{
    private readonly int _port;
    private readonly TelemetryRepository _repository;  // NEW
    private TcpListener? _listener;
    private int _connectedCount = 0;

    public TcpServer(int port, TelemetryRepository repository)  // NEW
    {
        _port = port;
        _repository = repository;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.WriteLine($"Server listening on port {_port}...");

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken));
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        int count = Interlocked.Increment(ref _connectedCount);
        string clientAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[+] Device connected: {clientAddress} | Total: {count}");

        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                try
                {
                    var reading = JsonSerializer.Deserialize<TelemetryReading>(message,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (reading != null)
                    {
                        // Save to database
                        await _repository.SaveReadingAsync(reading);

                        Console.WriteLine($"[SAVED] {reading.DeviceId} | " +
                                          $"{reading.Value} {reading.Unit} | " +
                                          $"{reading.Timestamp:HH:mm:ss}");
                    }
                }
                catch (JsonException)
                {
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
            int remaining = Interlocked.Decrement(ref _connectedCount);
            Console.WriteLine($"[-] Disconnected: {clientAddress} | Total: {remaining}");
            client.Dispose();
        }
    }
}