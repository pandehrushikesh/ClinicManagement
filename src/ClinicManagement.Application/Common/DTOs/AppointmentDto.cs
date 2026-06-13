namespace ClinicManagement.Application.Common.DTOs;

public record AppointmentDto(
    int Id,
    int PatientId,
    string PatientFullName,
    int DoctorId,
    string DoctorFullName,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Status,
    string? Notes,
    string? CancellationReason,
    DateTime CreatedAt
);
