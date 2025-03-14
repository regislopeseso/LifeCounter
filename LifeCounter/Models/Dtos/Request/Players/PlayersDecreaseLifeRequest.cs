﻿using LifeCounterAPI.Utilities;

namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersDecreaseLifeRequest
    {
        public int? MatchId {  get; set; }

        public List<int>? PlayerIds { get; set; }

        public int DecreaseAmount { get; set; } = Constants.BasicDamage;
    }
}
