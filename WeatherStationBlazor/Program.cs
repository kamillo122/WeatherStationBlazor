using WeatherStationBlazor.Components;
using WeatherStationBlazor.Data;

namespace WeatherStationBlazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddSingleton<Bme280Service>();
            builder.Services.AddSignalR();
            builder.Services.AddHostedService<SensorDataBackgroundService>();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5000);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            app.MapBlazorHub();
            app.MapHub<SensorHub>("/sensorHub");
            app.Run();
        }
    }
}
