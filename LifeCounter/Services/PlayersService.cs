using LifeCounterAPI.Models.Dtos.Request.Players;
using LifeCounterAPI.Models.Dtos.Response.Players;
using LifeCounterAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
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

            if(request.PlayersLifeTotals != null && request.PlayersLifeTotals.Count > request.PlayersCount)
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
                            StartingLifeTotal = gameDB.LifeTotal,
                            CurrentLifeTotal = gameDB.LifeTotal
                        });
                    }
                    else
                    {
                        newPlayers.Add(new Player
                        {
                            StartingLifeTotal = lifeTotal,
                            CurrentLifeTotal = lifeTotal
                        });
                    }
                }
                for (int i = 0; i < request.PlayersCount - request.PlayersLifeTotals.Count; i++)
                {
                    newPlayers.Add(new Player
                    {
                        StartingLifeTotal = gameDB.LifeTotal,
                        CurrentLifeTotal = gameDB.LifeTotal
                    });
                }

            }
            else
            {
                for (int i = 0; i < request.PlayersCount; i++)
                {
                    newPlayers.Add(new Player
                    {
                        StartingLifeTotal = gameDB.LifeTotal,
                        CurrentLifeTotal = gameDB.LifeTotal
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
                    LifeTotal = newPlayer.CurrentLifeTotal,
                });
            }

            return (content, $"New {gameDB.Name} match started with {newPlayers.Count} players");
        }

        public async Task<(PlayersIncreaseLifeTotalResponse?, string)> IncreaseLifeTotal(PlayersIncreaseLifeTotalRequest request)
        {
            var (requestIsValid, message) = await IncreaseLifeTotalRequestIsValid(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            await this._daoDbContext
                      .Players
                      .Where(a => a.Id == request.PlayerId)
                      .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLifeTotal, b => b.CurrentLifeTotal + request.HealingAmount));

            return (null, $"Player healed {request.HealingAmount}");
        }
        private async Task<(bool, string)> IncreaseLifeTotalRequestIsValid(PlayersIncreaseLifeTotalRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .Include(a => a.Match)
                                   .AnyAsync(a => a.Id == request.PlayerId && a.Match.IsFinished == false);

            if (exists == false)
            {
                return (false, "Error: invalid PlayerId");
            }

            if (request.HealingAmount <= 0)
            {
                return (false, "Error: invalid healing");
            }

            var isMaxLifeFixed = await this._daoDbContext
                .Games
                .Where(a => a.IsDeleted == false && a.Matches.Any(b => b.IsFinished == false &&
                                                    b.Players.Any(c => 
                                                    c.IsDeleted == false && c.Id == request.PlayerId)))
                .Select(a => a.FixedMaxLife == true)
                .FirstOrDefaultAsync();

            return (true, string.Empty);
        }

        public async Task<(PlayersDecreaseLifeTotalResponse?, string)> DecreaseLifeTotal(PlayersDecreaseLifeTotalRequest request)
        {
            var (requestIsValid, message) = await DecreaseLifeTotalRequestIsValid(request);

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
                playerDB.CurrentLifeTotal -= request.DamageAmount;
            }

            await this._daoDbContext.SaveChangesAsync();

            var defeatedPlayersCount = playersDB.Where(a => a.CurrentLifeTotal <= 0).Count();

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
        private async Task<(bool, string)> DecreaseLifeTotalRequestIsValid(PlayersDecreaseLifeTotalRequest request)
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

            if(request.DamageAmount < 0)
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

        public async Task<(PlayersSetLifeTotalResponse?, string)> SetLifeTotal(PlayersSetLifeTotalRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            if (request.LifeValue < 0)
            {
                return (null, "Error: a player's life total cannot be dropped bellow zero");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .Include(a => a.Match)
                                   .AnyAsync(a => a.Id == request.PlayerId && a.Match.IsFinished == false);

            if (exists == false)
            {
                return (null, "Error: invalid PlayerId");
            }

            await this._daoDbContext
                .Players
                .Where(a => a.Id == request.PlayerId)
                .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLifeTotal, request.LifeValue));

            return (null, $"Player's life successfully set to {request.LifeValue} points");
        }

        public async Task<(PlayersResetLifeTotalResponse?, string)> ResetLifeTotal(PlayersResetLifeTotalRequest request)
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
                      .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLifeTotal, b => b.StartingLifeTotal));

            return (null, "All Players life total reset to their starting life total");
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
                    CurrentLifeTotal = player.CurrentLifeTotal,
                });
            }

            content.ElapsedTime = elapsedTime;

            content.IsFinished = matchDB.IsFinished;

            var countAllPlayers = matchDB.Players.Count;

            var countDefeatedPlayers = matchDB.Players.Where(a => a.CurrentLifeTotal == 0).Count();

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
