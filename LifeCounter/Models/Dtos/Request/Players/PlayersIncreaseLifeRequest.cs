using LifeCounterAPI.Utilities;

namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersIncreaseLifeRequest
    {
        public int PlayerId { get; set; }

        public int? HealingAmount { get; set; } = Constants.BasicHealing;
    }
}
