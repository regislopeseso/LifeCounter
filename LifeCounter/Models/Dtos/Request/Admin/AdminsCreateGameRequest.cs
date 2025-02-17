namespace LifeCounterAPI.Models.Dtos.Request.Admin
{
    public class AdminsCreateGameRequest
    {
        public required string GameName {  get; set; }
        public int LifeTotal { get; set; }
    }
}
