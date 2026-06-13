using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Queries.GetAppointmentById;

public class GetAppointmentByIdQueryHandler
{
    private readonly IAppointmentRepository _appointments;

    public GetAppointmentByIdQueryHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery query, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), query.Id);

        return appointment.ToDto();
    }
}
