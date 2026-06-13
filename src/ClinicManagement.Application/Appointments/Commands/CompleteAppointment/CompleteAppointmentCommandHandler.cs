using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Commands.CompleteAppointment;

public class CompleteAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointments;

    public CompleteAppointmentCommandHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<AppointmentDto> Handle(CompleteAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), command.AppointmentId);

        appointment.Complete();
        await _appointments.SaveChangesAsync(cancellationToken);

        return appointment.ToDto();
    }
}
