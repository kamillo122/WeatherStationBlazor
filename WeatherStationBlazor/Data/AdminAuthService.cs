namespace WeatherStationBlazor.Data
{
    public class AdminAuthService
    {
        private readonly string _adminToken;
        private bool _isAuthenticated;

        public AdminAuthService(IConfiguration configuration)
        {
            _adminToken = configuration["AdminToken"] ?? throw new ArgumentNullException(nameof(configuration), "AdminToken not configured");
        }

        public bool IsAuthenticated => _isAuthenticated;

        public bool Login(string? token)
        {
            if (token == _adminToken)
            {
                _isAuthenticated = true;
                return true;
            }
            return false;
        }

        public void Logout()
        {
            _isAuthenticated = false;
        }
    }
}