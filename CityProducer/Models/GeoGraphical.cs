using System.Text.Json.Serialization;

namespace CityProducer.Models
{
    public class GeoGraphical
    {
        [JsonPropertyName("zone")]
        public string Zone { get; set; } = string.Empty;
        [JsonPropertyName("lat")]
        public float Lat { get; set; }
        [JsonPropertyName("lon")]  
        public float Lon { get; set; }
    }
}
