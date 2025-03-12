using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

            if (gameDB.FixedMaxLife == true && request.PlayersStartingLives != null && request.PlayersStartingLives.Any(a => a > gameDB.PlayersStartingLife) == true)
            {
                return (null, $"Error, the maximum life total allowed for this game is {gameDB.PlayersStartingLife}");
            }

            var newPlayers = new List<Player>();

            var correctedStartingLives = new List<int>() { };

            // Next, a sequence of "ifs" to transform and/or correct the parameters
            // PlayersCount and PlayersStartingLives
            if (request.PlayersCount.HasValue == false && request.PlayersStartingLives == null)
            {
                correctedStartingLives.AddRange(Enumerable.Repeat(gameDB.PlayersStartingLife, Constants.MinPlayerCount));

                request.PlayersStartingLives = correctedStartingLives;
            }

            if (request.PlayersCount.HasValue == false && request.PlayersStartingLives != null && request.PlayersStartingLives.Count == 1)
            {
                request.PlayersStartingLives.Add(gameDB.PlayersStartingLife);
            }

            if (request.PlayersCount.HasValue == true && request.PlayersCount != 0 && request.PlayersStartingLives == null)
            {
                if (request.PlayersCount == 1)
                {
                    correctedStartingLives.AddRange(Enumerable.Repeat(gameDB.PlayersStartingLife, Constants.MinPlayerCount));

                    request.PlayersStartingLives = correctedStartingLives;
                }
                else
                {
                    correctedStartingLives.AddRange(Enumerable.Repeat(gameDB.PlayersStartingLife, request.PlayersCount.Value));

                    request.PlayersStartingLives = correctedStartingLives;
                }
            }

            if (request.PlayersCount.HasValue == true &&
                request.PlayersCount != 0 &&
                request.PlayersStartingLives != null &&
                request.PlayersCount > request.PlayersStartingLives.Count)
            {
                var lackingStartingLivesEntries = request.PlayersCount - request.PlayersStartingLives.Count;

                request.PlayersStartingLives.AddRange(Enumerable.Repeat(gameDB.PlayersStartingLife, lackingStartingLivesEntries.Value));
            }
            // after this point it is guaranteed the existence of at least 2 PlayersStartingLives 

            int? playerMaxLife = gameDB.FixedMaxLife == true ? gameDB.PlayersStartingLife : null;

            foreach (var startingLifeValue in request.PlayersStartingLives!)
            {
                newPlayers.Add
                (
                    new Player
                    {
                        StartingLife = startingLifeValue,
                        CurrentLife = startingLifeValue,
                        MaxLife = playerMaxLife,
                        FixedMaxLife = gameDB.FixedMaxLife
                    }
                );
            }

            var startMark = DateTime.UtcNow.ToLocalTime().Ticks;

            var newMatch = new Match
            {
                GameId = request.GameId,
                Players = newPlayers,
                PlayersCount = newPlayers.Count,
                StartingTime = startMark,
                AutoEnd = gameDB.AutoEndMatch,
            };

            gameDB.Matches ??= [];

            gameDB.Matches.Add(newMatch);

            await this._daoDbContext.SaveChangesAsync();

            var content = new PlayersNewMatchResponse
            {
                GameId = newMatch.GameId,

                MatchId = newMatch.Id,

                Players = []
            };

            foreach (var newPlayer in newPlayers)
            {
                content.Players.Add(new PlayersNewMatchResponse_player
                {
                    PlayerId = newPlayer.Id,
                    StartingLife = newPlayer.CurrentLife,
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

            if (request.PlayersCount.HasValue == true && request.PlayersCount < 2)
            {
                return (false, $"Error: PlayersCount must at least 2");
            }

            if (request.PlayersStartingLives != null && request.PlayersStartingLives.Any(a => a <= 0) == true)
            {
                return (false, $"Error: starting life must be at least 1. Invalid values: {string.Join(", ", request.PlayersStartingLives.Where(a => a <= 0).ToList())}");
            }

            if (request.PlayersStartingLives == null || request.PlayersStartingLives.Count == 0)
            {
                if (request.PlayersCount.HasValue == true && request.PlayersCount < Constants.MinPlayerCount)
                {
                    return (false, $"Error: at least {Constants.MinPlayerCount} players are required to start the game");
                }
            }

            if (request.PlayersStartingLives != null && request.PlayersCount != 0 && request.PlayersStartingLives.Count > request.PlayersCount)
            {
                var message = $"Error: the number of starting life entries must be equal to or less than the player count, but not more. There ";

                message += request.PlayersCount == 1 ? $"is {request.PlayersCount} player " : $"are {request.PlayersCount} players ";

                message += $"and {request.PlayersStartingLives.Count} starting life entries. ";

                message += request.PlayersStartingLives.Count == 3 ? $"Remove the exceeding life total entry" : $"Remove {request.PlayersStartingLives.Count - request.PlayersCount} exceeding starting life entries";

                return (false, message);
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersIncreaseLifeResponse?, string)> IncreaseLife(PlayersIncreaseLifeRequest request)
        {
            var (requestIsValid, message) = IncreaseLifeValidation(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var matchDB = await this._daoDbContext
                .Matches
                .Include(a => a.Players)
                .FirstOrDefaultAsync(a => a.Players
                    .Select(b => b.Id)
                    .Contains(request.PlayerId));

            if (matchDB == null)
            {
                return (null, "Error: no match found connected to the requested player");
            }

            if (matchDB.IsFinished == true)
            {
                return (null, "Error: this player's match has already ended");
            }

            var playerDB = matchDB.Players.FirstOrDefault(a => a.Id == request.PlayerId);

            if (playerDB == null)
            {
                return (null, "Error: player not found");
            }

            if (playerDB.IsDeleted == true)
            {
                return (null, "Error: this player is deleted");
            }

            if (playerDB.FixedMaxLife == true && playerDB.CurrentLife == playerDB.MaxLife)
            {
                return (null, $"Error: this player's life total is already at the maximum value ({playerDB.MaxLife}) allowed for this game");
            }

            int? increaseAmount = 0;

            if ((request.IncreaseAmount + playerDB.CurrentLife) <= playerDB.MaxLife)
            {
                increaseAmount = request.IncreaseAmount;

                message = $"Player had his life increased by {increaseAmount} ";

                message += increaseAmount == 1 ? "point. " : "points. ";

                message += $"His current life has now {playerDB.CurrentLife + increaseAmount} points.";
            }
            else if (playerDB.FixedMaxLife == true && (request.IncreaseAmount + playerDB.CurrentLife) > playerDB.MaxLife)
            {
                increaseAmount = playerDB.MaxLife - playerDB.CurrentLife;

                message = $"Player had his life increased only by {increaseAmount} points, making it reach the maximum value ({playerDB.MaxLife}) allowed for this game.";
            }
            else
            {
                increaseAmount = request.IncreaseAmount;

                message = $"Player had his life increased by {increaseAmount} ";

                message += increaseAmount == 1 ? "point. " : "points. ";

                message += $"His current life has now {playerDB.CurrentLife + increaseAmount} points.";
            }

            playerDB.CurrentLife += increaseAmount!.Value;

            await this._daoDbContext.SaveChangesAsync();

            var players = new List<PlayersIncreaseLifeResponse_player>() { };

            foreach (var player in matchDB!.Players)
            {
                players.Add(new PlayersIncreaseLifeResponse_player
                {
                    PlayerId = player.Id,
                    CurrentLife = player.CurrentLife
                });
            }

            var content = new PlayersIncreaseLifeResponse()
            {
                Players = players
            };

            return (content, message);
        }
        private static (bool, string) IncreaseLifeValidation(PlayersIncreaseLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.PlayerId <= 0)
            {
                return (false, "Error: PlayerId must be a positive number");
            }

            if (request.IncreaseAmount <= 0)
            {
                return (false, "Error: invalid value. It must be a positive value.");
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersSetLifeResponse?, string)> SetLife(PlayersSetLifeRequest? request)
        {
            var (requestIsValid, message) = SetLifeValidation(request!);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var matchDB = await this._daoDbContext
              .Matches
              .Include(a => a.Players)
              .FirstOrDefaultAsync(a => a.Players.Select(b => b.Id).Contains(request!.PlayerId!.Value));

            if (matchDB == null)
            {
                return (null, "Error: no match found connected to the requested player");
            }

            if (matchDB.IsFinished == true)
            {
                return (null, "Error: this player's match has already ended");
            }

            var playerDB = matchDB.Players.FirstOrDefault(a => a.Id == request!.PlayerId);

            if (playerDB == null)
            {
                return (null, "Error: player not found");
            }

            if (playerDB.IsDeleted == true)
            {
                return (null, "Error: this player is deleted");
            }

            if (playerDB.FixedMaxLife == true && request!.NewCurrentLife >= playerDB.MaxLife)
            {
                return (null, $"Error: this player's life total is already at the maximum value ({playerDB.MaxLife}) allowed for this game.");
            }

            message = $"Player's life was successfully set to {request!.NewCurrentLife} points.";

            playerDB.CurrentLife = request.NewCurrentLife!.Value;

            var players = new List<PlayersSetLifeResponse_player>() { };

            foreach (var player in matchDB.Players)
            {
                players.Add(new PlayersSetLifeResponse_player
                {
                    PlayerId = player.Id,
                    CurrentLife = player.CurrentLife
                });
            }

            var content = new PlayersSetLifeResponse
            {
                Players = players
            };

            await this._daoDbContext.SaveChangesAsync();

            return (content, message);
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

            if (request.PlayerId != null && request.NewCurrentLife == null)
            {
                return (false, "Error: A value must be provided to set the players' current life.");
            }

            if (request.NewCurrentLife == null || request.NewCurrentLife < 0)
            {
                return (false, "Error: a player's life total cannot be dropped bellow zero");
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersDecreaseLifeResponse?, string)> DecreaseLife(PlayersDecreaseLifeRequest? request)
        {
            var (requestIsValid, message) = DecreaseLifeValidation(request!);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            Match? matchDB;

            (matchDB, message) = await GetMatch(request!.MatchId, request.PlayerIds);

            if (matchDB == null || matchDB.Players == null || matchDB.Players.Count == 0)
            {
                return (null, message);
            }

            var playerIds = new List<int>();

            var playerIdsDB = matchDB.Players
                .Select(a => a.Id)
                .ToList();

            if (request.PlayerIds != null && request.PlayerIds.Count != 0)
            {
                (requestIsValid, message) = ManyPlayersOneMatchValidation(playerIdsDB, request.PlayerIds);

                if (requestIsValid == false)
                {
                    return (null, message);
                }

                playerIds = request.PlayerIds;
            }
            else
            {
                playerIds = playerIdsDB;
            }

            if (playerIds == null || playerIds.Count == 0)
            {
                return (null, "Error: no player found");
            }

            var isAutoEndMatchOn = matchDB.Game!.AutoEndMatch;

            message = $"All players suffered {request.DecreaseAmount} damage. ";

            if (playerIds.Count == 1)
            {
                message = $"Player (id = {playerIds[0]}) suffered {request.DecreaseAmount} damage.";
            }
            else
            {
                message = $"Players (ids = {string.Join(", ", playerIds.ToList())}) suffered {request.DecreaseAmount} damage.";
            }

            foreach (var playerId in playerIds)
            {
                var playerDB = matchDB.Players
                    .Where(a => a.Id == playerId)
                    .FirstOrDefault();

                if (playerDB == null)
                {
                    continue;
                }

                playerDB.CurrentLife -= request.DecreaseAmount;
            }

            await this._daoDbContext.SaveChangesAsync();

            var defeatedPlayersCount = matchDB.Players
                .Where(a => a.CurrentLife <= 0)
                .Count();

            var isFinished = matchDB.PlayersCount - defeatedPlayersCount <= 1;

            if (isFinished == true && isAutoEndMatchOn == true)
            {
                var (isFinishSucessful, reportMessage) = await MatchesService.FinishMatch(_daoDbContext, matchId: matchDB.Id);

                if (isFinishSucessful == false)
                {
                    return (null, reportMessage);
                }

                message += reportMessage;
            }

            var players = new List<PlayersDecreaseLifeResponse_player>() { };

            foreach (var player in matchDB.Players)
            {
                players.Add(new PlayersDecreaseLifeResponse_player
                {
                    PlayerId = player.Id,
                    CurrentLife = player.CurrentLife
                });
            }

            var content = new PlayersDecreaseLifeResponse
            {
                Players = players
            };

            return (content, message.Trim());
        }
        private static (bool, string) DecreaseLifeValidation(PlayersDecreaseLifeRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.MatchId.HasValue == true && request.PlayerIds != null && request.PlayerIds.Count != 0)
            {
                return (false, "Error: inform either a valid MatchId OR one or more valid PlayerId(s) but not both");
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

            if (request.DecreaseAmount < 0)
            {
                return (false, "Error: invalid value. It must be zero or a positive value");
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersResetLifeResponse?, string)> ResetLife(PlayersResetLifeRequest request)
        {
            var (requestIsValid, message) = ResetLifeValidation(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            Match? matchDB;

            (matchDB, message) = await GetMatch(request.MatchId, request.PlayerIds);

            if (matchDB == null || matchDB.Players == null || matchDB.Players.Count == 0)
            {
                return (null, message);
            }

            var playerIds = new List<int>();

            var playerIdsDB = matchDB.Players.Select(a => a.Id).ToList();

            if (request.PlayerIds != null && request.PlayerIds.Count != 0)
            {
                (requestIsValid, message) = ManyPlayersOneMatchValidation(playerIdsDB, request.PlayerIds);

                if (requestIsValid == false)
                {
                    return (null, message);
                }

                playerIds = request.PlayerIds;
            }
            else
            {
                playerIds = playerIdsDB;
            }

            if (playerIds == null || playerIds.Count == 0)
            {
                return (null, "Error: no player found");
            }

            message = $"All players had their lives reset. ";

            message = playerIds.Count == 1 ?
                        $"Player (id = {playerIds[0]}) had his life reset." :
                        $"Players (ids = {string.Join(", ", playerIds)}) had their lives reset.";

            foreach (var playerId in playerIds)
            {
                var playerDB = matchDB.Players.Where(a => a.Id == playerId).FirstOrDefault();
                if (playerDB == null)
                {
                    continue;
                }
                playerDB.CurrentLife = playerDB.StartingLife;
            }

            await this._daoDbContext.SaveChangesAsync();



            var players = new List<PlayersResetLifeResponse_player>() { };

            foreach (var player in matchDB.Players)
            {
                players.Add
                    (
                        new PlayersResetLifeResponse_player
                        {
                            PlayerId = player.Id,
                            CurrentLife = player.CurrentLife
                        }
                    );
            }

            var content = new PlayersResetLifeResponse
            {
                Players = players
            };

            return (content, message.Trim());
        }
        private static (bool, string) ResetLifeValidation(PlayersResetLifeRequest request)
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

            return (true, string.Empty);
        }

        private async Task<(Match?, string)> GetMatch(int? matchId, List<int>? playerIds)
        {
            if (matchId.HasValue == true)
            {
                var matchDB = await this._daoDbContext
                                  .Matches
                                  .Include(a => a.Players)
                                  .Include(a => a.Game)
                                  .FirstOrDefaultAsync(a => a.Id == matchId);

                if (matchDB == null)
                {
                    return (null, "Error: match not found");
                }

                if (matchDB.IsFinished == true)
                {
                    return (null, $"Error: this match (id = {matchDB.Id}) is already finished");
                }

                return (matchDB, String.Empty);
            }
            else
            {
                if (playerIds != null && playerIds.Count > 0)
                {
                    var matchDB = await this._daoDbContext
                                            .Matches
                                            .Include(a => a.Players)
                                            .Include(a => a.Game)
                                            .FirstOrDefaultAsync(a => a.Players.Where(b => b.Id == playerIds[0]).Any());

                    if (matchDB == null)
                    {
                        return (null, "Error: match not found");
                    }

                    if (matchDB.IsFinished == true)
                    {
                        return (null, $"Error: the match (id = {matchDB.Id} linked to the PlayerId = {playerIds[0]}) is already finished");
                    }

                    return (matchDB, String.Empty);
                }
                else
                {
                    return (null, "Error: neither a MatchId nor one or more PlayerId(s) were provided");
                }
            }
        }

        private static (bool, string) ManyPlayersOneMatchValidation(List<int> matchDB_playerIds, List<int> request_playerIds)
        {
            var invalidPlayerIds = request_playerIds.Except(matchDB_playerIds).ToList();

            if (invalidPlayerIds.Count > 0)
            {
                var message = $"Error: ";
                message += invalidPlayerIds.Count == 1 ? $"player (id = {invalidPlayerIds[0]})" : $"Players (ids = {string.Join(", ", invalidPlayerIds)})";
                message += " not found in this match";
                return (false, message);
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersShowMatchStatusResponse?, string)> ShowMatchStatus(PlayersShowMatchStatusRequest request)
        {
            var (requestIsValid, message) = ShowMatchStatusValidation(request);

            if (requestIsValid == false)
            {
                return (null, message);
            }

            var matchDB = await this._daoDbContext
                .Matches
                .Include(a => a.Players)
                .FirstOrDefaultAsync(a => a.Id == request.MatchId);

            if (matchDB == null)
            {
                return (null, "Error: match not found");
            }



            var elapsedTime = matchDB.Duration_minutes;

            if (matchDB.IsFinished == false)
            {
                var currentTimeMark = DateTime.UtcNow.ToLocalTime().Ticks;

                elapsedTime = (int)Math.Round
                    (
                        ((decimal)((currentTimeMark - matchDB.StartingTime) / (60_000_000)))
                        , 0
                    );
            }

            var content = new PlayersShowMatchStatusResponse
            {
                GameId = matchDB.GameId,

                MatchId = matchDB.Id,

                Players = []
            };

            foreach (var player in matchDB.Players)
            {
                content.Players.Add(new PlayersShowMatchStatusResponse_players
                {
                    PlayerId = player.Id,
                    CurrentLife = player.CurrentLife,
                });
            }

            content.ElapsedTime_minutes = elapsedTime;

            content.IsFinished = matchDB.IsFinished;

            message = $"The Match (id = {matchDB.Id}) status showed successfully";

            if (matchDB.IsFinished == true)
            {
                message += ". This match is already finished";
                return (content, message);
            }

            if (matchDB.AutoEnd == true && elapsedTime >= 60 * 24 * 5 && matchDB.IsFinished == false)
            {
                var (isFinishSucessful, reportMessage) = await MatchesService.FinishMatch(_daoDbContext, matchId: matchDB.Id);
                if (isFinishSucessful == false)
                {
                    return (null, reportMessage);
                }

                reportMessage = ". This match's duration has reached 1 week and is therefore being automatically ended. ";
                reportMessage += "All players belonging to this match have been also deleted.";
                content.IsFinished = true;

                message += reportMessage;
            }

            var countAllPlayers = matchDB.Players.Count;

            var countDefeatedPlayers = matchDB.Players
                .Where(a => a.CurrentLife <= 0 || a.IsDeleted == true)
                .Count();

            var playersLeftAlive = countAllPlayers - countDefeatedPlayers;

            if (matchDB.AutoEnd == true && playersLeftAlive <= 1 && matchDB.IsFinished == false)
            {
                var (isFinishSucessful, reportMessage) = await MatchesService.FinishMatch(_daoDbContext, matchId: matchDB.Id);

                if (isFinishSucessful == false)
                {
                    return (null, reportMessage);
                }

                content.IsFinished = true;

                message += playersLeftAlive == 1 ? ". Only one player is alive" : ". No player is left alive.";

                message += reportMessage;
            }

            return (content, message);
        }

        public static (bool, string) ShowMatchStatusValidation(PlayersShowMatchStatusRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.MatchId <= 0)
            {
                return (false, "Error: invalid MatchiId. The value must be positive");
            }

            return (true, string.Empty);
        }

        public async Task<(PlayersEndMatchResponse?, string)> EndMatch(PlayersEndMatchRequest request)
        {
            var (requestIsValid, message) = EndMatchValidation(request);

            if (requestIsValid == false)
            {
                return (null, message);
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
                return (null, "Error: this match was already finished.");
            }

            var (isFinishSucessful, reportMessage) = await MatchesService.FinishMatch(_daoDbContext, matchId: request.MatchId);

            if (isFinishSucessful == false)
            {
                return (null, reportMessage);
            }

            return (null, reportMessage);
        }

        private static (bool, string) EndMatchValidation(PlayersEndMatchRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (request.MatchId <= 0)
            {
                return (false, "Error: invalid MatchId. It must be a positive value");
            }

            return (true, string.Empty);
        }
        public async Task<(PlayersShowStatsResponse?, string)> ShowStats(PlayersShowStatsRequest request)
        {
            var finishedMatches = 0;
            var unfinishedMatches = 0;
            var avgPlayersPerMatch = 0;
            var avgMatchDuration = 0;
            var mostPlayedGame_id = 0;
            var mostPlayedGame_name = string.Empty;
            var avgLongestGame_id = 0;
            var avgLongestGame_name = string.Empty;

            TimeSpan timeSpan = TimeSpan.FromMinutes(0);

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
                avgMatchDuration = 0;
            }
            else
            {
                int avgMatchDuration_minutes = (int)Math.Round
                    (
                        (
                            (await this._daoDbContext
                            .Matches
                            .Where(a => a.IsFinished == true)
                            .Select(a => a.Duration_minutes)
                            .AverageAsync())
                        )
                        , 0
                    );

                avgMatchDuration = avgMatchDuration_minutes;
            }

            //mostPlayedGame_id = await this._daoDbContext
            //    .Matches
            //    .Where(a => a.IsFinished == true)
            //    .GroupBy(a => a.GameId)
            //    .Select(a => a.Count())
            //    .MaxAsync();

            mostPlayedGame_id = await this._daoDbContext
                .Matches
                .Where(a => a.IsFinished == true)
                .GroupBy(a => a.GameId)
                .OrderByDescending(a => a.Count())
                .ThenByDescending(a => a.Sum(b => b.Duration_minutes))
                .Select(a => a.Key)
                .FirstOrDefaultAsync();

            mostPlayedGame_name = await this._daoDbContext
                .Games
                .Where(a => a.Id == mostPlayedGame_id)
                .Select(a => a.Name)
                .FirstOrDefaultAsync();

            avgLongestGame_id = await this._daoDbContext
                .Matches
                .Where(a => a.IsFinished == true)
                .GroupBy(a => a.GameId)
                .Select(a => new { GameId = a.Key, AvgDuration = a.Select(b => b.Duration_minutes).Average() })
                .OrderByDescending(a => a.AvgDuration)
                .Select(a => a.GameId)
                .FirstOrDefaultAsync();

            avgLongestGame_name = await this._daoDbContext
                .Games
                .Where(a => a.Id == avgLongestGame_id)
                .Select(a => a.Name)
                .FirstOrDefaultAsync();

            var content = new PlayersShowStatsResponse
            {
                FinishedMatches = finishedMatches,
                UnfinishedMatches = unfinishedMatches,
                AvgPlayersPerMatch = avgPlayersPerMatch,
                AvgMatchDuration = avgMatchDuration,
                MostPlayedGame_id = mostPlayedGame_id,
                MostPlayedGame_name = mostPlayedGame_name!,
                AvgLongestGame_id = avgLongestGame_id,
                AvgLongestGame_name = avgLongestGame_name!
            };

            return (content, "Statistics showed successfully");
        }
    }
}