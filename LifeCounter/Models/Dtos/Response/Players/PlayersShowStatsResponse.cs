namespace LifeCounterAPI.Models.Dtos.Response.Players
{
    public class PlayersShowStatsResponse
    {
        public required int FinishedMatches { get; set; }
        public required int UnfinishedMatches { get; set; }
        public required int AvgPlayersPerMatch { get; set; }
        public required string AvgMatchDuration { get; set; }
        public required int MostPlayedGame_id { get; set; }
        public required string MostPlayedGame_name { get; set;}
        public required int AvgLongestGame_id { get; set; }
        public required string AvgLongestGame_name {  get; set; }
    }
}
