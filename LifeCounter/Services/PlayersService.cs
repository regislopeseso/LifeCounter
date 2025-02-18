using LifeCounterAPI.Models.Dtos.Request.Players;
using LifeCounterAPI.Models.Dtos.Response.Players;
using LifeCounterAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
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

            var newMatch = new Match
            {
                GameId = request.GameId,
                Players = newPlayers,
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
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .Include(a => a.Match)
                                   .AnyAsync(a => a.Id == request.PlayerId && a.Match.IsFinished == false);

            if (exists == false)
            {
                return (null, "Error: invalid PlayerId");
            }

            if (request.HealingAmount <= 0)
            {
                return (null, "Error: invalid healing");
            }

            await this._daoDbContext
                      .Players
                      .Where(a => a.Id == request.PlayerId)
                      .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLifeTotal, b => b.CurrentLifeTotal + request.HealingAmount));

            return (null, $"Player healed {request.HealingAmount}");
        }

        public async Task<(PlayersDecreaseLifeTotalResponse?, string)> DecreaseLifeTotal(PlayersDecreaseLifeTotalRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .Include(a => a.Match)
                                   .AnyAsync(a => a.Id == request.PlayerId && a.Match.IsFinished == false);

            if (exists == false)
            {
                return (null, "Error: invalid PlayerId");
            }

            if (request.DamageAmount <= 0)
            {
                return (null, "Error: invalid healing");
            }

            await this._daoDbContext
                      .Players
                      .Where(a => a.Id == request.PlayerId)
                      .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLifeTotal, b => b.CurrentLifeTotal - request.DamageAmount));

            return (null, $"Player suffered {request.DamageAmount} damage");
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
            
            var elapsedTime = DateTime.UtcNow.ToLocalTime() - matchDB.StartingTime;

            if(matchDB.IsFinished == true)
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

            if(request.MatchId <= 0)
            {
                return (null, "Error: invalid MatchId");
            }


            var matchDB = await this._daoDbContext
                                    .Matches
                                    .Include(a => a.Game)
                                    .FirstOrDefaultAsync(a => a.Id == request.MatchId);

            if(matchDB == null)
            {
                return (null, "Error: match not found");
            }

            if(matchDB.IsFinished == true)
            {
                return (null, "Error: this match has been already finished previously");
            }

            var timeNow = DateTime.Now;

            var duration = timeNow - matchDB.StartingTime;

            matchDB.Duration = duration;
            matchDB.EndingTime = timeNow;
            matchDB.IsFinished = true;

            await this._daoDbContext.SaveChangesAsync();

            //Caso não for desejável o envio de alguma informação mas somente "null, alterar PlayersEndMatchResponse para um objeto vazio e remove acima a inclusão de Game: ".Include(a => a.Game)"
            var content = new PlayersEndMatchResponse
            {
                GameId = matchDB.GameId,
                GameName = matchDB.Game.Name,
                MatchId = matchDB.Id,
                MatchBegin = matchDB.StartingTime,
                MatchEnd = timeNow,
                MatchDuration = duration
            };

            return (content, "Match ended successfully");
        }



    }
}
