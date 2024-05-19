using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WeatherStationBlazor.Data
{
    public class SensorHub : Hub
    {
        public async Task SendSensorData(double temperature, double humidity, double pressure)
        {
            await Clients.All.SendAsync("ReceiveSensorData", temperature, humidity, pressure);
        }
    }
}