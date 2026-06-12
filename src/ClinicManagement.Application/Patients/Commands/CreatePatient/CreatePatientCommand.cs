using ClinicManagement.Application.Common.DTOs;

namespace ClinicManagement.Application.Patients.Commands.CreatePatient;

public record CreatePatientCommand(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone
);
