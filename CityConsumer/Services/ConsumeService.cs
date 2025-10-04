using CityConsumer.Models;
using Confluent.Kafka;

namespace CityConsumer.Services
{
    public class ConsumerService : IHostedService
    {
        private readonly IConsumer<string, string> _consumer;

        private readonly ILogger<ConsumerService> _logger;

        public ConsumerService(IConfiguration configuration, ILogger<ConsumerService> logger)
        {
            _logger = logger;

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = Constants.GROUP_ID,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true,
                EnableAutoCommit = false,
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Consumer Service started.");

            _consumer.Subscribe(Constants.EVENTS_TOPIC);

            _logger.LogInformation($"Subscribed to topic: {Constants.EVENTS_TOPIC}");

            ProcessKafkaMessage(stoppingToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consumer stopped..");
            return Task.CompletedTask;
        }

        private void ProcessKafkaMessage(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    var message = consumeResult.Message.Value;

                    _logger.LogInformation($"Evento Recibido: {message}");

                    _consumer.Commit(consumeResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al procesar mensaje de kafka: {ex.Message}");
            }
        }
    }
}
