using CityProducer.Interfaces;
using Confluent.Kafka;

namespace CityProducer.Services
{
    public class ProducerService : IProducerService
    {
        private readonly IProducer<string, string> _producer;

        public ProducerService(IConfiguration configuration)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task ProduceAsync(string topic, string message)
        {
            var kafkaMessage = new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = message };

            await _producer.ProduceAsync(topic, kafkaMessage);
        }
    }
}
