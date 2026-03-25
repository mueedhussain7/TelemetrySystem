using NUnit.Framework;
using System.Text.Json;
using TelemetryServer;

namespace TelemetryServer.Tests;

[TestFixture]
public class JsonParsingTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public void ParseJson_ValidPayload_ReturnsCorrectReading()
    {
        string json = """
            {
                "deviceId": "Device-01",
                "timestamp": "2026-03-22T21:05:22Z",
                "value": 24.57,
                "unit": "°C"
            }
            """;

        var reading = JsonSerializer.Deserialize<TelemetryReading>(json, Options);

        Assert.That(reading,           Is.Not.Null);
        Assert.That(reading!.DeviceId, Is.EqualTo("Device-01"));
        Assert.That(reading.Value,     Is.EqualTo(24.57));
        Assert.That(reading.Unit,      Is.EqualTo("°C"));
    }

    [Test]
    public void ParseJson_ValidPayload_ParsesTimestampCorrectly()
    {
        string json = """
            {
                "deviceId": "Device-02",
                "timestamp": "2026-03-22T21:05:22Z",
                "value": 63.12,
                "unit": "%"
            }
            """;

        var reading = JsonSerializer.Deserialize<TelemetryReading>(json, Options);

        Assert.That(reading!.Timestamp.Year,  Is.EqualTo(2026));
        Assert.That(reading.Timestamp.Month,  Is.EqualTo(3));
        Assert.That(reading.Timestamp.Day,    Is.EqualTo(22));
    }

    [Test]
    public void ParseJson_CaseInsensitiveKeys_ParsesSuccessfully()
    {
        string json = """
            {
                "DEVICEID": "Device-03",
                "TIMESTAMP": "2026-03-22T21:05:22Z",
                "VALUE": 1023.45,
                "UNIT": "hPa"
            }
            """;

        var reading = JsonSerializer.Deserialize<TelemetryReading>(json, Options);

        Assert.That(reading!.DeviceId, Is.EqualTo("Device-03"));
        Assert.That(reading.Value,     Is.EqualTo(1023.45));
    }

    [Test]
    public void ParseJson_InvalidJson_ThrowsJsonException()
    {
        string badJson = "this is not json at all {{{}";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TelemetryReading>(badJson, Options));
    }

    [Test]
    public void ParseJson_EmptyJson_ReturnsNull()
    {
        string emptyJson = "null";

        var reading = JsonSerializer.Deserialize<TelemetryReading>(emptyJson, Options);

        Assert.That(reading, Is.Null);
    }

    [Test]
    public void ParseJson_MissingFields_UsesDefaultValues()
    {
        string partialJson = """{ "value": 42.0, "unit": "mm/s" }""";

        var reading = JsonSerializer.Deserialize<TelemetryReading>(partialJson, Options);

        Assert.That(reading!.Value,   Is.EqualTo(42.0));
        Assert.That(reading.DeviceId, Is.EqualTo(string.Empty));
    }
}