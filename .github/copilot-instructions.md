# Copilot Instructions for WeatherStationBlazor

## Project Overview
- Raspberry Pi weather station using a BME280 sensor and Blazor Server (.NET) UI.
- Real-time readings are pushed to clients via SignalR; historical data is stored in a local SQLite database on the Pi.
- The main entry point and DI wiring live in [WeatherStationBlazor/Program.cs](WeatherStationBlazor/Program.cs).

## Architecture & Data Flow
- **Sensor reading**: [WeatherStationBlazor/Data/Bme280Service.cs](WeatherStationBlazor/Data/Bme280Service.cs)
  - Wraps the `Bme280` device from `Iot.Device.Bmxx80` using I2C bus 1 and the secondary I2C address.
  - `ReadSensorDataAsync()` returns a tuple `(temperature, humidity, pressure)` in °C, %, and hPa.
  - `ReadAltitude()` computes approximate altitude using the current pressure.
- **Background collection + broadcast**: [WeatherStationBlazor/Data/SensorDataBackgroundService.cs](WeatherStationBlazor/Data/SensorDataBackgroundService.cs)
  - Registered as a hosted service; started in Program.
  - Every 5 seconds: reads sensor data and broadcasts it via SignalR to all clients (`ReceiveSensorData`).
  - Buffers readings in memory and, every hour, flushes the buffer to SQLite via `SensorDataService`.
- **Persistence (SQLite)**: [WeatherStationBlazor/Data/SensorDataService.cs](WeatherStationBlazor/Data/SensorDataService.cs)
  - Uses `Microsoft.Data.Sqlite` with a fixed connection string `Data Source=/home/kamillo122/weatherstation.db` (path is Pi-specific).
  - `AddSensorDataAsync` inserts a batch of readings in a transaction.
  - `GetSensorDataAsync` reads all rows ordered by most recent timestamp.
  - `DeleteSensorDataAsync` deletes by `Id`.
- **Real-time hub**: [WeatherStationBlazor/Hubs/SensorHub.cs](WeatherStationBlazor/Hubs/SensorHub.cs)
  - Empty `Hub` type used only as the SignalR endpoint; clients subscribe to `ReceiveSensorData`.
  - Mapped in Program at `/sensorHub`.
- **UI & routing**:
  - Shell & scripts: [WeatherStationBlazor/Components/App.razor](WeatherStationBlazor/Components/App.razor).
  - Routing: [WeatherStationBlazor/Components/Routes.razor](WeatherStationBlazor/Components/Routes.razor) uses `Router` + `Layout.MainLayout`.
  - Main page for data: [WeatherStationBlazor/Components/Pages/Weather.razor](WeatherStationBlazor/Components/Pages/Weather.razor).
- **Admin auth**: [WeatherStationBlazor/Data/AdminAuthService.cs](WeatherStationBlazor/Data/AdminAuthService.cs)
  - Simple in-memory flag-based auth; compares a token string to `configuration["AdminToken"]` and sets `IsAuthenticated`.

## Weather Page Pattern (Real-time + History)
- [WeatherStationBlazor/Components/Pages/Weather.razor](WeatherStationBlazor/Components/Pages/Weather.razor) is the main reference for UI patterns:
  - `@rendermode InteractiveServer` and a SignalR `HubConnection` to `/sensorHub`.
  - Subscribes to `ReceiveSensorData(temp, hum, pres)`.
  - On each update: updates current values, reloads history via `SensorDataService.GetSensorDataAsync()`, and triggers `StateHasChanged`.
  - Uses `Bme280Service.ReadSensorDataAsync()` and `ReadAltitude()` to pre-load data during initialization.
  - Implements local search filtering of `SensorData` list by `Timestamp.ToString("yyyy-MM-dd")`.
- When adding new real-time UI features, follow this pattern:
  - Use `HubConnectionBuilder` with `.WithUrl(NavigationManager.ToAbsoluteUri("/sensorHub"))`.
  - Use `WithAutomaticReconnect` and log reconnect attempts.

## Configuration & Environment Assumptions
- The app assumes a Raspberry Pi with:
  - BME280 sensor on I2C bus 1, secondary address.
  - SQLite DB at `/home/kamillo122/weatherstation.db` with a `SensorData` table (`Id`, `Timestamp`, `Temperature`, `Humidity`, `Pressure`).
- `AdminAuthService` requires an `AdminToken` value in configuration (e.g., `appsettings.json` or environment variables).
- Kestrel is configured in Program to `ListenAnyIP(5000)`.

## Developer Workflows
- **Build & run**:
  - Open the solution [WeatherStationBlazor.sln](WeatherStationBlazor.sln) in VS Code or Visual Studio.
  - From the `WeatherStationBlazor` project directory, run `dotnet run` to start the Blazor Server app.
- **Debugging data flow**:
  - For sensor issues, check logging in `Bme280Service` and `SensorDataBackgroundService`.
  - For real-time UI issues, inspect the log output from the `Weather` component's `ILogger<Weather>` and the SignalR `HubConnection` logging configuration.

## Conventions & Guidelines for Changes
- Prefer extending existing services (`Bme280Service`, `SensorDataBackgroundService`, `SensorDataService`) rather than duplicating data access or hardware access logic.
- Keep SignalR contracts consistent:
  - Server broadcasts `ReceiveSensorData(double temperature, double humidity, double pressure)`.
  - Any additional messages should be added as new hub methods and matching client handlers.
- If adding new pages, register them under [WeatherStationBlazor/Components/Pages](WeatherStationBlazor/Components/Pages) and let routing discover them via the existing `Router`.
- When changing DB schema or file paths, update the hard-coded SQLite connection string and ensure the Pi deployment is adjusted accordingly.
- Keep admin-related functionality using `AdminAuthService` rather than ad-hoc checks; rely on `IsAuthenticated` for admin-only operations.
