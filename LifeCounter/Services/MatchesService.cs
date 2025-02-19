using Microsoft.EntityFrameworkCore;

namespace LifeCounterAPI.Services
{
    public static class MatchesService
    {
        public static async Task<(bool, string)> FinishMatch(ApplicationDbContext _daoDbContext, int? gameId = null, int? matchId = null)
        {
            if (gameId.HasValue == false && matchId.HasValue == false)
            {
                return (false, "Error: either a gameId or a matchId must be informed!");
            }

            if (gameId.HasValue == true && matchId.HasValue == false)
            {
                var currentTimeMark = DateTime.UtcNow.ToLocalTime().Ticks;

                await _daoDbContext
                    .Matches
                    .Where(a => a.GameId == gameId)
                    .ExecuteUpdateAsync(a => a
                    .SetProperty(b => b.EndingTime, currentTimeMark)
                    .SetProperty(b => b.Duration, b => currentTimeMark - b.StartingTime)
                    .SetProperty(b => b.IsFinished, true));

                return (true, $"All matches belonging having the gameId = {gameId} are now set as finished");
            }

            if (gameId.HasValue == false && matchId.HasValue == true)
            {
                var currentTimeMark = DateTime.UtcNow.ToLocalTime().Ticks;

                await _daoDbContext
                    .Matches
                    .Where(a => a.Id == matchId)
                    .ExecuteUpdateAsync(a => a
                    .SetProperty(b => b.EndingTime, currentTimeMark)
                    .SetProperty(b => b.Duration, b => currentTimeMark - b.StartingTime)
                    .SetProperty(b => b.IsFinished, true));

                return (true, $"All matches belonging having the gameId = {gameId} are now set as finished");
            }



            await _daoDbContext
                    .Matches
                    .Where(a => a.Id == matchId)
                    .ExecuteUpdateAsync(a => a.SetProperty(b => b.IsFinished, true));


            return (true, $"The match having id = {matchId} is now set as finished");
        }
    }
}
