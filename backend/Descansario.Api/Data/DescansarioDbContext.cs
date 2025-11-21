using Microsoft.EntityFrameworkCore;
using Descansario.Api.Models;

namespace Descansario.Api.Data;

public class DescansarioDbContext : DbContext
{
    public DescansarioDbContext(DbContextOptions<DescansarioDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> Persons { get; set; } = null!;
    public DbSet<Vacation> Vacations { get; set; } = null!;
    public DbSet<Holiday> Holidays { get; set; } = null!;
    public DbSet<Configuration> Configurations { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Person configuration
        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(p => p.Email).IsUnique();
            entity.Property(p => p.AvailableDays).HasDefaultValue(20);

            // Relación con Vacations
            entity.HasMany(p => p.Vacations)
                  .WithOne(v => v.Person)
                  .HasForeignKey(v => v.PersonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Vacation configuration
        modelBuilder.Entity<Vacation>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.StartDate).IsRequired();
            entity.Property(v => v.EndDate).IsRequired();
            // WorkingDaysCount ya NO se persiste - se calcula on-demand
            entity.Property(v => v.Status)
                  .HasConversion<string>()
                  .HasDefaultValue(VacationStatus.Pending);
        });

        // Holiday configuration
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Date).IsRequired();
            entity.Property(h => h.Name).IsRequired().HasMaxLength(200);
            entity.Property(h => h.Country)
                  .HasConversion<string>()
                  .IsRequired();
            entity.Property(h => h.Region).HasMaxLength(100);

            // Índice compuesto para evitar duplicados
            entity.HasIndex(h => new { h.Date, h.Country, h.Region }).IsUnique();
        });

        // Configuration configuration
        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FirstDayOfWeek).HasDefaultValue(1);
            entity.Property(c => c.WeekendDays).HasDefaultValue("0,6");
            entity.Property(c => c.DefaultCountry)
                  .HasConversion<string>()
                  .HasDefaultValue(Country.AR);
        });

        // Seed configuration inicial
        modelBuilder.Entity<Configuration>().HasData(
            new Configuration
            {
                Id = 1,
                FirstDayOfWeek = 1,
                WeekendDays = "0,6",
                DefaultCountry = Country.AR
            }
        );

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Role)
                  .HasConversion<string>()
                  .HasDefaultValue(UserRole.User);
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("datetime('now')");
        });

        // Seed usuario admin inicial
        // Password: "admin123" (cambiar en producción)
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Email = "admin@descansario.com",
                // BCrypt hash de "admin123"
                PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIr.8HJZgi",
                Name = "Administrador",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
