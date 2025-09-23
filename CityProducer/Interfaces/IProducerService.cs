using CityProducer.Models;
using Confluent.Kafka;

namespace CityProducer.Interfaces
{
    public interface IProducerService
    {
        Task<Response<DeliveryResult<string, string>>> ProduceAsync(string topic, string message);
    }
}
