using System.Text.RegularExpressions;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("Email is required.");
        if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) throw new DomainException($"'{value}' is not a valid email address.");
        Value = value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
