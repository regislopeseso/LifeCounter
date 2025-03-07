using LifeCounterAPI.Models.Dtos.Request.Admin;
using LifeCounterAPI.Models.Dtos.Response.Admin;
using LifeCounterAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeCounterAPI.Services
{
    public class AdminsService
    {
        private readonly ApplicationDbContext _daoDbContext;

        public AdminsService(ApplicationDbContext daoDbContext)
        {
            _daoDbContext = daoDbContext;
        }

        public async Task<(AdminsCreateGameResponse?, string)> CreateGame(AdminsCreateGameRequest? request)
        {          
            var (isValid, message) = CreateValidation(request);

            if (isValid == false)
            {
                return (null, message);
            }

            var exists = await this._daoDbContext
                                   .Games
                                   .AnyAsync(a => a.Name == request!.GameName);

            if (exists == true)
            {
                return (null, $"Error: {request!.GameName} already exists");
            }

            var newGame = new Game()
            {
                Name = request!.GameName!,
                PlayersStartingLife = request.PlayersStartingLife.HasValue == true ? request.PlayersStartingLife.Value : 99,
                FixedMaxLife = request.FixedMaxLife == true ? request.FixedMaxLife.Value : false,
                AutoEndMatch = request.AutoEndMatch == true ? request.AutoEndMatch.Value : false
            };

            this._daoDbContext.Add(newGame);

            await this._daoDbContext.SaveChangesAsync();

            return (null, "New game created successfully");
        }

        public static (bool, string) CreateValidation(AdminsCreateGameRequest? request)
        {
            if (request == null)
            {
                return (false, "Error: no information provided");
            }

            if (String.IsNullOrWhiteSpace(request.GameName) == true)
            {
                return (false, "Error: informing a name for the new game is mandatory ");
            }

            if (request.PlayersStartingLife.HasValue == false)
            {
                return (false, "Error: informing the players starting life for the new game is mandatory");
            }

            if(request.FixedMaxLife.HasValue == false)
            {
                return (false, "Error: informing if max life should be fixed or not is mandatory.");
            }

            if(request.AutoEndMatch.HasValue == false)
            {
                return (false, "Error: informing if the auto end battle should be on or off is mandatory.");
            }

            return (true, String.Empty);
        }

        public async Task<(AdminsEditGameResponse?, string)> EditGame(AdminsEditGameRequest? request)
        {            
            var (isValid, message) = EditValidation(request);

            if (isValid == false)
            {
                return (null, message);
            }

            var gameDB = await this._daoDbContext
                                   .Games
                                   .FirstOrDefaultAsync(a => a.Id == request!.GameId);

            if (gameDB == null)
            {
                return (null, "Error: game not found");
            }

            if(gameDB.IsDeleted == true)
            {
                return (null, "Error: this game has been deleted");
            }

            var exists = await this._daoDbContext
                                   .Games
                                   .AnyAsync(a => a.Name == request!.GameName && a.Id != request.GameId);

            if (exists == true)
            {
                return (null, $"Error: {request!.GameName} already exists");
            }            

            gameDB.Name = request!.GameName != null ? request.GameName : string.Empty;
            gameDB.PlayersStartingLife = request.StartingLife.HasValue == true ? request.StartingLife.Value : 99;
            gameDB.FixedMaxLife = request.FixedMaxLife == true ? request.FixedMaxLife.Value : false;
            gameDB.AutoEndMatch = request.AutoEndMatch == true ? request.AutoEndMatch.Value : false;

            await this._daoDbContext.SaveChangesAsync();

            return (null, "Game edited successfully");
        }

        public static (bool, string) EditValidation(AdminsEditGameRequest? request)
        {
            if (request == null)
            {
                return (false, "Error: no information was provided");
            }

            if (request.GameId <= 0)
            {
                return (false, $"Error: invalid GameId: {request.GameId}. It must be a positive value");
            }

            if (String.IsNullOrWhiteSpace(request.GameName) == true)
            {
                return (false, "Error: informing a name for the game being edited is mandatory ");
            }

            if (request.StartingLife.HasValue == false)
            {
                return (false, "Error: informing the players StatingLife for the game being edited is mandatory");
            }

            if(request.AutoEndMatch.HasValue == false)
            {
                return (false, "Error: informing if the auto end match should be on or off for the game being edited is mandatory.");
            }

            return (true, String.Empty);
        }

        public async Task<(AdminsDeleteGameResponse?, string)> DeleteGame(AdminsDeleteGameRequest? request)
        {
            var (isValid, message) = DeleteGameValidation(request);

            if (isValid == false)
            {
                return (null, message);
            }

            var exists = await this._daoDbContext
                                   .Games
                                   .AnyAsync(a => a.Id == request!.GameId);

            if (exists == false)
            {
                return (null, "Error: requested GameId does not exist");
            }

            var gameDB = await this._daoDbContext
                                   .Games                             
                                   .FirstOrDefaultAsync(a => a.Id == request!.GameId);

            if (gameDB == null)
            {
                return (null, "Error: game not found");
            }

            if(gameDB.IsDeleted == true)
            {
                return (null, "Error: this game was has been deleted");
            }
                     
            var (areMatchesFinished, reportMessage) = await MatchesService.FinishMatch(_daoDbContext, request!.GameId);

            if (areMatchesFinished == false)
            {
                return (null,  reportMessage);
            }

            gameDB.IsDeleted = true;

            await this._daoDbContext.SaveChangesAsync();

            return (null, "Game deleted successfully" + reportMessage);
        }
        private static (bool, string) DeleteGameValidation(AdminsDeleteGameRequest? request)
        {
            if (request == null)
            {
                return (false, "Error: no informations was provided");
            }

            if(request.GameId <= 0)
            {
                return (false, $"Error: invalid GameId: {request.GameId}. It must be a positive value");
            }

            return (true, String.Empty);
        }
    }
}
