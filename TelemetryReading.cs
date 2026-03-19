namespace TelemetryServer;

public class TelemetryReading
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}