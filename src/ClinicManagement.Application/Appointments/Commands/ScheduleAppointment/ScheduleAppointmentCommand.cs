namespace ClinicManagement.Application.Appointments.Commands.ScheduleAppointment;

public record ScheduleAppointmentCommand(
    int PatientId,
    int DoctorId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Notes
);
