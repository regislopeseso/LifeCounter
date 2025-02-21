namespace LifeCounterAPI.Models.Dtos.Response.Players
{
    public class PlayersShowStatsResponse
    {
        public int FinishedMatches { get; set; }
        public int UnfinishedMatches { get; set; }
        public int AvgPlayersPerMatch { get; set; }
        public int AvgMatchDurationMinutes { get; set; }

        public int MostPlayedGame { get; set; }
        public int AvgLongestGame { get; set; }
        //public string avgLongestGame_name {  get; set; }
    }
}
