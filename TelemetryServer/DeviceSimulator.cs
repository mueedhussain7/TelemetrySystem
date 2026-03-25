using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TelemetryServer;

public class DeviceSimulator
{
    public string DeviceId { get; }
    public string SensorType { get; }
    private readonly string _host;
    private readonly int _port;

    private static readonly string[] SensorTypes = 
        { "temperature", "humidity", "pressure", "vibration" };

    public DeviceSimulator(int deviceNumber, string host, int port)
    {
        DeviceId = $"Device-{deviceNumber:D2}";   // e.g. "Device-07"
        SensorType = SensorTypes[deviceNumber % SensorTypes.Length];
        _host = host;
        _port = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Keep trying to connect if the server isn't ready yet
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_host, _port, cancellationToken);

                Console.WriteLine($"[SIM] {DeviceId} connected ({SensorType} sensor)");

                await SendReadingsAsync(client.GetStream(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break; // clean shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SIM] {DeviceId} error: {ex.Message} — retrying in 3s");
                await Task.Delay(3000, cancellationToken);
            }
        }

        Console.WriteLine($"[SIM] {DeviceId} stopped.");
    }

    private async Task SendReadingsAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var reading = new TelemetryReading
            {
                DeviceId  = DeviceId,
                Timestamp = DateTime.UtcNow,
                Value     = GenerateValue(),
                Unit      = GetUnit()
            };

            string json = JsonSerializer.Serialize(reading);
            byte[] data = Encoding.UTF8.GetBytes(json + "\n");

            await stream.WriteAsync(data, cancellationToken);

            // Send one reading per second
            await Task.Delay(1000, cancellationToken);
        }
    }

    // Generate a realistic random value for this sensor type
    private double GenerateValue() => SensorType switch
    {
        "temperature" => Math.Round(20 + Random.Shared.NextDouble() * 15, 2),  // 20–35 °C
        "humidity"    => Math.Round(40 + Random.Shared.NextDouble() * 40, 2),  // 40–80 %
        "pressure"    => Math.Round(1000 + Random.Shared.NextDouble() * 50, 2),// 1000–1050 hPa
        "vibration"   => Math.Round(Random.Shared.NextDouble() * 5, 3),         // 0–5 mm/s
        _             => Math.Round(Random.Shared.NextDouble() * 100, 2)
    };

    private string GetUnit() => SensorType switch
    {
        "temperature" => "°C",
        "humidity"    => "%",
        "pressure"    => "hPa",
        "vibration"   => "mm/s",
        _             => "units"
    };
}