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
                        newPlayers.Add(new Player { LifeTotal = gameDB.LifeTotal });
                    }
                    else
                    {
                        newPlayers.Add(new Player { LifeTotal = lifeTotal });
                    }
                }
                for (int i = 0; i < request.PlayersCount - request.PlayersLifeTotals.Count; i++)
                {
                    newPlayers.Add(new Player { LifeTotal = gameDB.LifeTotal });
                }

            }
            else
            {
                for (int i = 0; i < request.PlayersCount; i++)
                {
                    newPlayers.Add(new Player { LifeTotal = gameDB.LifeTotal });
                }
            }

            gameDB.Players ??= new List<Player>(); // Ensure the list is initialized
            gameDB.Players.AddRange(newPlayers);

            await _daoDbContext.SaveChangesAsync();

            
            var content = new List<PlayersStartGameResponse>();
            foreach(var newPlayer in  gameDB.Players)
            {
                content.Add(new PlayersStartGameResponse
                {
                    PlayerId = newPlayer.Id,
                    LifeTotal = newPlayer.LifeTotal,
                });
            }
              

            return (content, $"{gameDB.Name} match started with {gameDB.Players.Count} players");
        }

        public async Task<(PlayersIncreaseLifeTotalResponse>?, string)> IncreaseLifeTotal(PlayersIncreaseLifeTotalRequest request)
    }
}
