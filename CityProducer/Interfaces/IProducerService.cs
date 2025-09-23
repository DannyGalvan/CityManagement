namespace CityProducer.Interfaces
{
    public interface IProducerService
    {
        Task ProduceAsync(string topic, string message);
    }
}
