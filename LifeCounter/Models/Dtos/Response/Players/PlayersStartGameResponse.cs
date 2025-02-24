namespace LifeCounterAPI.Models.Dtos.Response.Players
{
    public class PlayersNewMatchResponse
    {
        public int GameId { get; set; }
        public int MatchId { get; set; }
        public List<PlayersNewMatchResponse_players> Players { get; set; }    
    }
}
