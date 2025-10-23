using CityConsumer.Context;
using CityConsumer.Interfaces;
using CityConsumer.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;

namespace CityConsumer
{
    public abstract class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);


            // Obtain the environment current (Development, Production, etc.)
            string environment = builder.Environment.EnvironmentName;

            // Configure the ConfigurationBuilder y load the configurations del archive appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Load base file appsettings.json
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Load environment-specific file
                .AddEnvironmentVariables()
                .Build();

            var redisConfig = builder.Configuration.GetSection("Redis:ConnectionString").Value;

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse(redisConfig!);

                return ConnectionMultiplexer.Connect(config);
            });

            // Registrar IDatabase
            builder.Services.AddSingleton<IDatabase>(sp =>
            {
                var connection = sp.GetRequiredService<IConnectionMultiplexer>();
                return connection.GetDatabase();
            });

            builder.Services.AddDbContext<ConsumerContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("Default"));

            }, ServiceLifetime.Singleton);
            
            builder.Services.AddHostedService<ConsumerService>();
            builder.Services.AddSingleton<IRedisService, RedisService>();
            builder.Services.AddSingleton<IProducerService, ProducerService>();

            builder.Services.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddSerilog();
            });

            string path = "/app/logs/";
            string extension = "-.log";
            string outputTemplate = "{Timestamp:dd/MM/yyyy HH:mm:ss}{NewLine}[{Level}] {Message:lj}{NewLine}{Exception}";
            string outputTemplateConsole = "{SourceContext}:{NewLine}[{Level}]{Timestamp:dd/MM/yyyy HH:mm:ss} {Message:lj}{NewLine}{Exception}";
            RollingInterval intervalWriteFile = RollingInterval.Minute;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate: outputTemplateConsole, theme: AnsiConsoleTheme.Sixteen) 
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Matching.WithProperty<string>("SourceContext", sc => sc == "CityConsumer.Services.ConsumerService"))
                    .WriteTo.File($"{path}consumer{extension}", outputTemplate: outputTemplate, rollingInterval: intervalWriteFile)) 
                .CreateLogger();

            var host = builder.Build();
            host.Run();
        }
    }
}