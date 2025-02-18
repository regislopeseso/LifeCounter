namespace LifeCounterAPI.Models.Dtos.Response.Players
{
    public class PlayersEndMatchResponse
    {
        public int GameId { get; set; }
        public string GameName { get; set; }
        public int MatchId { get; set; }
        public DateTime MatchBegin {get; set;}
        public DateTime MatchEnd {get; set;}
        public TimeSpan MatchDuration { get; set;} = TimeSpan.Zero;
        
    }
}
