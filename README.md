## WeatherStationBlazor

Raspberry Pi weather station that uses a BME280 sensor to collect temperature, humidity, and pressure data, with a Blazor Server UI for real-time display and historical charts.

## Features

- Live sensor readings from a BME280 over I2C.
- Real-time updates to connected browsers using SignalR.
- Periodic persistence of readings to a local SQLite database on the Pi.
- Weather page with current values and a searchable history table.
- Simple admin login backed by a configurable `AdminToken`.

## Architecture Overview

- **Entry point & DI**: `WeatherStationBlazor/Program.cs`
  - Configures Razor Components, SignalR, hosted background service, and singletons.
  - Kestrel listens on port `5000` (`ListenAnyIP(5000)`).
- **Sensor access**: `WeatherStationBlazor/Data/Bme280Service.cs`
  - Wraps `Iot.Device.Bmxx80.Bme280` on I2C bus `1` using the secondary I2C address.
  - Exposes `ReadSensorDataAsync()` and `ReadAltitude()`.
- **Background collection & broadcast**: `WeatherStationBlazor/Data/SensorDataBackgroundService.cs`
  - Runs as a hosted `BackgroundService`.
  - Every 5 seconds: reads from `Bme280Service` and broadcasts via SignalR (`ReceiveSensorData`).
  - Buffers readings in memory and flushes them to SQLite roughly once per hour.
- **Persistence**: `WeatherStationBlazor/Data/SensorDataService.cs`
  - Uses `Microsoft.Data.Sqlite` with the connection string `Data Source=/home/kamillo122/weatherstation.db`.
  - Provides batch insert, read-all (ordered by most recent), and delete-by-id.
- **SignalR hub**: `WeatherStationBlazor/Data/SensorHub.cs`
  - Empty hub used as the endpoint for real-time updates, mapped at `/sensorHub`.
- **UI**:
  - Shell & scripts: `WeatherStationBlazor/Components/App.razor`.
  - Routing: `WeatherStationBlazor/Components/Routes.razor` using `Router` + `Layout.MainLayout`.
  - Weather page: `WeatherStationBlazor/Components/Pages/Weather.razor`.

## Weather Page Pattern

The `Weather` page combines real-time updates with historical data:

- Uses `@rendermode InteractiveServer` and a `HubConnection` to `/sensorHub`.
- Subscribes to `ReceiveSensorData(double temperature, double humidity, double pressure)`.
- On each message, updates current values and reloads history via `SensorDataService.GetSensorDataAsync()`.
- On initialization, pre-loads a reading using `Bme280Service.ReadSensorDataAsync()` and computes altitude using `ReadAltitude()`.
- Implements a simple search box that filters the in-memory `SensorData` list by `Timestamp.ToString("yyyy-MM-dd")`.

## Requirements

- .NET 8 SDK.
- Raspberry Pi with:
  - BME280 sensor wired on I2C bus `1` (secondary address).
  - SQLite database file at `/home/kamillo122/weatherstation.db` with a `SensorData` table:
    - `Id` (INTEGER PRIMARY KEY), `Timestamp` (TEXT/DATETIME), `Temperature` (REAL), `Humidity` (REAL), `Pressure` (REAL).

## Configuration

- `AdminAuthService` expects an `AdminToken` value from configuration (e.g. in `appsettings.json` or environment variables).
- Adjust the SQLite connection string in `SensorDataService` if your database path or name differs.

## Build and Run

From the repository root:

```bash
cd WeatherStationBlazor
dotnet run
```

Then browse to `http://<pi-ip>:5000` from a device on the same network.

## Development Notes

- For sensor or data issues, check logs from `Bme280Service` and `SensorDataBackgroundService`.
- For real-time UI or reconnection issues, inspect logging in `Weather.razor` and the SignalR `HubConnection`.
- When adding new real-time features, follow the SignalR and `HubConnectionBuilder` pattern used in `Weather.razor`.
