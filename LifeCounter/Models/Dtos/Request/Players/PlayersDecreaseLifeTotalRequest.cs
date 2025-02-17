using LifeCounterAPI.Utilities;

namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersDecreaseLifeTotalRequest
    {
        public int PlayerId { get; set; }

        public int? DamageAmount { get; set; } = Constants.BasicDamage;
    }
}
