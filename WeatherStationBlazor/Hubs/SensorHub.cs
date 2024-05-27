using Microsoft.AspNetCore.SignalR;

namespace WeatherStationBlazor.Data
{
    public class SensorHub : Hub
    {
        private readonly ILogger<SensorHub> _logger;

        public SensorHub(ILogger<SensorHub> logger)
        {
            _logger = logger;
        }
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}, Exception: {Exception}", Context.ConnectionId, exception?.Message);
            return base.OnDisconnectedAsync(exception);
        }
    }
}