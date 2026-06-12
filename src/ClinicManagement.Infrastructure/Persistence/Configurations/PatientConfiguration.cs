using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DateOfBirth)
            .IsRequired();

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasConversion(e => e.Value, v => new Email(v));

        builder.Property(p => p.Phone)
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion(ph => ph.Value, v => new PhoneNumber(v));

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.LastName);
    }
}
