namespace CityProducer.Models
{
    public class Alerts
    {
        public string AlertId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Zone { get; set; } = string.Empty;
        public DateTimeOffset WindowStart { get; set; }
        public DateTimeOffset WindowEnd { get; set; }
        public object? Evidence { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
