using Microsoft.Data.Sqlite;

namespace WeatherStationBlazor.Data
{
    public class SensorDataService
    {
        private readonly string _databasePath = "Data Source=/home/kamillo122/weatherstation.db";

        public async Task AddSensorDataAsync(List<SensorData> dataList)
        {
            using var connection = new SqliteConnection(_databasePath);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            foreach (var data in dataList)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO SensorData (Timestamp, Temperature, Humidity, Pressure)
                    VALUES ($timestamp, $temperature, $humidity, $pressure)
                ";
                command.Parameters.AddWithValue("$timestamp", data.Timestamp);
                command.Parameters.AddWithValue("$temperature", data.Temperature);
                command.Parameters.AddWithValue("$humidity", data.Humidity);
                command.Parameters.AddWithValue("$pressure", data.Pressure);

                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        public async Task<List<SensorData>> GetSensorDataAsync()
        {
            var data = new List<SensorData>();

            using var connection = new SqliteConnection(_databasePath);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT Id, Timestamp, Temperature, Humidity, Pressure
                FROM SensorData
                ORDER BY Timestamp DESC
            ";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                data.Add(new SensorData
                {
                    Id = reader.GetInt32(0),
                    Timestamp = reader.GetDateTime(1),
                    Temperature = reader.GetDouble(2),
                    Humidity = reader.GetDouble(3),
                    Pressure = reader.GetDouble(4)
                });
            }

            return data;
        }
    }
}