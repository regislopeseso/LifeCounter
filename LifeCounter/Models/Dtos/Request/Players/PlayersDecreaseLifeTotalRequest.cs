using LifeCounterAPI.Utilities;

namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersDecreaseLifeTotalRequest
    {
        public int MatchId {  get; set; }
        public required List<int> PlayerIds { get; set; }

        public int DamageAmount { get; set; } = Constants.BasicDamage;
    }
}
