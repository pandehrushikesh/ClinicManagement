using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Commands.CancelAppointment;

public class CancelAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointments;

    public CancelAppointmentCommandHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<AppointmentDto> Handle(CancelAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), command.AppointmentId);

        appointment.Cancel(command.Reason);
        await _appointments.SaveChangesAsync(cancellationToken);

        return appointment.ToDto();
    }
}
