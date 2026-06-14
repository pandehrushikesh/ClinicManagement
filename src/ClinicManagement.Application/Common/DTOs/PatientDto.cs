namespace ClinicManagement.Application.Common.DTOs;

public record PatientDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    DateTime DateOfBirth,
    string Email,
    string Phone,
    bool IsActive,
    DateTime CreatedAt
);
