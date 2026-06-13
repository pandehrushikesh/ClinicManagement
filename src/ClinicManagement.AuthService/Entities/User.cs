using BC = BCrypt.Net.BCrypt;

namespace ClinicManagement.AuthService.Entities;

public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string email, string passwordHash, string role = "Staff")
    {
        return new User
        {
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool VerifyPassword(string password) =>
        BC.Verify(password, PasswordHash);
}
