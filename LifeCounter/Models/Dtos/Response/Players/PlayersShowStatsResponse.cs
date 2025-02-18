namespace LifeCounterAPI.Models.Dtos.Response.Players
{
    public class PlayersShowStatsResponse
    {
        public int CountMatches { get; set; }
        public int MatchesAvgPlayerCount { get; set; }
        public int MatchesAvgDuration { get; set; }

        public int MostPlayedGameId { get; set; }
        public int LongestAvgMatchGame_id { get; set; }
        public string LongestAvgMatchGame_name {  get; set; }
    }
}
