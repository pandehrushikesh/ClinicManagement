namespace ClinicManagement.Shared.Events;

public record AppointmentScheduledEvent(
    int AppointmentId,
    int PatientId,
    string PatientFullName,
    int DoctorId,
    string DoctorFullName,
    DateTime ScheduledAt,
    int DurationMinutes,
    DateTime OccurredAt
);
