using Microsoft.EntityFrameworkCore;
using PaL.X.Shared.Models;

namespace PaL.X.Data;

public class PalContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Session> Sessions { get; set; }

    public PalContext(DbContextOptions<PalContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Fallback for design time
            optionsBuilder.UseNpgsql("Host=localhost;Database=PaL.X.v2;Username=postgres;Password=2012704");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}
