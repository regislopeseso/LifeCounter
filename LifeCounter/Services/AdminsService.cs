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

        public async Task<(AdminsCreateLifeCounterResponse?, string)> CreateLifeCounter(AdminsCreateLifeCounterRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information providade");
            }

            var (isValid, message) = CreateIsValid(request);

            if (isValid == false)
            {
                return (null, message);
            }

            var exists = await this._daoDbContext
                .LifeCounters
                .AnyAsync(a => a.GameName == request.GameName);

            if (exists == true)
            {
                return (null, $"Error: a Life Counter for the game: {request.GameName} already exists");
            }


            var newLifeCounter = new LifeCounter()
            {
                GameName = request.GameName,
            };

            if (request.LifeTotal != 0)
            {
                newLifeCounter.LifeTotal = request.LifeTotal;
            };

            this._daoDbContext.Add(newLifeCounter);

            await this._daoDbContext.SaveChangesAsync();

            return (null, "New Life Counter created successfully");
        }

        public static (bool, string) CreateIsValid(AdminsCreateLifeCounterRequest request)
        {
            if (String.IsNullOrWhiteSpace(request.GameName) == true)
            {
                return (false, "Error: naming the game is mandatory");
            }

            return (true, String.Empty);
        }

        public async Task<(AdminsEditLifeCounterResponse?, string)> EditLifeCounter(AdminsEditLifeCounterRequest request)
        {
            if (request == null)
            {
                return (null, "Error: no information providade");
            }

            var (isValid, message) = EditIsValid(request);

            if (isValid == false)
            {
                return (null, message);
            }

            var exists = await this._daoDbContext
                .LifeCounters
                .AnyAsync(a => a.GameName == request.GameName && a.Id != request.LifeCounterId);

            if (exists == true)
            {
                return (null, $"Error: a Life Counter for the game: {request.GameName} already exists");
            }

            var lifeCounterDB = await this._daoDbContext
                                          .LifeCounters
                                          .FirstOrDefaultAsync(a => a.Id == request.LifeCounterId);

            lifeCounterDB.GameName = request.GameName;

            if (request.LifeTotal != 0)
            {
                lifeCounterDB.LifeTotal = request.LifeTotal;
            }

            await this._daoDbContext.SaveChangesAsync();

            return (null, "Life Counter edited successfully");
        }

        public static (bool, string) EditIsValid(AdminsEditLifeCounterRequest request)
        {
            if (String.IsNullOrWhiteSpace(request.GameName) == true)
            {
                return (false, "Error, naming the game is mandatory");
            }

            if (request.LifeCounterId <= 0)
            {
                return (false, $"Error: invalid LifeCounterId: {request.LifeCounterId}");
            }

            return (true, String.Empty);
        }

        public async Task<(AdminsRemoveLifeCounterResponse?, string)> RemoveLifeCounter(AdminsRemoveLifeCounterRequest request)
        {
            if (request == null || request.LifeCounterId <= 0)
            {
                return (null, "Error: invalid LifeCounterId");
            }

            var exists = await this._daoDbContext
                .LifeCounters
                .AnyAsync(a => a.Id == request.LifeCounterId);

            if (exists == false)
            {
                return (null, "Error: requested LifeCounterId does not exist");
            }

            try
            {
                var lifeCounterId = await this._daoDbContext
                                              .LifeCounters
                                              .Where(a => a.Id == request.LifeCounterId)
                                              .ExecuteDeleteAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete Life Counter: {ex.Message}", ex);
            }

            return (null, "Life Counter removed successfully");
        }
    }
}
