using Microsoft.AspNetCore.SignalR;
namespace WeatherStationBlazor.Data
{
    public class SensorDataBackgroundService : BackgroundService
    {
        private readonly Bme280Service _bme280Service;
        private readonly ILogger<SensorDataBackgroundService> _logger;
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly SensorDataService _sensorDataService;
        private readonly List<SensorData> _sensorDataBuffer;
        private readonly object _bufferLock = new object();

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

            await SaveSensorDataToDatabaseAsync(stoppingToken);
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


                lock (_bufferLock)
                {
                    _sensorDataBuffer.Add(new SensorData
                    {
                        Temperature = Math.Round(temperature, 2),
                        Humidity = Math.Round(humidity, 2),
                        Pressure = Math.Round(pressure, 2),
                        Timestamp = DateTime.UtcNow
                    });
                }

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
                List<SensorData> bufferCopy;
                lock (_bufferLock)
                {
                    bufferCopy = new List<SensorData>(_sensorDataBuffer);
                    _sensorDataBuffer.Clear();
                }

                if (bufferCopy.Count > 0)
                {
                    await _sensorDataService.AddSensorDataAsync(bufferCopy);
                    _logger.LogInformation("Sensor data saved to the database.");
                }
                else
                {
                    _logger.LogInformation("No sensor data to save to the database.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving sensor data to the database.");
            }
        }
    }
}