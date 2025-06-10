using LifeCounterAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options){ }

    public DbSet<Game> Games { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Player> Players { get; set; }

    public DbSet<User> Users { get; set; } 
    public DbSet<LifeCounter> LifeCounters { get; set; }

}