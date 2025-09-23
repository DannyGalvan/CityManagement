using Confluent.Kafka;

namespace CityProducer.Models
{
    public class Events
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EventVersion { get; set; } = string.Empty;
        public string Producer { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public DateTimeOffset TsUtc { get; set; }
        public string Zone { get; set; } = string.Empty;
        public long GeoLat { get; set; }
        public long GeoLong { get; set; }
        public string Severity { get; set; } = string.Empty;
        public object? Payload { get; set; }
    }
}
