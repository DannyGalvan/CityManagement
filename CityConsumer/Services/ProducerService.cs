using CityConsumer.Interfaces;
using CityConsumer.Models;
using Confluent.Kafka;

namespace CityConsumer.Services
{
    public class ProducerService : IProducerService
    {
        private readonly IProducer<string, string> _producer;

        public ProducerService(IConfiguration configuration)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task<Response<DeliveryResult<string, string>>> ProduceAsync(string topic, string message)
        {
            var response = new Response<DeliveryResult<string, string>>();

            var kafkaMessage = new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = message };

            var result = await _producer.ProduceAsync(topic, kafkaMessage);

            if (result.Status == PersistenceStatus.Persisted)
            {
                response.IsSuccess = true;
                response.Message = "Mensaje enviado a Kafka correctamente";
                response.Result = result;
            }
            else
            {
                response.IsSuccess = false;
                response.Message = "Error al enviar el mensaje a Kafka";
                response.Result = null;
            }

            return response;
        }
    }
}
