namespace ClinicManagement.Application.Common.DTOs;

public record PatientDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    string Email,
    string Phone,
    bool IsActive,
    DateTime CreatedAt
);
