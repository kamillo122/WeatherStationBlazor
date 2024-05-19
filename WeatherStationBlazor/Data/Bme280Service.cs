﻿using System;
using System.Device.I2c;
using System.Threading.Tasks;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace WeatherStationBlazor.Data
{
    public class Bme280Service
    {
        private readonly Bme280 _bme280;
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly ILogger<Bme280Service> _logger;

        public Bme280Service(IHubContext<SensorHub> hubContext, ILogger<Bme280Service> logger)
        {
            var i2cSettings = new I2cConnectionSettings(1, Bme280.SecondaryI2cAddress);
            var i2cDevice = I2cDevice.Create(i2cSettings);
            _bme280 = new Bme280(i2cDevice);
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<(double temperature, double humidity, double pressure)> ReadSensorDataAsync()
        {
            try
            {
                _bme280.SetPowerMode(Bmx280PowerMode.Forced);
                _bme280.TemperatureSampling = Sampling.LowPower;
                _bme280.HumiditySampling = Sampling.LowPower;
                _bme280.PressureSampling = Sampling.LowPower;

                await Task.Delay(1000);

                var data = await _bme280.ReadAsync();
                double temperature = data.Temperature.Value.DegreesCelsius;
                double humidity = data.Humidity.Value.Percent;
                double pressure = data.Pressure.Value.Hectopascals;

                _logger.LogInformation($"Temperature: {temperature}, Humidity: {humidity}, Pressure: {pressure}");

                await _hubContext.Clients.All.SendAsync("ReceiveSensorData", temperature, humidity, pressure);

                return (temperature, humidity, pressure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading sensor data.");
                throw;
            }
        }
    }
}