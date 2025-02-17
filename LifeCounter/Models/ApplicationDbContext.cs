using LifeCounterAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options){ }

    public DbSet<LifeCounter> LifeCounters { get; set; }

}