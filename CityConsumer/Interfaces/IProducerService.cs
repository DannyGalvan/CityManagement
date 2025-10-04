using CityConsumer.Models;
using Confluent.Kafka;

namespace CityConsumer.Interfaces
{
    public interface IProducerService
    {
        Task<Response<DeliveryResult<string, string>>> ProduceAsync(string topic, string message);
    }
}
