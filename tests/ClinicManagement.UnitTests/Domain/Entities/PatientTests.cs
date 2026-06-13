using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Domain.ValueObjects;
using FluentAssertions;

namespace ClinicManagement.UnitTests.Domain.Entities;

public class PatientTests
{
    private static Patient ValidPatient() => Patient.Create(
        "John", "Doe",
        DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
        new Email("john@example.com"),
        new PhoneNumber("9876543210"));

    [Fact]
    public void Create_ValidData_ReturnsActivePatient()
    {
        var patient = ValidPatient();

        patient.FirstName.Should().Be("John");
        patient.LastName.Should().Be("Doe");
        patient.FullName.Should().Be("John Doe");
        patient.IsActive.Should().BeTrue();
        patient.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("   ", "Doe")]
    [InlineData("John", "")]
    [InlineData("John", "   ")]
    public void Create_MissingName_ThrowsDomainException(string firstName, string lastName)
    {
        var act = () => Patient.Create(
            firstName, lastName,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
            new Email("john@example.com"),
            new PhoneNumber("9876543210"));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_FutureDateOfBirth_ThrowsDomainException()
    {
        var act = () => Patient.Create(
            "John", "Doe",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            new Email("john@example.com"),
            new PhoneNumber("9876543210"));

        act.Should().Throw<DomainException>().WithMessage("*past*");
    }

    [Fact]
    public void Deactivate_ActivePatient_SetsInactive()
    {
        var patient = ValidPatient();
        patient.Deactivate();
        patient.IsActive.Should().BeFalse();
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsDomainException()
    {
        var patient = ValidPatient();
        patient.Deactivate();

        var act = () => patient.Deactivate();
        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }

    [Fact]
    public void Reactivate_InactivePatient_SetsActive()
    {
        var patient = ValidPatient();
        patient.Deactivate();
        patient.Reactivate();
        patient.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_AlreadyActive_ThrowsDomainException()
    {
        var patient = ValidPatient();

        var act = () => patient.Reactivate();
        act.Should().Throw<DomainException>().WithMessage("*already active*");
    }

    [Fact]
    public void UpdateContactInfo_ChangesEmailAndPhone()
    {
        var patient = ValidPatient();
        var newEmail = new Email("new@example.com");
        var newPhone = new PhoneNumber("1234567890");

        patient.UpdateContactInfo(newEmail, newPhone);

        patient.Email.Should().Be(newEmail);
        patient.Phone.Should().Be(newPhone);
        patient.UpdatedAt.Should().NotBeNull();
    }
}
