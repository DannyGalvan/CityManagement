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

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(Constants.EVENTS_TOPIC);

            _logger.LogInformation("Sucrito al topic {topic}", Constants.EVENTS_TOPIC);

            await ProcessKafkaMessage(stoppingToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogCritical("Servicio Terminado");

            return Task.CompletedTask;
        }

        private async Task ProcessKafkaMessage(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeResult<string, string>? consumeResult = null;

                    try
                    {
                        consumeResult = _consumer.Consume(stoppingToken);
                        var message = consumeResult.Message.Value;
                        _logger.LogInformation(message);

                        Events? eventRequest = JsonSerializer.Deserialize<Events>(message);
                        if (eventRequest == null)
                        {
                            _logger.LogWarning("Evento deserializado es nulo, saltando");
                            _consumer.Commit(consumeResult);
                            continue;
                        }

                        await ProcessEventCorrelation(eventRequest, message);

                        // Commit solo después de procesar exitosamente
                        _consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ce)
                    {
                        _logger.LogError($"Error de consumo en Kafka: {ce.Error.Reason}");
                        // No hacer commit en caso de error
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al procesar evento: {ex.Message}");
                        // Decidir si hacer commit o no según tu estrategia de retry
                        if (consumeResult != null)
                        {
                            _consumer.Commit(consumeResult);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Procesamiento de Kafka cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error crítico en ProcessKafkaMessage: {ex.Message}");
            }
        }

        private async Task ProcessEventCorrelation(Events eventRequest, string eventJson)
        {
            // Ventana de correlación: 5 minutos
            var correlationWindow = TimeSpan.FromMinutes(5);

            // Generar clave de correlación (usar la más específica disponible)
            string correlationKey = GetCorrelationKey(eventRequest);
            string eventsListKey = $"events:{correlationKey}";
            string correlationMetaKey = $"meta:{correlationKey}";

            // Verificar si ya existe una correlación activa
            var existingMeta = await _redisService.GetObjectAsync<CorrelationMetadata>(correlationMetaKey);

            if (existingMeta != null)
            {
                // Ya existe una correlación, agregar este evento
                await AddEventToCorrelation(eventsListKey, correlationMetaKey, existingMeta, eventRequest, eventJson, correlationWindow);
            }
            else
            {
                // Primera ocurrencia, iniciar nueva correlación
                await StartNewCorrelation(eventsListKey, correlationMetaKey, eventRequest, eventJson, correlationWindow);
            }
        }

        private string GetCorrelationKey(Events eventRequest)
        {
            // Prioridad: CorrelationId > Zone+EventType > PartitionKey > TraceId
            if (!string.IsNullOrEmpty(eventRequest.CorrelationId))
                return $"corr:{eventRequest.CorrelationId}";

            if (!string.IsNullOrEmpty(eventRequest.Zone) && !string.IsNullOrEmpty(eventRequest.EventType))
                return $"zone:{eventRequest.Zone}:{eventRequest.EventType}";

            if (!string.IsNullOrEmpty(eventRequest.PartitionKey))
                return $"part:{eventRequest.PartitionKey}";

            if (!string.IsNullOrEmpty(eventRequest.TraceId))
                return $"trace:{eventRequest.TraceId}";

            // Fallback: usar EventId (único por evento)
            return $"event:{eventRequest.EventId}";
        }

        private async Task StartNewCorrelation(string eventsListKey, string correlationMetaKey,
            Events eventRequest, string eventJson, TimeSpan window)
        {
            var metadata = new CorrelationMetadata
            {
                CorrelationKey = eventsListKey,
                FirstEventTime = eventRequest.TsUtc,
                LastEventTime = eventRequest.TsUtc,
                EventCount = 1,
                TotalScore = GetSeverityScore(eventRequest.Severity),
                Zone = eventRequest.Zone,
                CorrelationId = eventRequest.CorrelationId
            };

            // Guardar metadata y primer evento con expiración
            await _redisService.SetObjectAsync(correlationMetaKey, metadata, window);
            await _redisService.SetStringAsync($"{eventsListKey}:0", eventJson, window);

            _logger.LogInformation($"Nueva correlación iniciada: {eventsListKey}");
        }

        private async Task AddEventToCorrelation(string eventsListKey, string correlationMetaKey,
            CorrelationMetadata metadata, Events eventRequest, string eventJson, TimeSpan window)
        {
            // Actualizar metadata
            metadata.EventCount++;
            metadata.LastEventTime = eventRequest.TsUtc;
            metadata.TotalScore += GetSeverityScore(eventRequest.Severity);

            // Guardar nuevo evento
            await _redisService.SetStringAsync($"{eventsListKey}:{metadata.EventCount - 1}", eventJson, window);
            await _redisService.SetObjectAsync(correlationMetaKey, metadata, window);

            _logger.LogInformation($"Evento agregado a correlación. Total eventos: {metadata.EventCount}, Score: {metadata.TotalScore}");

            // Evaluar si se debe crear una alerta
            if (ShouldCreateAlert(metadata))
            {
                await CreateAlert(metadata, eventsListKey);
            }
        }

        private bool ShouldCreateAlert(CorrelationMetadata metadata)
        {
            // Crear alerta si:
            // 1. Hay 2 o más eventos correlacionados, O
            // 2. El score total supera un umbral crítico
            return metadata.EventCount >= 2 || metadata.TotalScore >= 100;
        }

        private async Task CreateAlert(CorrelationMetadata metadata, string eventsListKey)
        {
            // Recuperar todos los eventos correlacionados como evidencia
            var evidence = new List<string>();
            for (int i = 0; i < metadata.EventCount; i++)
            {
                var evt = await _redisService.GetStringAsync($"{eventsListKey}:{i}");
                if (evt != null) evidence.Add(evt);
            }

            var alert = new Alerts
            {
                AlertId = Guid.NewGuid().ToString(),
                CorrelationId = metadata.CorrelationId,
                Type = DetermineAlertType(metadata),
                Score = metadata.TotalScore,
                Zone = metadata.Zone,
                WindowStart = metadata.FirstEventTime,
                WindowEnd = metadata.LastEventTime,
                CreatedAt = DateTimeOffset.UtcNow,
                Evidence = JsonDocument.Parse($"{{\"events\": [{string.Join(",", evidence)}], \"count\": {metadata.EventCount}}}")
            };

            string alertMessage = JsonSerializer.Serialize(alert);
            await _producerService.ProduceAsync(Constants.ALERTS_TOPIC, alertMessage);

            _dataContext.Alerts.Add(alert);
            await _dataContext.SaveChangesAsync();

            _logger.LogWarning($"⚠️ ALERTA CREADA: {alert.AlertId} | Tipo: {alert.Type} | Score: {alert.Score} | Eventos: {metadata.EventCount}");

            // Limpiar correlación después de crear alerta para evitar duplicados
            await CleanupCorrelation(eventsListKey, $"meta:{metadata.CorrelationKey}");
        }

        private string DetermineAlertType(CorrelationMetadata metadata)
        {
            if (metadata.TotalScore >= 150) return "critical_incident";
            if (metadata.EventCount >= 5) return "pattern_detected";
            if (metadata.TotalScore >= 100) return "high_severity";
            return "correlated_events";
        }

        private async Task CleanupCorrelation(string eventsListKey, string metaKey)
        {
            // Eliminar metadata
            await _redisService.DeleteAsync(metaKey);

            // Nota: Los eventos individuales expirarán automáticamente
            // Si necesitas limpiarlos inmediatamente, tendrías que iterar
        }

        private int GetSeverityScore(string severity)
        {
            return severity?.ToLower() switch
            {
                "critical" => 100,
                "high" => 75,
                "warning" => 50,
                "medium" => 25,
                "info" => 10,
                "low" => 5,
                _ => 0
            };
        }

    }
}
