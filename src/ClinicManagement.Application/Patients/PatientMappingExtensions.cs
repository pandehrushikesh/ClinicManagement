using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Patients;

internal static class PatientMappingExtensions
{
    internal static PatientDto ToDto(this Patient p) => new(
        p.Id,
        p.FirstName,
        p.LastName,
        p.FullName,
        p.DateOfBirth,
        p.Email.Value,
        p.Phone.Value,
        p.IsActive,
        p.CreatedAt);
}
