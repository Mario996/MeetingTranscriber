using Common.Model;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { }

        public DbSet<User> User { get; set; }
        public DbSet<Transcription> Transcription { get; set; }
        public DbSet<TranscriptionUser> TranscriptionUser { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TranscriptionUser>()
                .HasKey(tu => new { tu.TranscriptionId, tu.UserId });

            modelBuilder.Entity<TranscriptionUser>()
                .HasOne(tu => tu.Transcription)
                .WithMany(t => t.TranscriptionUsers)
                .HasForeignKey(tu => tu.TranscriptionId);

            modelBuilder.Entity<TranscriptionUser>()
                .HasOne(tu => tu.User)
                .WithMany(u => u.TranscriptionUsers)
                .HasForeignKey(tu => tu.UserId);
        }
    }
}
