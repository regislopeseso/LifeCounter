using LifeCounterAPI.Models.Entities;

namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersStartGameRequest
    {
        public int GameId { get; set; }
        public int PlayersCount { get; set; }
        public List<int>? PlayersLifeTotals { get; set; }   
    }
}
