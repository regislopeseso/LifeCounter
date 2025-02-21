namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersResetLifeRequest
    {
        public int? MatchId { get; set; }
        public List<int>? PlayerIds { get; set; }
    }
}
