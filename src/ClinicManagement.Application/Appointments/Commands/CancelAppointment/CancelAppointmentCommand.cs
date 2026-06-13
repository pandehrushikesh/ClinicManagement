namespace ClinicManagement.Application.Appointments.Commands.CancelAppointment;

public record CancelAppointmentCommand(int AppointmentId, string Reason);
