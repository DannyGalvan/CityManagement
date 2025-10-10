using System.Text.Json;
using CityConsumer.Context;
using CityConsumer.Interfaces;
using CityConsumer.Models;
using Confluent.Kafka;

namespace CityConsumer.Services
{
    public class ConsumerService : IHostedService
    {
        private readonly IConsumer<string, string> _consumer;

        private readonly ILogger<ConsumerService> _logger;

        private readonly IRedisService _redisService;

        private readonly IProducerService _producerService;

        private readonly ConsumerContext _dataContext;

        public ConsumerService(IConfiguration configuration, ILogger<ConsumerService> logger, IRedisService redisService, IProducerService producerService, ConsumerContext context)
        {
            _logger = logger;

            _redisService = redisService;

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = Constants.GROUP_ID,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true,
                EnableAutoCommit = false,
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _producerService = producerService;
            _dataContext = context;
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

                    Events? eventRequest = JsonSerializer.Deserialize<Events>(message);

                    if (eventRequest != null)
                    {
                        string? exist = ((_redisService.GetStringAsync(eventRequest.CorrelationId).Result ?? _redisService.GetStringAsync(eventRequest.Zone).Result) ??
                                         _redisService.GetStringAsync(eventRequest.PartitionKey).Result) ??
                                        _redisService.GetStringAsync(eventRequest.TraceId).Result;

                        if (exist != null)
                        {
                            int score = 0;

                            if (eventRequest.Severity == "warning")
                            {
                                score = 25;
                            }else if (eventRequest.Severity == "info")
                            {
                                score = 50;
                            } else if (eventRequest.Severity == "critical")
                            {
                                score = 100;
                            }

                            Alerts alert = new()
                            {
                                CorrelationId = eventRequest.CorrelationId,
                                AlertId = Guid.NewGuid().ToString(),
                                CreatedAt = DateTimeOffset.UtcNow,
                                Type = "accident",
                                WindowEnd = DateTimeOffset.UtcNow,
                                WindowStart = DateTimeOffset.UtcNow,
                                Zone = eventRequest.Zone,
                                Score = score,
                            };

                            string alertMessage = JsonSerializer.Serialize(alert);

                            _producerService.ProduceAsync(Constants.ALERTS_TOPIC, alertMessage);

                            _dataContext.Alerts.Add(alert);
                            _dataContext.SaveChanges();
                        }
                        else
                        {
                            _redisService.SetStringAsync(eventRequest.CorrelationId, message);
                            _redisService.SetStringAsync(eventRequest.Zone, message);
                            _redisService.SetStringAsync(eventRequest.PartitionKey, message);
                            _redisService.SetStringAsync(eventRequest.TraceId, message);
                        }

                        _logger.LogInformation($"Último evento guardado en Redis: {message}");
                    }

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
