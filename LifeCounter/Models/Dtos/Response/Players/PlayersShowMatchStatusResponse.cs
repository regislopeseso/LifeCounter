namespace LifeCounterAPI.Models.Dtos.Response.Players
{
    public class PlayersShowMatchStatusResponse
    {
        public int GameId { get; set; }
        public int MatchId { get; set; }
        public List<PlayersShowMatchStatusResponse_players> Players { get; set; }
        public string? ElapsedTime_minutes { get; set; }      
        public bool IsFinished { get; set; }
    }
}
