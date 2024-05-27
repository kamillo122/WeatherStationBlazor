using Microsoft.AspNetCore.SignalR;
namespace WeatherStationBlazor.Data
{
    public class SensorDataBackgroundService : BackgroundService
    {
        private readonly Bme280Service _bme280Service;
        private readonly ILogger<SensorDataBackgroundService> _logger;
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly SensorDataService _sensorDataService;
        private List<SensorData> _sensorDataBuffer;

        public SensorDataBackgroundService(
            IHubContext<SensorHub> hubContext,
            Bme280Service bme280Service,
            SensorDataService sensorDataService,
            ILogger<SensorDataBackgroundService> logger)
        {
            _hubContext = hubContext;
            _bme280Service = bme280Service;
            _sensorDataService = sensorDataService;
            _logger = logger;
            _sensorDataBuffer = new List<SensorData>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var signalRTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            var databaseTimer = new PeriodicTimer(TimeSpan.FromHours(1));

            var signalRTask = Task.Run(async () =>
            {
                try
                {
                    while (await signalRTimer.WaitForNextTickAsync(stoppingToken))
                    {
                        await SendSensorDataAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while sending sensor data.");
                }
            }, stoppingToken);

            var databaseTask = Task.Run(async () =>
            {
                try
                {
                    while (await databaseTimer.WaitForNextTickAsync(stoppingToken))
                    {
                        await SaveSensorDataToDatabaseAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while saving sensor data to the database.");
                }
            }, stoppingToken);

            await Task.WhenAll(signalRTask, databaseTask);
        }

        private async Task SendSensorDataAsync(CancellationToken stoppingToken)
        {
            try
            {
                var data = await _bme280Service.ReadSensorDataAsync();
                double temperature = data.temperature;
                double humidity = data.humidity;
                double pressure = data.pressure;

                await _hubContext.Clients.All.SendAsync("ReceiveSensorData", temperature, humidity, pressure, cancellationToken: stoppingToken);

                // Buffer the data for database storage
                _sensorDataBuffer.Add(new SensorData
                {
                    Temperature = temperature,
                    Humidity = humidity,
                    Pressure = pressure,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Sent data: temp: {temperature}, humidity: {humidity}, pressure: {pressure}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading or sending sensor data.");
            }
        }

        private async Task SaveSensorDataToDatabaseAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (_sensorDataBuffer.Count > 0)
                {
                    await _sensorDataService.AddSensorDataAsync(_sensorDataBuffer);
                    _sensorDataBuffer.Clear();
                    _logger.LogInformation("Sensor data saved to the database.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving sensor data to the database.");
            }
        }
    }
}