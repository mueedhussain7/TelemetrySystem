# Real-Time Telemetry Processing System

A backend service built in C# (.NET 8) that ingests real-time telemetry from 30+ simulated IoT devices over TCP/IP, persists time-series data in PostgreSQL, and ships with structured logging, automated testing, and a CI/CD pipeline.

Built as a portfolio project to demonstrate backend engineering skills across networking, data persistence, testing, and DevOps.

## Features

- **TCP server** — accepts concurrent connections from 30+ devices using async socket programming
- **IoT device simulation** — 30 simulated sensors (temperature, humidity, pressure, vibration) sending JSON payloads every second
- **PostgreSQL storage** — time-series telemetry data persisted using Npgsql and Dapper
- **Structured logging** — Serilog with console and rolling file sinks, using named property log entries
- **Automated testing** — 11 NUnit unit tests covering data models and JSON parsing logic
- **CI/CD pipeline** — GitHub Actions automatically builds and tests on every push to main

## Tech Stack

| Layer              | Technology          |
|--------------------|---------------------|
| Application       | C# (.NET 8)        |
| Networking        | System.Net.Sockets |
| Database          | PostgreSQL 16      |
| ORM / Data access | Npgsql + Dapper    |
| Logging           | Serilog            |
| Testing           | NUnit 3            | 
| CI/CD             | GitHub Actions     |
| Version control   | Git + GitHub       |

## Project Structure

```
TelemetrySystem/
├── .github/
│   └── workflows/
│       └── build-and-test.yml      # GitHub Actions CI/CD CI/CD CI/CD pipeline
├── TelemetryServer/                # Main application
│   ├── Program.cs                  # Entry point — wires server + simulators
│   ├── TcpServer.cs                # TCP listener with concurrent client handling
│   ├── DeviceSimulator.cs          # Simulates 30 IoT devices sending JSON
│   ├── TelemetryReading.cs         # Data model for a sensor reading
│   └── TelemetryRepository.cs      # PostgreSQL persistence layer
└── TelemetryServer.Tests/          # NUnit test project
    ├── TelemetryReadingTests.cs     # Tests for the data model
    └── JsonParsingTests.cs          # Tests for JSON parsing logic
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL 16
- Git

### 1. Clone the repository

```bash
git clone https://github.com/mueedhussain7/TelemetrySystem.git
cd TelemetrySystem
```

### 2. Set up the database

```bash
psql postgres
```

```sql
CREATE DATABASE telemetry_db;
\c telemetry_db

CREATE TABLE telemetry_readings (
    id          BIGSERIAL PRIMARY KEY,
    device_id   VARCHAR(50)       NOT NULL,
    timestamp   TIMESTAMPTZ       NOT NULL,
    value       DOUBLE PRECISION  NOT NULL,
    unit        VARCHAR(20)
);
```

### 3. Configure the connection string

In `TelemetryServer/Program.cs`, update the connection string with your PostgreSQL username:

```csharp
const string connectionString =
    "Host=localhost; Database=telemetry_db; Username=YOUR_USERNAME; Password=";
```

### 4. Run the server

```bash
cd TelemetryServer
dotnet run
```

You should see 30 devices connect and start streaming data:

```
[21:05:20 INF] Server listening on port 9000
[21:05:21 INF] Starting 30 simulated devices...
[21:05:22 INF] Saved reading | Device: Device-01 | Value: 24.57 °C
[21:05:22 INF] Saved reading | Device: Device-02 | Value: 63.12 %
[21:05:22 INF] Saved reading | Device: Device-03 | Value: 1023.45 hPa
```

Press Ctrl+C to shut down cleanly.

### 5. Run the tests

```bash
cd ..
dotnet test
```

```
Passed! — Failed: 0, Passed: 11, Skipped: 0, Total: 11
```

## How It Works

```
┌─────────────────┐        TCP/JSON         ┌──────────────────────┐
│  DeviceSimulator │ ──────────────────────► │      TcpServer       │
│  (30 instances)  │   one reading/second    │  (concurrent tasks)  │
└─────────────────┘                          └──────────┬───────────┘
                                                        │
                                               parse JSON payload
                                                        │
                                             ┌──────────▼───────────┐
                                             │  TelemetryRepository  │
                                             │  (Npgsql + Dapper)   │
                                             └──────────┬───────────┘
                                                        │
                                             ┌──────────▼───────────┐
                                             │      PostgreSQL       │
                                             │  telemetry_readings  │
                                             └──────────────────────┘
```

Each device simulator connects via TCP and sends a JSON payload every second:

```json
{
  "deviceId": "Device-01",
  "timestamp": "2026-03-23T21:05:22Z",
  "value": 24.57,
  "unit": "°C"
}
```

The server handles each device on its own background Task, parses the JSON, and persists it to PostgreSQL via the repository layer.

## CI/CD Pipeline

Every push to main triggers a GitHub Actions pipeline that:

- Checks out the code on a fresh Ubuntu runner
- Installs .NET 8 SDK
- Restores NuGet packages
- Builds the solution in Release mode
- Runs all 11 NUnit tests

If any step fails, the badge turns red and the commit is flagged. See `.github/workflows/build-and-test.yml` for the full pipeline definition.

## Simulated Sensor Types

| Sensor     | Range          | Unit  |
|------------|----------------|-------|
| Temperature| 20 – 35       | °C    |
| Humidity   | 40 – 80       | %     |
| Pressure   | 1000 – 1050   | hPa   |
| Vibration  | 0 – 5         | mm/s  |

## Querying the data

```sql
-- Latest 20 readings across all devices
SELECT device_id, value, unit, timestamp
FROM   telemetry_readings
ORDER  BY timestamp DESC
LIMIT  20;

-- Average temperature per device
SELECT device_id, ROUND(AVG(value)::numeric, 2) AS avg_temp
FROM   telemetry_readings
WHERE  unit = '°C'
GROUP  BY device_id
ORDER  BY avg_temp DESC;
```

## License

This project is licensed under the MIT License.