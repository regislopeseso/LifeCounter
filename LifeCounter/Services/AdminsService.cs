using LifeCounterAPI.Models.Dtos.Request.Admin;
using LifeCounterAPI.Models.Dtos.Response.Admin;
using LifeCounterAPI.Models.Entities;
using LifeCounterAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeCounterAPI.Services
{
    [Table("battles")]
    public class AdminsService
    {
        private readonly ApplicationDbContext _daoDbContext;

        public AdminsService(ApplicationDbContext daoDbContext)
        {
            _daoDbContext = daoDbContext;
        }

        public async Task<(AdminsCreateGameResponse?, string)> CreateGame(AdminsCreateGameRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            var (isValid, message) = CreateIsValid(request);

            if (isValid == false)
            {
                return (null, message);
            }

            var exists = await this._daoDbContext
                                   .Games
                                   .AnyAsync(a => a.Name == request.GameName);

            if (exists == true)
            {
                return (null, $"Error: {request.GameName} already exists");
            }

            var newGame = new Game()
            {
                Name = request.GameName,
                LifeTotal = request.LifeTotal.HasValue == true ? request.LifeTotal.Value : 99,
            };

            this._daoDbContext.Add(newGame);

            await this._daoDbContext.SaveChangesAsync();

            return (null, "New game created successfully");
        }

        public static (bool, string) CreateIsValid(AdminsCreateGameRequest request)
        {
            if (String.IsNullOrWhiteSpace(request.GameName) == true)
            {
                return (false, "Error: informing a name for the new game is mandatory ");
            }

            if (request.LifeTotal.HasValue == false)
            {
                return (false, "Error: informing a LifeTotal for the new game is mandatory");
            }

            return (true, String.Empty);
        }

        public async Task<(AdminsEditGameResponse?, string)> EditGame(AdminsEditGameRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information provided");
            }

            var (isValid, message) = EditIsValid(request);

            if (isValid == false)
            {
                return (null, message);
            }

            var exists = await this._daoDbContext
                                   .Games
                                   .AnyAsync(a => a.Name == request.GameName && a.Id != request.GameId);

            if (exists == true)
            {
                return (null, $"Error: {request.GameName} already exists");
            }

            var gameDB = await this._daoDbContext
                                          .Games
                                          .FirstOrDefaultAsync(a => a.Id == request.GameId && a.IsDeleted == false);

            if (gameDB == null)
            {
                return (null, "Error: game not found");
            }

            gameDB.Name = request.GameName;
            gameDB.LifeTotal = request.LifeTotal.HasValue == true ? request.LifeTotal.Value : 99;

            await this._daoDbContext.SaveChangesAsync();

            return (null, "Game edited successfully");
        }

        public static (bool, string) EditIsValid(AdminsEditGameRequest request)
        {
            if (String.IsNullOrWhiteSpace(request.GameName) == true)
            {
                return (false, "Error: informing a name for the game being edited is mandatory ");
            }

            if (request.GameId <= 0)
            {
                return (false, $"Error: invalid GameId: {request.GameId}");
            }

            if (request.LifeTotal.HasValue == false)
            {
                return (false, "Error: informing a LifeTotal for the game being edited is mandatory");
            }

            return (true, String.Empty);
        }

        public async Task<(AdminsDeleteGameResponse?, string)> DeleteGame(AdminsDeleteGameRequest request)
        {
            if (request == null || request.GameId <= 0)
            {
                return (null, "Error: invalid GameId");
            }

            var exists = await this._daoDbContext
                                   .Games
                                   .AnyAsync(a => a.Id == request.GameId);

            if (exists == false)
            {
                return (null, "Error: requested GameId does not exist");
            }

            var gameDB = await this._daoDbContext
                                   .Games
                                   .Include(a => a.Matches)
                                   .Where(a => a.Id == request.GameId)
                                   .FirstOrDefaultAsync();
            if (gameDB == null)
            {
                return (null, "Error: this game has been already deleted");
            }

            var matchesDB = gameDB.Matches;
            
            foreach(var match in matchesDB)
            {
                match.IsFinished = true;
            }
            
            gameDB.IsDeleted = true;

            await this._daoDbContext.SaveChangesAsync();

            return (null, "Game deleted successfully. All matches of this game are now set as finished");
        }
    }
}
