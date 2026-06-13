using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Domain.ValueObjects;
using FluentAssertions;

namespace ClinicManagement.UnitTests.Domain.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("first.last+tag@clinic.org")]
    public void Create_ValidEmail_NormalisesToLowercase(string input)
    {
        var email = new Email(input);
        email.Value.Should().Be(input.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNull_ThrowsDomainException(string? input)
    {
        var act = () => new Email(input!);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    [InlineData("missing@")]
    [InlineData("spaces in@email.com")]
    public void Create_InvalidFormat_ThrowsDomainException(string input)
    {
        var act = () => new Email(input);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TwoEmails_SameValue_AreEqual()
    {
        var a = new Email("test@example.com");
        var b = new Email("TEST@example.com");
        a.Should().Be(b);
    }
}
