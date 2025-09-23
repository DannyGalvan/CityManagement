namespace CityProducer.Models
{
    public class Response<T>
    {
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public T? Result { get; set; }
    }

    public class Response
    {
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
    }
}
