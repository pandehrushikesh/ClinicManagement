using ClinicManagement.AuthService.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.AuthService.Persistence;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.Role).IsRequired().HasMaxLength(50);
            b.Property(u => u.CreatedAt).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
