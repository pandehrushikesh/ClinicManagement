using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Appointments;

internal static class AppointmentMappingExtensions
{
    internal static AppointmentDto ToDto(this Appointment a) => new(
        a.Id,
        a.PatientId,
        a.Patient?.FullName ?? string.Empty,
        a.DoctorId,
        a.Doctor?.FullName ?? string.Empty,
        a.ScheduledAt,
        a.DurationMinutes,
        a.Status.ToString(),
        a.Notes,
        a.CancellationReason,
        a.CreatedAt);
}
