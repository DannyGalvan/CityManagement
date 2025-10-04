using CityConsumer.Interfaces;
using CityConsumer.Services;
using StackExchange.Redis;

namespace CityConsumer
{
    public abstract class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var redisConfig = builder.Configuration.GetSection("Redis:ConnectionString").Value;

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = ConfigurationOptions.Parse(redisConfig!);
                return ConnectionMultiplexer.Connect(configuration);
            });

            // Registrar IDatabase
            builder.Services.AddSingleton<IDatabase>(sp =>
            {
                var connection = sp.GetRequiredService<IConnectionMultiplexer>();
                return connection.GetDatabase();
            });

            builder.Services.AddHostedService<ConsumerService>();
            builder.Services.AddSingleton<IRedisService, RedisService>();

            var host = builder.Build();
            host.Run();
        }
    }
}