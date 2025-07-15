using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence.Game;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<GameEntity> Games { get; set; }
    public DbSet<ParticipantEntity> Players { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameEntity>(e =>
        {
            e.ToTable("Games");
            e.HasKey(x => x.Id);
            e.Property(x => x.Phase).IsRequired();
            e.Property(x => x.Created).IsRequired();
            e.Property(x => x.CurrentPlayers);
            e.Property(x => x.MaxPlayers);
        });

        modelBuilder.Entity<ParticipantEntity>(e =>
        {
            e.ToTable("Participants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nick).IsRequired();
            e
                .HasOne<GameEntity>()
                .WithMany()
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}