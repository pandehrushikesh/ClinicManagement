using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Domain.ValueObjects;
using FluentAssertions;

namespace ClinicManagement.UnitTests.Domain.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+1-800-555-0199")]
    [InlineData("9876543210")]
    [InlineData("+91 98765 43210")]
    public void Create_ValidPhone_Succeeds(string input)
    {
        var phone = new PhoneNumber(input);
        phone.Value.Should().Be(input.Trim());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNull_ThrowsDomainException(string? input)
    {
        var act = () => new PhoneNumber(input!);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("123")]       // too short
    [InlineData("12345678901234567")]  // too long
    public void Create_InvalidLength_ThrowsDomainException(string input)
    {
        var act = () => new PhoneNumber(input);
        act.Should().Throw<DomainException>();
    }
}
