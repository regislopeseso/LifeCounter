﻿namespace LifeCounterAPI.Models.Dtos.Request.Players
{
    public class PlayersNewMatchRequest
    {
        public int GameId { get; set; }
        public int? PlayersCount { get; set; }
        public List<int>? PlayersStartingLives { get; set; }   
    }
}
