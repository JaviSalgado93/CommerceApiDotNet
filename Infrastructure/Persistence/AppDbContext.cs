using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<Department> Departments { get; set; }
        public DbSet<Municipality> Municipalities { get; set; }
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

            // Departments
            modelBuilder.Entity<Department>()
                .ToTable("Departments")
                .HasKey(d => d.Id);

            modelBuilder.Entity<Department>()
                .Property(d => d.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Code)
                .IsUnique();

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Name)
                .IsUnique();

            // Municipalities
            modelBuilder.Entity<Municipality>()
                .ToTable("Municipalities")
                .HasKey(m => m.Id);

            modelBuilder.Entity<Municipality>()
                .Property(m => m.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Municipality>()
                .HasIndex(m => m.Code)
                .IsUnique();

            modelBuilder.Entity<Municipality>()
                .HasOne(m => m.Department)
                .WithMany(d => d.Municipalities)
                .HasForeignKey(m => m.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Merchants - IMPORTANTE: Tiene trigger INSTEAD OF INSERT
            modelBuilder.Entity<Merchant>()
                .ToTable(tb => tb.HasTrigger("tr_Merchants_Insert"))
                .HasKey(m => m.Id);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.Id)
                .ValueGeneratedOnAdd(); // Identity (auto-increment)

            modelBuilder.Entity<Merchant>()
                .HasIndex(m => m.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL"); // Permite múltiples NULLs

            modelBuilder.Entity<Merchant>()
                .HasOne(m => m.Municipality)
                .WithMany(mu => mu.Merchants)
                .HasForeignKey(m => m.MunicipalityId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Seed Data - Establecimientos (SOLO si no existen)
            SeedEstablishments(modelBuilder);
        }

        /// <summary>
        /// Siembra datos de establecimientos para las pruebas.
        /// Se crea SOLO si la tabla está vacía.
        /// </summary>
        private static void SeedEstablishments(ModelBuilder modelBuilder)
        {
            // Solo seedear si no hay datos
            var establishments = new List<Establishment>
            {
                // Merchant 1 (Id=1): Empresa Comercial 1
                new Establishment { Id = 1, MerchantId = 1, Name = "Sucursal Centro Bogotá", Revenue = 150000.50m, EmployeeCount = 25, CreatedAt = DateTime.UtcNow },
                new Establishment { Id = 2, MerchantId = 1, Name = "Sucursal Suba Bogotá", Revenue = 120000.75m, EmployeeCount = 18, CreatedAt = DateTime.UtcNow },
                new Establishment { Id = 3, MerchantId = 1, Name = "Sucursal Chapinero Bogotá", Revenue = 95000.25m, EmployeeCount = 12, CreatedAt = DateTime.UtcNow },
                
                // Merchant 2 (Id=2): Empresa Comercial 2
                new Establishment { Id = 4, MerchantId = 2, Name = "Sucursal Centro Medellín", Revenue = 200000.00m, EmployeeCount = 35, CreatedAt = DateTime.UtcNow },
                new Establishment { Id = 5, MerchantId = 2, Name = "Sucursal Laureles Medellín", Revenue = 175000.50m, EmployeeCount = 28, CreatedAt = DateTime.UtcNow },
                
                // Merchant 3 (Id=3): Empresa Comercial 3
                new Establishment { Id = 6, MerchantId = 3, Name = "Sucursal Centro Cali", Revenue = 180000.25m, EmployeeCount = 30, CreatedAt = DateTime.UtcNow },
                new Establishment { Id = 7, MerchantId = 3, Name = "Sucursal San Fernando Cali", Revenue = 165000.75m, EmployeeCount = 25, CreatedAt = DateTime.UtcNow },
                new Establishment { Id = 8, MerchantId = 3, Name = "Sucursal Puerto Cali", Revenue = 155000.00m, EmployeeCount = 22, CreatedAt = DateTime.UtcNow },
                
                // Merchant 4 (Id=4): Empresa Comercial 4
                new Establishment { Id = 9, MerchantId = 4, Name = "Sucursal Centro Barranquilla", Revenue = 190000.50m, EmployeeCount = 32, CreatedAt = DateTime.UtcNow },
                new Establishment { Id = 10, MerchantId = 4, Name = "Sucursal Riomar Barranquilla", Revenue = 170000.25m, EmployeeCount = 27, CreatedAt = DateTime.UtcNow },
                
                // Merchant 5 (Id=5): Empresa Comercial 5 - Sin establecimientos
            };

            modelBuilder.Entity<Establishment>().HasData(establishments);
        }
    }
}
