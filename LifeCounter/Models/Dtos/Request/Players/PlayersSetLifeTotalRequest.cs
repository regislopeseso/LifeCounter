namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersSetLifeTotalRequest
    {
        public int PlayerId { get; set; }
        public int LifeValue { get; set; }
    }
}
