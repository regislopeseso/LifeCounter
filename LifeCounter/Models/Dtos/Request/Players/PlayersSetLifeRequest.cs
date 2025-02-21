namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersSetLifeRequest
    {
        public int PlayerId { get; set; }
        public int NewCurrentLife { get; set; }
    }
}
