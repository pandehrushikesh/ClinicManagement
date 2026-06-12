using System.Text.RegularExpressions;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.ValueObjects;

public record PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("Phone number is required.");
        var digits = Regex.Replace(value, @"\D", "");
        if (digits.Length < 7 || digits.Length > 15) throw new DomainException($"'{value}' is not a valid phone number.");
        Value = value.Trim();
    }

    public override string ToString() => Value;
}
