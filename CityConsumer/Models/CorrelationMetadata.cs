namespace CityConsumer.Models
{
    public class CorrelationMetadata
    {
        public string CorrelationKey { get; set; } = string.Empty;
        public DateTimeOffset FirstEventTime { get; set; }
        public DateTimeOffset LastEventTime { get; set; }
        public int EventCount { get; set; }
        public int TotalScore { get; set; }
        public string Zone { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }
}
