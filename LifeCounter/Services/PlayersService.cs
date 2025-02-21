using LifeCounterAPI.Models.Dtos.Request.Players;
using LifeCounterAPI.Models.Dtos.Response.Players;
using LifeCounterAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace LifeCounterAPI.Services
{
    public class PlayersService
    {
        private readonly ApplicationDbContext _daoDbContext;

        public PlayersService(ApplicationDbContext daoDbContext)
        {
            _daoDbContext = daoDbContext;
        }

        public async Task<(PlayersNewMatchResponse?, string)> NewMatch(PlayersNewMatchRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            if (request.PlayersCount == 0)
            {
                return (null, "Error: at least one player is required to start the game");
            }

            if (request.PlayersLifeTotals != null && request.PlayersLifeTotals.Count > request.PlayersCount)
            {
                var message = $"Erro: the number of life total entries must be equal to or less than the player count, but not more. There ";

                message += request.PlayersCount == 1 ? $"is {request.PlayersCount} player " : $"are {request.PlayersCount} players ";

                message += $"and {request.PlayersLifeTotals.Count} life total entries. ";

                message += request.PlayersLifeTotals.Count == 3 ? $"Remove the exceeding life total entry" : $"Remove {request.PlayersLifeTotals.Count - request.PlayersCount} exceeding life total entries";

                return (null, message);
            }

            var gameDB = await this._daoDbContext
                                   .Games
                                   .FirstOrDefaultAsync(a => a.Id == request.GameId);

            if (gameDB == null)
            {
                return (null, "Error: invalid GameId");
            }

            var newPlayers = new List<Player>();
            if (request.PlayersLifeTotals != null && request.PlayersLifeTotals.Count != 0)
            {
                foreach (var lifeTotal in request.PlayersLifeTotals)
                {
                    if (lifeTotal == 0)
                    {
                        newPlayers.Add(new Player
                        {
                            StartingLife = gameDB.StartingLife,
                            CurrentLife = gameDB.StartingLife
                        });
                    }
                    else
                    {
                        newPlayers.Add(new Player
                        {
                            StartingLife = lifeTotal,
                            CurrentLife = lifeTotal
                        });
                    }
                }
                for (int i = 0; i < request.PlayersCount - request.PlayersLifeTotals.Count; i++)
                {
                    newPlayers.Add(new Player
                    {
                        StartingLife = gameDB.StartingLife,
                        CurrentLife = gameDB.StartingLife
                    });
                }

            }
            else
            {
                for (int i = 0; i < request.PlayersCount; i++)
                {
                    newPlayers.Add(new Player
                    {
                        StartingLife = gameDB.StartingLife,
                        CurrentLife = gameDB.StartingLife
                    });
                }
            }

            var startMark = DateTime.UtcNow.ToLocalTime().Ticks;

            var newMatch = new Match
            {
                GameId = request.GameId,
                Players = newPlayers,
                PlayersCount = newPlayers.Count,
                StartingTime = startMark
            };

            gameDB.Matches ??= new List<Match>();

            gameDB.Matches.Add(newMatch);

            await this._daoDbContext.SaveChangesAsync();

            var content = new PlayersNewMatchResponse();
            content.GameId = newMatch.GameId;
            content.MatchId = newMatch.Id;
            content.Players = new List<PlayersNewMatchResponse_players>() { };
            foreach (var newPlayer in newPlayers)
            {
                content.Players.Add(new PlayersNewMatchResponse_players
                {
                    PlayerId = newPlayer.Id,
                    LifeTotal = newPlayer.CurrentLife,
                });
            }

            return (content, $"New {gameDB.Name} match started with {newPlayers.Count} players");
        }

        public async Task<(PlayersIncreaseLifeResponse?, string)> IncreaseLife(PlayersIncreaseLifeRequest request)
        {
            var (requestIsValid, message) = await IsValid_IncreaseLifeRequest(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var isMaxLifeFixed = await IsLifeTotalFixed(request.PlayerId);

            if (isMaxLifeFixed == true)
            {
                await this._daoDbContext
                          .Players
                          .Where(a => a.Id == request.PlayerId)
                          .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLife, b =>
                                                    ((request.HealingAmount + b.CurrentLife) <= b.StartingLife) ?
                                                    (request.HealingAmount + b.CurrentLife) : (b.StartingLife)
                          ));

                message = $"Player healed {request.HealingAmount} life points.";

                return (null, $"Player healed {request.HealingAmount} life points.");
            }

            await this._daoDbContext
                     .Players
                     .Where(a => a.Id == request.PlayerId)
                     .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLife, b => b.CurrentLife + request.HealingAmount));

            return (null, $"Player gained {request.HealingAmount} life points");
        }
        private async Task<(bool, string)> IsValid_IncreaseLifeRequest(PlayersIncreaseLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .AnyAsync(a => a.Id == request.PlayerId && a.IsDeleted == false && a.Match.IsFinished == false);

            if (exists == false)
            {
                return (false, "Error: player not found");
            }

            if (request.HealingAmount <= 0)
            {
                return (false, "Error: invalid healing");
            }

            return (true, string.Empty);
        }
        public async Task<(PlayersSetLifeResponse?, string)> SetLife(PlayersSetLifeRequest request)
        {
            var (requestIsValid, message) = IsValid_SetLifeRequest(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var playerDB = await this._daoDbContext
                                     .Players
                                     .Include(a => a.Match)
                                     .FirstOrDefaultAsync(a => a.Id == request.PlayerId && a.IsDeleted == false && a.Match.IsFinished == false);

            if (playerDB == null)
            {
                return (null, "Error: player not found");
            }
           
            message = $"Player's life was successfully set to {request.NewCurrentLife} points.";

            var isMaxLifeFixed = await IsLifeTotalFixed(request.PlayerId);

            if (isMaxLifeFixed == true)
            {
                if (playerDB.CurrentLife == playerDB.StartingLife)
                {
                    return (null, $"Error: player's current life is already at the maximum allowed value for this game: {playerDB.StartingLife}");
                }

                if (request.NewCurrentLife > playerDB.StartingLife)
                {
                    return (null, $"Error: the maximum amount allowed for this game is {playerDB.StartingLife}");
                }
               
                playerDB.CurrentLife = request.NewCurrentLife;

                await this._daoDbContext.SaveChangesAsync();

                message = $"Player's life was successfully set to {request.NewCurrentLife} points.";

                return (null, message);
            }

            await this._daoDbContext
                      .Players
                      .Where(a => a.Id == request.PlayerId)
                      .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLife, request.NewCurrentLife));

            return (null, message);
        }
        private static (bool, string) IsValid_SetLifeRequest(PlayersSetLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.NewCurrentLife < 0)
            {
                return (false, "Error: a player's life total cannot be dropped bellow zero");
            }
               
            return (true, string.Empty);
        }
        private async Task<bool> IsLifeTotalFixed(int playerId)
        {
            return await this._daoDbContext.Games
                             .Where(a => a.Matches.Any(b => b.Players.Any(c => c.Id == playerId)))
                             .Select(a => a.FixedMaxLife)
                             .FirstOrDefaultAsync();
        }


        public async Task<(PlayersDecreaseLifeResponse?, string)> DecreaseLife(PlayersDecreaseLifeRequest request)
        {
            var (requestIsValid, message) = await IsValid_DecreaseLifeRequest(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var playersDB = new List<Player>();

            var playerIds = new List<int>();

            var matchPlayersCount = 0;

            var matchId = 0;

            if (request.MatchId.HasValue == true)
            {
                var matchDB = await this._daoDbContext
                                    .Matches
                                    .Include(a => a.Players)
                                    .FirstOrDefaultAsync(a => a.Id == request.MatchId);

                playersDB = matchDB.Players;

                matchPlayersCount = matchDB.PlayersCount;

                matchId = matchDB.Id;

                if (request.PlayerIds == null || request.PlayerIds.Count == 0)
                {
                    playerIds = playersDB.Select(a => a.Id).ToList();
                }
                else
                {
                    playerIds = request.PlayerIds;
                }
            }

            if (request.MatchId.HasValue == false)
            {


                playersDB = await this._daoDbContext
                                      .Players
                                      .Include(a => a.Match)
                                      .Where(a => request.PlayerIds.Contains(a.Id))
                                      .ToListAsync();

                var matchDB = playersDB.Select(a => a.Match).FirstOrDefault();

                matchPlayersCount = matchDB.PlayersCount;

                matchId = matchDB.Id;

                if (request.PlayerIds.Count != 0)
                {
                    playerIds = request.PlayerIds;
                }
            }

            message = $"All players suffered {request.DamageAmount} damage. ";

            if (playerIds.Count == 1)
            {
                message = $"Player (id = {playerIds[0]}) suffered {request.DamageAmount} damage.";
            }
            else
            {
                message = $"Players (ids = {string.Join(", ", playerIds)}) suffered {request.DamageAmount} damage.";
            }

            foreach (var playerId in playerIds)
            {
                var playerDB = playersDB.Where(a => a.Id == playerId).FirstOrDefault();
                playerDB.CurrentLife -= request.DamageAmount;
            }

            await this._daoDbContext.SaveChangesAsync();

            var defeatedPlayersCount = playersDB.Where(a => a.CurrentLife <= 0).Count();

            var isFinished = matchPlayersCount - defeatedPlayersCount <= 1;


            if (isFinished == true)
            {
                var (isFinishSucessful, gameOverMessage) = await MatchesService.FinishMatch(_daoDbContext, matchId: matchId);
                if (isFinishSucessful == false)
                {
                    return (null, gameOverMessage);
                }
                message += gameOverMessage;
            }

            return (null, message.Trim());
        }
        private async Task<(bool, string)> IsValid_DecreaseLifeRequest(PlayersDecreaseLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.MatchId.HasValue == false && request.PlayerIds == null || request.PlayerIds?.Count == 0)
            {
                return (false, "Error: neither a MatchId nor at least one PlayerId were provided");
            }

            if (request.MatchId.HasValue == true && request.MatchId <= 0)
            {
                return (false, "Error: invalid MatchId");
            }

            if (request.PlayerIds != null && request.PlayerIds.Count > 0 && request.PlayerIds.Any(a => a <= 0))
            {
                return (false, "Error: invalid PlayerId");
            }

            if (request.DamageAmount < 0)
            {
                return (false, "Error: invalid DamageAmount. It must be greater non negative value");
            }

            if (request.MatchId.HasValue == true)
            {
                var matchIdExists = await this._daoDbContext
                                              .Matches
                                              .AnyAsync(a => a.Id == request.MatchId && a.IsFinished == false);
                if (matchIdExists == false)
                {
                    return (false, "Error: match not found");
                }
            }

            if (request.PlayerIds != null && request.PlayerIds.Count != 0)
            {
                var playersMatchDB = await this._daoDbContext
                                             .Players
                                             .Where(a => request.PlayerIds.Contains(a.Id) && a.IsDeleted == false)
                                             .Select(a => a.Id)
                                             .ToListAsync();

                var invalidPlayers = request.PlayerIds.Except(playersMatchDB).ToList();

                if (invalidPlayers != null && invalidPlayers.Count > 0)
                {
                    string message = string.Empty;
                    message += invalidPlayers.Count == 1 ? $"Player (id = {invalidPlayers[0]}) not found" : $"Players (ids = {string.Join(", ", invalidPlayers)}) not found";

                    return (false, message);
                }
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersResetLifeResponse?, string)> ResetLife(PlayersResetLifeRequest request)
        {
            var (requestIsValid, message) = await IsValid_ResetLifeRequest(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }
           

            var matchDB = await this._daoDbContext
                                   .Matches
                                   .Include(a => a.Players)
                                   .FirstOrDefaultAsync(a => a.Id == request.MatchId && a.IsFinished == false);

            if (matchDB == null)
            {
                return (null, "Error: game match not found");
            }

            if (matchDB.Players == null || matchDB.Players.Count == 0)
            {
                return (null, "Error: no players joined this match");
            }

            await this._daoDbContext
                      .Players
                      .Where(a => a.MatchId == request.MatchId)
                      .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLife, b => b.StartingLife));

            return (null, "All Players life total reset to their starting life total");
        }
        private async Task<(bool, string)> IsValid_ResetLifeRequest(PlayersResetLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.MatchId.HasValue == false && request.PlayerIds == null || request.PlayerIds?.Count == 0)
            {
                return (false, "Error: neither a MatchId nor at least one PlayerId were provided");
            }

            if (request.MatchId.HasValue == true && request.MatchId <= 0)
            {
                return (false, "Error: invalid MatchId");
            }

            if (request.PlayerIds != null && request.PlayerIds.Count > 0 && request.PlayerIds.Any(a => a <= 0))
            {
                return (false, "Error: invalid PlayerId");
            }

            if (request.MatchId.HasValue == true)
            {
                var matchIdExists = await this._daoDbContext
                                              .Matches
                                              .AnyAsync(a => a.Id == request.MatchId && a.IsFinished == false);
                if (matchIdExists == false)
                {
                    return (false, "Error: match not found");
                }
            }

            if (request.PlayerIds != null && request.PlayerIds.Count != 0)
            {
                var playersMatchDB = await this._daoDbContext
                                             .Players
                                             .Where(a => request.PlayerIds.Contains(a.Id) && a.IsDeleted == false)
                                             .Select(a => a.Id)
                                             .ToListAsync();

                var invalidPlayers = request.PlayerIds.Except(playersMatchDB).ToList();

                if (invalidPlayers != null && invalidPlayers.Count > 0)
                {
                    string message = string.Empty;
                    message += invalidPlayers.Count == 1 ? $"Player (id = {invalidPlayers[0]}) not found" : $"Players (ids = {string.Join(", ", invalidPlayers)}) not found";

                    return (false, message);
                }
            }

            return (true, string.Empty);
        }
        public async Task<(PlayersShowMatchStatusResponse?, string)> ShowMatchStatus(PlayersShowMatchStatusRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            if (request.MatchId <= 0)
            {
                return (null, "Error: invalid MatchiId");
            }

            var matchDB = await this._daoDbContext
                                    .Matches
                                    .Include(a => a.Players)
                                    .FirstOrDefaultAsync(a => a.Id == request.MatchId);

            if (matchDB == null)
            {
                return (null, "Error: match not found");
            }

            var currentTimeMark = DateTime.UtcNow.ToLocalTime().Ticks;

            var elapsedTime = currentTimeMark - matchDB.StartingTime;

            if (matchDB.IsFinished == true)
            {
                elapsedTime = matchDB.Duration;
            }

            var content = new PlayersShowMatchStatusResponse();

            content.GameId = matchDB.GameId;

            content.MatchId = matchDB.Id;

            content.Players = new List<PlayersShowMatchStatusResponse_players>() { };
            foreach (var player in matchDB.Players)
            {
                content.Players.Add(new PlayersShowMatchStatusResponse_players
                {
                    PlayerId = player.Id,
                    CurrentLifeTotal = player.CurrentLife,
                });
            }

            content.ElapsedTime = elapsedTime;

            content.IsFinished = matchDB.IsFinished;

            var countAllPlayers = matchDB.Players.Count;

            var countDefeatedPlayers = matchDB.Players.Where(a => a.CurrentLife == 0).Count();

            if (countAllPlayers - countDefeatedPlayers <= 1)
            {
                await this._daoDbContext
                          .Matches
                          .Where(a => a.Id == request.MatchId)
                          .ExecuteUpdateAsync(a => a
                          .SetProperty(b => b.Duration, elapsedTime)
                          .SetProperty(b => b.IsFinished, true));

                content.IsFinished = true;
            }

            return (content, $"Match id {matchDB.Id} status showed successfully");
        }

        public async Task<(PlayersEndMatchResponse?, string)> EndMatch(PlayersEndMatchRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            if (request.MatchId <= 0)
            {
                return (null, "Error: invalid MatchId");
            }


            var matchDB = await this._daoDbContext
                                    .Matches
                                    .Include(a => a.Game)
                                    .FirstOrDefaultAsync(a => a.Id == request.MatchId);

            if (matchDB == null)
            {
                return (null, "Error: match not found");
            }

            if (matchDB.IsFinished == true)
            {
                return (null, "Error: this match has been already finished previously");
            }

            var message = $"Match ended successfully";


            var (isFinishSucessful, gameOverMessage) = await MatchesService.FinishMatch(_daoDbContext, matchId: request.MatchId);

            if (isFinishSucessful == false)
            {
                return (null, gameOverMessage);
            }
            message += gameOverMessage;

            return (null, message);
        }

        public async Task<(PlayersShowStatsResponse?, string)> ShowStats(PlayersShowStatsRequest request)
        {
            var matchesDB = await this._daoDbContext
                                      .Matches.Include(a => a.Players)
                                      .Where(a => a.IsFinished == true)
                                      .ToListAsync();

            if (matchesDB == null || matchesDB.Count == 0)
            {
                return (null, "Error: no was match found");
            }

            var countAllMachtes = matchesDB.Count;

            if (countAllMachtes == 0)
            {
                return (null, "Error: no players found for the registered matches");
            }
            var totalPlayers = matchesDB.Select(a => a.Players.Count).Sum();

            var averagePlayersPerMatch = (int)Math.Ceiling((double)(totalPlayers / countAllMachtes));

            var totalLength = matchesDB.Select(a => a.Duration).Sum();

            var totalLengthMin = totalLength / (10_000_000 * 60);

            var averageMatchDuration = (int)Math.Ceiling((double)(totalLengthMin / countAllMachtes));

            var mostPlayedGame = matchesDB.GroupBy(a => a.GameId).OrderByDescending(a => a.Count()).Select(a => new { GameId = a.Key, Count = a.Count() }).FirstOrDefault();

            var gamesDB = await this._daoDbContext
                                  .Games
                                  .Include(a => a.Matches)
                                  .ToListAsync();

            var dic = new Dictionary<int, long>();

            foreach (var game in gamesDB)
            {
                dic.Add(game.Id, game.Matches.Select(a => a.Duration).Sum());
            }

            var longestAvgMatchGame_id = dic.MaxBy(a => a.Value).Key;
            var longestAvgMatchGame_name = gamesDB.Where(a => a.Id == longestAvgMatchGame_id).Select(a => a.Name).FirstOrDefault();

            var content = new PlayersShowStatsResponse
            {
                CountMatches = countAllMachtes,
                MatchesAvgPlayerCount = averagePlayersPerMatch,
                MatchesAvgDuration = averageMatchDuration,
                MostPlayedGameId = mostPlayedGame.GameId,
                LongestAvgMatchGame_id = longestAvgMatchGame_id,
                LongestAvgMatchGame_name = longestAvgMatchGame_name
            };

            return (content, "Statistics showed successfully");
        }
    }
}
