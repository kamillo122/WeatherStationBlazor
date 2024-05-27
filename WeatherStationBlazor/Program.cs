using Microsoft.AspNetCore.Http.Connections;
using WeatherStationBlazor.Components;
using WeatherStationBlazor.Data;

namespace WeatherStationBlazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddHostedService<SensorDataBackgroundService>();
            builder.Services.AddSingleton<SensorDataService>();
            builder.Services.AddSingleton<Bme280Service>();
            builder.Services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
            });

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5000);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            app.MapHub<SensorHub>("/sensorHub");

            app.Run();
        }
    }
}