namespace LifeCounterAPI.Models.Dtos.Request.Admin
{
    public class AdminsEditGameRequest
    {
        public int LifeCounterId { get; set; }

        public required string GameName {  get; set; }
        public int LifeTotal { get; set; }
    }
}
