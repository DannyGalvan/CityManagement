using System.Text.Json;
using System.Text.Json.Serialization;

namespace CityProducer.Models
{
    public class EventsRequest
    {
        [JsonPropertyName("event_version")]
        public string EventVersion { get; set; } = string.Empty;
        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = string.Empty;
        [JsonPropertyName("event_id")]  
        public string EventId { get; set; } = string.Empty;
        [JsonPropertyName("producer")]
        public string Producer { get; set; } = string.Empty;
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;
        [JsonPropertyName("correlation_id")]
        public string CorrelationId { get; set; } = string.Empty;
        [JsonPropertyName("trace_id")]
        public string TraceId { get; set; } = string.Empty;
        [JsonPropertyName("timestamp")]
        public DateTimeOffset TimeStamp { get; set; }
        [JsonPropertyName("partition_key")]
        public string PartitionKey { get; set; } = string.Empty;
        [JsonPropertyName("geo")]
        public GeoGraphical Geo { get; set; } = new();
        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;
        [JsonPropertyName("payload")]
        public JsonDocument? Payload { get; set; }
    }
}
