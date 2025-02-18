using LifeCounterAPI.Models.Dtos.Request.Players;
using LifeCounterAPI.Models.Dtos.Response.Players;
using LifeCounterAPI.Models.Entities;
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

        public async Task<(List<PlayersStartGameResponse>?, string)> StartGame(PlayersStartGameRequest request)
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

            gameDB.Players ??= new List<Player>(); // Ensure the list is initialized
            gameDB.Players.AddRange(newPlayers);

            await _daoDbContext.SaveChangesAsync();


            var content = new List<PlayersStartGameResponse>();
            foreach (var newPlayer in gameDB.Players)
            {
                content.Add(new PlayersStartGameResponse
                {
                    PlayerId = newPlayer.Id,
                    LifeTotal = newPlayer.CurrentLifeTotal,
                });
            }


            return (content, $"{gameDB.Name} match started with {gameDB.Players.Count} players");
        }

        public async Task<(PlayersIncreaseLifeTotalResponse?, string)> IncreaseLifeTotal(PlayersIncreaseLifeTotalRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .AnyAsync(a => a.Id == request.PlayerId);

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
                                   .AnyAsync(a => a.Id == request.PlayerId);

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
            if(request == null)
            {
                return (null, "Error: no information provided");
            }

            if(request.LifeValue < 0)
            {
                return (null, "Error: a player's life total cannot be dropped bellow zero");
            }

            var exists = await this._daoDbContext
                                   .Players
                                   .AnyAsync(a => a.Id == request.PlayerId);

            if(exists == false)
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
            if(request == null)
            {
                return (null, "Error: no information provided");
            }

            if(request.GameId <= 0)
            {
                return (null, "Error: invalid GameId");
            }

            var gameDB = await this._daoDbContext
                                   .Games
                                   .Include(a => a.Players)
                                   .FirstOrDefaultAsync(a => a.Id == request.GameId);

            if(gameDB == null)
            {
                return (null, "Error: game not found");
            }

            if(gameDB.Players == null || gameDB.Players.Count == 0)
            {
                return (null, "Error: no players joined this game");
            }

            await this._daoDbContext
                      .Players
                      .Where(a => a.GameId == request.GameId)
                      .ExecuteUpdateAsync(a => a.SetProperty(b => b.CurrentLifeTotal, b => b.StartingLifeTotal));

            return (null, "All Players life total reset to their starting life total");
        }
    }
}
