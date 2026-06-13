using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Appointments.Queries.GetAppointments;

public record GetAppointmentsQuery(
    int AfterId = 0,
    int PageSize = 20,
    int? PatientId = null,
    int? DoctorId = null,
    AppointmentStatus? Status = null
);
