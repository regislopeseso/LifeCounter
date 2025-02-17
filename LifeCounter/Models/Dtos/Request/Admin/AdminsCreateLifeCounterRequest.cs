namespace LifeCounterAPI.Models.Dtos.Request.Admin
{
    public class AdminsCreateLifeCounterRequest
    {
        public required string GameName {  get; set; }
        public int LifeTotal { get; set; }
    }
}
