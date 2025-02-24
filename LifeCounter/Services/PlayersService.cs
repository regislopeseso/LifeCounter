using LifeCounterAPI.Models.Dtos.Request.Players;
using LifeCounterAPI.Models.Dtos.Response.Players;
using LifeCounterAPI.Models.Entities;
using LifeCounterAPI.Utilities;
using Microsoft.EntityFrameworkCore;

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
            var (requestIsValid, message) = NewMatchValidation(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var gameDB = await this._daoDbContext
                                   .Games
                                   .FirstOrDefaultAsync(a => a.Id == request.GameId);

            if (gameDB == null)
            {
                return (null, "Error: invalid GameId");
            }

            if (gameDB.FixedMaxLife == true && request.PlayersLifeTotals != null && request.PlayersLifeTotals.Any(a => a > gameDB.StartingLife) == true)
            {
                return (null, $"Error, the maximum life total allowed for this game is {gameDB.FixedMaxLife}");
            }

            if (request.PlayersCount.HasValue == false)
            {
                if (request.PlayersLifeTotals == null || request.PlayersLifeTotals.Count == 0)
                {
                    var newPlayersLifeTotals = new List<int>() { };

                    while (newPlayersLifeTotals.Count < 2)
                    {
                        newPlayersLifeTotals.Add(gameDB.StartingLife);
                    }

                    request.PlayersLifeTotals = newPlayersLifeTotals;
                }

                if (request.PlayersLifeTotals != null && request.PlayersLifeTotals.Count < Constants.MinPlayerCount)
                {
                    while (request.PlayersLifeTotals.Count < 2)
                    {
                        request.PlayersLifeTotals.Add(gameDB.StartingLife);
                    }
                }
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
        private static (bool, string) NewMatchValidation(PlayersNewMatchRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.PlayersCount < 0)
            {
                return (false, $"Error: PlayersCount must be a positive number");
            }

            if (request.PlayersLifeTotals != null && request.PlayersLifeTotals.Any(a => a <= 0) == true)
            {
                return (false, $"Error: starting life must be at least 1. Invalid values: {string.Join(", ", request.PlayersLifeTotals.Where(a => a <= 0).ToList())}");
            }

            if (request.PlayersLifeTotals == null || request.PlayersLifeTotals.Count == 0)
            {
                if (request.PlayersCount.HasValue == true && request.PlayersCount < Constants.MinPlayerCount)
                {
                    return (false, $"Error: at least {Constants.MinPlayerCount} players are required to start the game");
                }
            }


            if (request.PlayersLifeTotals != null && request.PlayersCount != 0 && request.PlayersLifeTotals.Count > request.PlayersCount)
            {
                var message = $"Erro: the number of life total entries must be equal to or less than the player count, but not more. There ";

                message += request.PlayersCount == 1 ? $"is {request.PlayersCount} player " : $"are {request.PlayersCount} players ";

                message += $"and {request.PlayersLifeTotals.Count} life total entries. ";

                message += request.PlayersLifeTotals.Count == 3 ? $"Remove the exceeding life total entry" : $"Remove {request.PlayersLifeTotals.Count - request.PlayersCount} exceeding life total entries";

                return (false, message);
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersIncreaseLifeResponse?, string)> IncreaseLife(PlayersIncreaseLifeRequest request)
        {
            var (requestIsValid, message) = await IncreaseLifeValidation(request);

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
        private async Task<(bool, string)> IncreaseLifeValidation(PlayersIncreaseLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if(request.PlayerId <= 0)
            {
                return (false, "Error: PlayerId must be a positive number");
            }

            if (request.HealingAmount <= 0)
            {
                return (false, "Error: invalid healing amount. The healing amount amount must be a positive value.");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .AnyAsync(a => a.Id == request.PlayerId);

            if (exists == false)
            {
                return (false, "Error: player not found");
            }

            var isMatchFinished = await this._daoDbContext
                                            .Matches
                                            .AnyAsync(a => a.Players.Select(a => a.Id)
                                                                    .Contains(request.PlayerId)
                                                                    && a.IsFinished == true);

            if (isMatchFinished == true)
            {
                return (false, "Error: this player's match is already finished");
            }        

            return (true, string.Empty);
        }

        public async Task<(PlayersSetLifeResponse?, string)> SetLife(PlayersSetLifeRequest? request)
        {
            var (requestIsValid, message) = SetLifeValidation(request);

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
                var isMatchFinished = await this._daoDbContext
                                            .Matches
                                            .AnyAsync(a => a.Players.Select(a => a.Id)
                                                                    .Contains(request.PlayerId.Value)
                                                                    && a.IsFinished == true);
                if (isMatchFinished == true)
                {
                    return (null, "Error: this player's match is already finished");
                }

                return (null, "Error: player not found");
            }

            message = $"Player's life was successfully set to {request.NewCurrentLife} points.";

            var isMaxLifeFixed = await IsLifeTotalFixed(request.PlayerId.Value);

            if (isMaxLifeFixed == true)
            {
                if (request.NewCurrentLife > playerDB.StartingLife)
                {
                    return (null, $"Error: the maximum amount allowed for this game is {playerDB.StartingLife}");
                }

                playerDB.CurrentLife = request.NewCurrentLife.Value;

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
        private static (bool, string) SetLifeValidation(PlayersSetLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.PlayerId == null || request.PlayerId <= 0)
            {
                return (false, "Error: the playerId must be a positive value");
            }

            if(request.PlayerId != null && request.NewCurrentLife == null)
            {
                return (false, "Error: A value must be provided to set the players' current life.");
            }

            if (request.NewCurrentLife == null || request.NewCurrentLife < 0)
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
            var (requestIsValid, message) = await DecreaseLifeValidation(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var playersDB = new List<Player>();

            var playerIds = new List<int>();

            var matchPlayersCount = 0;

            var matchId = 0;

            var isAutoEndMatchOn = false;

            if (request.MatchId.HasValue == true)
            {
                var matchDB = await this._daoDbContext
                                    .Matches
                                    .Include(a => a.Game)
                                    .Include(a => a.Players)
                                    .FirstOrDefaultAsync(a => a.Id == request.MatchId);

                playersDB = matchDB.Players;

                isAutoEndMatchOn = matchDB.Game.AutoEndMatch;

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

                isAutoEndMatchOn = matchDB.Game.AutoEndMatch;

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

            if (isFinished == true && isAutoEndMatchOn == true)
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
        private async Task<(bool, string)> DecreaseLifeValidation(PlayersDecreaseLifeRequest request)
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
                    var isMatchFinished = await this._daoDbContext
                                                    .Matches
                                                    .AnyAsync(a => a.Id == request.MatchId && a.IsFinished == true);

                    if (isMatchFinished == true)
                    {
                        return (false, "Error: this match is already finished");
                    }

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
                    string message = "Error: ";

                    message += invalidPlayers.Count == 1 ? $"player (id = {invalidPlayers[0]}) not found" : $"Players (ids = {string.Join(", ", invalidPlayers)}) not found";

                    var isMatchFinished = await this._daoDbContext
                                             .Players
                                             .Where(a => request.PlayerIds.Contains(a.Id) && a.Match.IsFinished == true)
                                             .AnyAsync();

                    if (isMatchFinished == true)
                    {
                        message = "Error: ";
                        message += request.PlayerIds.Count == 1 ?
                            "this player's match is already finished" :
                            "the match these players were participating in has finished.";
                    }

                    return (false, message);
                }
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersResetLifeResponse?, string)> ResetLife(PlayersResetLifeRequest request)
        {
            var (requestIsValid, message) = await ResetLifeValidation(request);

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

                if (request.PlayerIds != null && request.PlayerIds.Count != 0)
                {
                    playerIds = request.PlayerIds;
                }
            }

            message = $"All players had their lives reset. ";

            if (playerIds.Count == 1)
            {
                message = $"Player (id = {playerIds[0]}) had his life reset.";
            }
            else
            {
                message = $"Players (ids = {string.Join(", ", playerIds)}) had their lives reset.";
            }

            foreach (var playerId in playerIds)
            {
                var playerDB = playersDB.Where(a => a.Id == playerId).FirstOrDefault();
                playerDB.CurrentLife = playerDB.StartingLife;
            }

            await this._daoDbContext.SaveChangesAsync();

            return (null, message.Trim());
        }
        private async Task<(bool, string)> ResetLifeValidation(PlayersResetLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.MatchId.HasValue == false && (request.PlayerIds == null || request.PlayerIds.Count == 0))
            {
                return (false, "Error: neither a MatchId nor at least one PlayerId were provided");
            }

            if (request.MatchId.HasValue == true && (request.PlayerIds != null && request.PlayerIds.Count > 0))
            {
                return (false, "Error: inform either a valid MatchId OR one or more valid PlayerId(s) but not both");
            }

            if (request.MatchId.HasValue == true && request.MatchId <= 0)
            {
                return (false, "Error: invalid MatchId. The value must be positive");
            }

            if (request.PlayerIds != null && request.PlayerIds.Count > 0 && request.PlayerIds.Any(a => a <= 0))
            {
                return (false, "Error: one or more negative values for PlayerId were informed. PlayerId must be a positive value");
            }

            if (request.MatchId.HasValue == true)
            {
                var matchExists = await this._daoDbContext
                                              .Matches
                                              .Where(a => a.Id == request.MatchId)                                             
                                              .FirstOrDefaultAsync();

                if (matchExists == null)
                {
                    return (false, "Error: match not found");
                }
                if (matchExists.IsFinished == true)
                {                    
                    return (false, "Error: this match is already finished");
                }
            }

            if (request.PlayerIds != null && request.PlayerIds.Count != 0)
            {
                var playersGroupedByMatchId = await this._daoDbContext
                                             .Players
                                             .Where(a => request.PlayerIds.Contains(a.Id) && a.IsDeleted == false)
                                             .GroupBy(a => a.MatchId)
                                             .Select(a => a.Select(a => a.Id).ToList())
                                             .ToListAsync();

                if(playersGroupedByMatchId.Count > 1)
                {
                    return (false, "");
                }

                var playersMatchDB = playersGroupedByMatchId.FirstOrDefault();

                var invalidPlayers = request.PlayerIds.Except(playersGroupedByMatchId[0]).ToList();

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
            var finishedMatches = 0;
            var unfinishedMatches = 0;
            var avgPlayersPerMatch = 0;
            var avgMatchDurationMinutes = 0;
            var mostPlayedGame = 0;
            var avgLongestGame = 0;

            finishedMatches = await this._daoDbContext
                                        .Matches
                                        .Where(a => a.IsFinished == true)
                                        .CountAsync();

            unfinishedMatches = await this._daoDbContext
                                          .Matches
                                          .Where(a => a.IsFinished == false)
                                          .CountAsync();

            if (finishedMatches == 0 && unfinishedMatches == 0)
            {
                finishedMatches = 0;
            }
            else
            {
                avgPlayersPerMatch = (await this._daoDbContext
                                               .Matches
                                               .Select(a => a.PlayersCount)
                                               .SumAsync())
                                               / (finishedMatches + unfinishedMatches);
            }

            if (finishedMatches == 0)
            {
                avgMatchDurationMinutes = 0;
            }
            else
            {
                avgMatchDurationMinutes = (await this._daoDbContext
                                              .Matches
                                              .Where(a => a.IsFinished == true)
                                              .Select(a => (int)a.Duration)
                                              .SumAsync())
                                              / finishedMatches;
            }

            mostPlayedGame = await this._daoDbContext
                                       .Matches
                                       .Where(a => a.IsFinished == true)
                                       .GroupBy(a => a.GameId)
                                       .Select(a => a.Count())
                                       .MaxAsync();

            avgLongestGame = await this._daoDbContext
                                       .Matches
                                       .Where(a => a.IsFinished == true)
                                       .GroupBy(a => a.GameId)
                                       .Select(a => new { GameId = a.Key, AvgDuration = a.Select(b => b.Duration).Average() })
                                       .OrderByDescending(a => a.AvgDuration)
                                       .Select(a => a.GameId)
                                       .FirstOrDefaultAsync();

            var content = new PlayersShowStatsResponse
            {
                FinishedMatches = finishedMatches,
                UnfinishedMatches = unfinishedMatches,
                AvgPlayersPerMatch = avgPlayersPerMatch,
                AvgMatchDurationMinutes = avgMatchDurationMinutes,
                MostPlayedGame = mostPlayedGame,
                AvgLongestGame = avgLongestGame
            };

            return (content, "Statistics showed successfully");
        }
    }
}
