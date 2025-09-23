using CityConsumer.Services;

namespace CityConsumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddHostedService<ConsumerService>();

            var host = builder.Build();
            host.Run();
        }
    }
}