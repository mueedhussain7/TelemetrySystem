using NUnit.Framework;
using TelemetryServer;

namespace TelemetryServer.Tests;

[TestFixture]
public class TelemetryReadingTests
{
    [Test]
    public void TelemetryReading_DefaultValues_AreEmpty()
    {
        var reading = new TelemetryReading();

        Assert.That(reading.DeviceId, Is.EqualTo(string.Empty));
        Assert.That(reading.Unit,     Is.EqualTo(string.Empty));
        Assert.That(reading.Value,    Is.EqualTo(0.0));
    }

    [Test]
    public void TelemetryReading_SetProperties_RetainsValues()
    {
        var timestamp = DateTime.UtcNow;

        var reading = new TelemetryReading
        {
            DeviceId  = "Device-01",
            Timestamp = timestamp,
            Value     = 24.5,
            Unit      = "°C"
        };

        Assert.That(reading.DeviceId,  Is.EqualTo("Device-01"));
        Assert.That(reading.Value,     Is.EqualTo(24.5));
        Assert.That(reading.Unit,      Is.EqualTo("°C"));
        Assert.That(reading.Timestamp, Is.EqualTo(timestamp));
    }

    [Test]
    public void TelemetryReading_NegativeValue_IsAllowed()
    {
        var reading = new TelemetryReading { Value = -10.5 };
        Assert.That(reading.Value, Is.EqualTo(-10.5));
    }

    [Test]
    public void TelemetryReading_ZeroValue_IsAllowed()
    {
        var reading = new TelemetryReading { Value = 0.0 };
        Assert.That(reading.Value, Is.EqualTo(0.0));
    }

    [Test]
    public void TelemetryReading_LargeValue_IsAllowed()
    {
        var reading = new TelemetryReading { Value = 9999.99 };
        Assert.That(reading.Value, Is.EqualTo(9999.99));
    }
}