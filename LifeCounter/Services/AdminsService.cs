using LifeCounterAPI.Models.Dtos.Request.Admin;
using LifeCounterAPI.Models.Dtos.Response.Admin;
using LifeCounterAPI.Models.Entities;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;

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
            var (isValid, message) = IsValid_Create(request);

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
                StartingLife = request.LifeTotal.HasValue == true ? request.LifeTotal.Value : 99,
                FixedMaxLife = request.FixedMaxLife == true ? request.FixedMaxLife.Value : false,
            };

            this._daoDbContext.Add(newGame);

            await this._daoDbContext.SaveChangesAsync();

            return (null, "New game created successfully");
        }

        public static (bool, string) IsValid_Create(AdminsCreateGameRequest request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (String.IsNullOrWhiteSpace(request.GameName) == true)
            {
                return (false, "Error: informing a name for the new game is mandatory ");
            }

            if (request.LifeTotal.HasValue == false)
            {
                return (false, "Error: informing a LifeTotal for the new game is mandatory");
            }

            if(request.FixedMaxLife.HasValue == false)
            {
                return (false, "Error: informing if the game has max life total fixed or not is mandatory. Inform either: 1 - yes or 0 - no.");
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
            gameDB.StartingLife = request.LifeTotal.HasValue == true ? request.LifeTotal.Value : 99;

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
                                   .Where(a => a.Id == request.GameId)
                                   .FirstOrDefaultAsync();

            if (gameDB == null)
            {
                return (null, "Error: this game has been already deleted");
            }
                     
            var (areMatchesFinished, message) = await MatchesService.FinishMatch(_daoDbContext, request.GameId);

            if (areMatchesFinished == false)
            {
                return (null,  message);
            }

            gameDB.IsDeleted = true;

            await this._daoDbContext.SaveChangesAsync();

            return (null, "Game deleted successfully. " + message);
        }
    }
}
