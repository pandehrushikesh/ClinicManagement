using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;
using FluentAssertions;

namespace ClinicManagement.UnitTests.Domain.Entities;

public class DoctorTests
{
    [Fact]
    public void Create_ValidData_ReturnsActiveDoctor()
    {
        var doctor = Doctor.Create("Alice", "Smith", "Cardiology");

        doctor.FirstName.Should().Be("Alice");
        doctor.LastName.Should().Be("Smith");
        doctor.Specialty.Should().Be("Cardiology");
        doctor.FullName.Should().Be("Dr. Alice Smith");
        doctor.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Smith", "Cardiology")]
    [InlineData("Alice", "", "Cardiology")]
    [InlineData("Alice", "Smith", "")]
    public void Create_MissingField_ThrowsDomainException(string firstName, string lastName, string specialty)
    {
        var act = () => Doctor.Create(firstName, lastName, specialty);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Fact]
    public void Deactivate_ActiveDoctor_SetsInactive()
    {
        var doctor = Doctor.Create("Alice", "Smith", "Cardiology");
        doctor.Deactivate();
        doctor.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsDomainException()
    {
        var doctor = Doctor.Create("Alice", "Smith", "Cardiology");
        doctor.Deactivate();

        var act = () => doctor.Deactivate();
        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }
}
