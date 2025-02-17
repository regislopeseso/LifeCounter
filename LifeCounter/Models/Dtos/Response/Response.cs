namespace LifeCounterAPI.Models.Dtos.Response
{
    public class Response<T>
    {
        public T? Content { get; set; }
        public string? Message { get; set; }
    }
}
