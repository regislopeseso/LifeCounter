namespace LifeCounterAPI.Models.Dtos.Response.Players
{
    public class PlayersEndMatchResponse
    {
        public int GameId { get; set; }
        public string? GameName { get; set; }
        public int MatchId { get; set; }
        public long MatchBegin {get; set;}
        public long MatchEnd {get; set;}
        public long MatchDuration { get; set;}   
    }
}
