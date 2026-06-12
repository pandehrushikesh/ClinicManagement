using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Domain.ValueObjects;

namespace ClinicManagement.Domain.Entities;

public class Patient
{
    public int Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public DateOnly DateOfBirth { get; private set; }
    public Email Email { get; private set; } = default!;
    public PhoneNumber Phone { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Patient() { }

    public static Patient Create(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Email email,
        PhoneNumber phone)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name is required.");
        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow)) throw new DomainException("Date of birth must be in the past.");

        return new Patient
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            Email = email,
            Phone = phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateContactInfo(Email email, PhoneNumber phone)
    {
        Email = email;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("Patient is already inactive.");
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (IsActive) throw new DomainException("Patient is already active.");
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";
}
