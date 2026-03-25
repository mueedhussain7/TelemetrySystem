using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Serilog;

namespace TelemetryServer;

public class TcpServer
{
    private readonly int _port;
    private readonly TelemetryRepository _repository;
    private TcpListener? _listener;
    private int _connectedCount = 0;

    public TcpServer(int port, TelemetryRepository repository)
    {
        _port = port;
        _repository = repository;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        Log.Information("Server listening on port {Port}", _port);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, not an error
            Log.Information("Server stopped.");
        }
        finally
        {
            _listener.Stop();
        }
    }
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        int count = Interlocked.Increment(ref _connectedCount);
        string clientAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

        Log.Debug("Device connected: {Address} | Total connected: {Count}",
            clientAddress, count);

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
                        await _repository.SaveReadingAsync(reading);

                        Log.Information("Saved reading | Device: {DeviceId} | " +
                                        "Value: {Value} {Unit}",
                            reading.DeviceId, reading.Value, reading.Unit);
                    }
                }
                catch (JsonException)
                {
                    Log.Warning("Could not parse message from {Address}: {Message}",
                        clientAddress, message);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Unexpected error handling client {Address}", clientAddress);
        }
        finally
        {
            int remaining = Interlocked.Decrement(ref _connectedCount);
            Log.Debug("Device disconnected: {Address} | Total connected: {Remaining}",
                clientAddress, remaining);
            client.Dispose();
        }
    }
}