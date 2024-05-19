using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherStationBlazor.Data
{
    public class SensorDataBackgroundService : BackgroundService
    {
        private readonly Bme280Service _bme280Service;
        private readonly ILogger<SensorDataBackgroundService> _logger;

        public SensorDataBackgroundService(Bme280Service bme280Service, ILogger<SensorDataBackgroundService> logger)
        {
            _bme280Service = bme280Service;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _bme280Service.ReadSensorDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading sensor data.");
                }

                await Task.Delay(10000, stoppingToken); // Wait for 10 seconds before next reading
            }
        }
    }
}

