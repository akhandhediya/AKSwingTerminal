using Microsoft.EntityFrameworkCore;
using AKSwingTerminal.Models;

namespace AKSwingTerminal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ApiCredentials> ApiCredentials { get; set; }
        public DbSet<FyersToken> FyersTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // A user can have many API credentials
                entity.HasMany(e => e.ApiCredentials)
                      .WithOne(e => e.User)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // A user can have many tokens
                entity.HasMany(e => e.FyersTokens)
                      .WithOne(e => e.User)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // API Credentials configuration
            modelBuilder.Entity<ApiCredentials>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AppId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AppSecret).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RedirectUrl).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            // Fyers Token configuration
            modelBuilder.Entity<FyersToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(500);
                entity.Property(e => e.RefreshToken).HasMaxLength(500);
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            // Seed initial admin user if needed
            // This is useful for single-user applications as requested
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Admin User",
                    Email = "admin@example.com",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
