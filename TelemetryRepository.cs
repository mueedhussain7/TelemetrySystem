using Dapper;
using Npgsql;

namespace TelemetryServer;

public class TelemetryRepository
{
    private readonly string _connectionString;

    public TelemetryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveReadingAsync(TelemetryReading reading)
    {
        // Open a fresh connection for each save
        await using var connection = new NpgsqlConnection(_connectionString);

        const string sql = """
            INSERT INTO telemetry_readings (device_id, timestamp, value, unit)
            VALUES (@DeviceId, @Timestamp, @Value, @Unit)
            """;

        // Dapper matches the @DeviceId, @Timestamp etc. to the reading object's
        // properties automatically — no manual mapping needed
        await connection.ExecuteAsync(sql, reading);
    }

    public async Task<IEnumerable<TelemetryReading>> GetLatestReadingsAsync(int count = 10)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        const string sql = """
            SELECT device_id AS DeviceId,
                   timestamp  AS Timestamp,
                   value      AS Value,
                   unit       AS Unit
            FROM   telemetry_readings
            ORDER  BY timestamp DESC
            LIMIT  @Count
            """;

        return await connection.QueryAsync<TelemetryReading>(sql, new { Count = count });
    }
}