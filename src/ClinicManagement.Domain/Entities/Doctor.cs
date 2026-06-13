using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Doctor
{
    public int Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Specialty { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private Doctor() { }

    public static Doctor Create(string firstName, string lastName, string specialty)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name is required.");
        if (string.IsNullOrWhiteSpace(specialty)) throw new DomainException("Specialty is required.");

        return new Doctor
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Specialty = specialty.Trim(),
            IsActive = true
        };
    }

    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("Doctor is already inactive.");
        IsActive = false;
    }

    public string FullName => $"Dr. {FirstName} {LastName}";
}
