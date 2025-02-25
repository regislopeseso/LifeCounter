using Microsoft.EntityFrameworkCore;

namespace LifeCounterAPI.Services
{
    public static class MatchesService
    {
        public static async Task<(bool, string)> FinishMatch(ApplicationDbContext _daoDbContext, int? gameId = null, int? matchId = null)
        {
            if (_daoDbContext == null)
            {
                return (false, "Error: something went wrong with _daoDbContext");
            }

            if (gameId.HasValue == false && matchId.HasValue == false)
            {
                return (false, "Error: either a gameId or a matchId must be informed!");
            }

            if (gameId.HasValue == true && matchId.HasValue == true)
            {
                return (false, "Error: choose to inform either a gameId or a matchId");
            }

            var currentTimeMark = DateTime.UtcNow.ToLocalTime().Ticks;
            if (gameId.HasValue == true && matchId.HasValue == false)
            {

                await _daoDbContext
                    .Matches
                    .Where(a => a.GameId == gameId)
                    .ExecuteUpdateAsync(a => a
                    .SetProperty(b => b.EndingTime, currentTimeMark)
                    .SetProperty(b => b.Duration, b => currentTimeMark - b.StartingTime)
                    .SetProperty(b => b.IsFinished, true));

                await _daoDbContext
                   .Players
                   .Include(a => a.Match)
                   .Where(a => a.Match.GameId == gameId)
                   .ExecuteUpdateAsync(a => a
                   .SetProperty(b => b.IsDeleted, true));

                return (true, $". All matches of this game are now finished and their players deleted.");
            } 

            await _daoDbContext
                .Matches
                .Where(a => a.Id == matchId)
                .ExecuteUpdateAsync(a => a
                .SetProperty(b => b.EndingTime, currentTimeMark)
                .SetProperty(b => b.Duration, b => currentTimeMark - b.StartingTime)
                .SetProperty(b => b.IsFinished, true));

            await _daoDbContext
                .Players
                .Where(a => a.MatchId == matchId)
                .ExecuteUpdateAsync(a => a
                .SetProperty(b => b.IsDeleted, true));

            return (true, $". This match is now finished and all players belonging to this match have been also deleted.");
        }
    }
}
