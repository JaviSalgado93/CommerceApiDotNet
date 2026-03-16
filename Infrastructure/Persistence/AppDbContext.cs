using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Infrastructure.Persistence.Entities;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #region DbSets
        public DbSet<RoleEntity> Roles { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
        public DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; set; }
        public DbSet<TokenBlacklistEntity> TokenBlacklist { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<Establishment> Establishments { get; set; }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    configuration.GetConnectionString("dbContext"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure()
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users - IMPORTANTE: Desactivar OUTPUT para que funcione con triggers INSTEAD OF
            modelBuilder.Entity<UserEntity>()
                .HasKey(u => u.Id);

            // No usar identity, ya que el trigger maneja las inserciones
            modelBuilder.Entity<UserEntity>()
                .Property(u => u.Id)
                .HasDefaultValueSql("NEWID()")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<UserEntity>()
                .ToTable(tb => tb.HasTrigger("tr_Users_Update")); // Indicar que hay trigger

            modelBuilder.Entity<UserEntity>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<UserEntity>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UserEntity>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // RefreshTokens
            modelBuilder.Entity<RefreshTokenEntity>()
                .HasKey(rt => rt.Id);

            modelBuilder.Entity<RefreshTokenEntity>()
                .Property(rt => rt.Id)
                .ValueGeneratedNever(); // No generar valores automáticos

            modelBuilder.Entity<RefreshTokenEntity>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshTokenEntity>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // PasswordResetTokens
            modelBuilder.Entity<PasswordResetTokenEntity>()
                .HasKey(prt => prt.Id);

            modelBuilder.Entity<PasswordResetTokenEntity>()
                .Property(prt => prt.Id)
                .ValueGeneratedNever(); // No generar valores automáticos

            modelBuilder.Entity<PasswordResetTokenEntity>()
                .HasIndex(prt => prt.Token)
                .IsUnique();

            modelBuilder.Entity<PasswordResetTokenEntity>()
                .HasOne(prt => prt.User)
                .WithMany()
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // TokenBlacklist
            modelBuilder.Entity<TokenBlacklistEntity>()
                .HasKey(tb => tb.Id);

            modelBuilder.Entity<TokenBlacklistEntity>()
                .Property(tb => tb.Id)
                .ValueGeneratedNever(); // No generar valores automáticos

            modelBuilder.Entity<TokenBlacklistEntity>()
                .HasIndex(tb => tb.TokenHash)
                .IsUnique();

            modelBuilder.Entity<TokenBlacklistEntity>()
                .HasOne(tb => tb.User)
                .WithMany()
                .HasForeignKey(tb => tb.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Merchants
            modelBuilder.Entity<Merchant>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.Id)
                .ValueGeneratedOnAdd(); // Identity (auto-increment)

            modelBuilder.Entity<Merchant>()
                .HasIndex(m => m.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL"); // Permite múltiples NULLs

            modelBuilder.Entity<Merchant>()
                .HasOne(m => m.CreatedByUser)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Establishments
            modelBuilder.Entity<Establishment>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Establishment>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd(); // Identity (auto-increment)

            modelBuilder.Entity<Establishment>()
                .HasOne(e => e.Merchant)
                .WithMany(m => m.Establishments)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra Merchant, se borran Establishments
        }
    }
}
