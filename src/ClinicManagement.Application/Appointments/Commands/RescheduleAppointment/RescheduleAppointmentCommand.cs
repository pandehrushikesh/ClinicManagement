namespace ClinicManagement.Application.Appointments.Commands.RescheduleAppointment;

public record RescheduleAppointmentCommand(int AppointmentId, DateTime NewScheduledAt, int NewDurationMinutes);
