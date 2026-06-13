using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Commands.RescheduleAppointment;

public class RescheduleAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointments;

    public RescheduleAppointmentCommandHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<AppointmentDto> Handle(RescheduleAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), command.AppointmentId);

        var hasConflict = await _appointments.HasConflictAsync(
            appointment.DoctorId, command.NewScheduledAt, command.NewDurationMinutes,
            excludeAppointmentId: command.AppointmentId, cancellationToken: cancellationToken);

        if (hasConflict)
            throw new DomainException("The doctor already has an appointment in that time slot.");

        appointment.Reschedule(command.NewScheduledAt, command.NewDurationMinutes);
        await _appointments.SaveChangesAsync(cancellationToken);

        return appointment.ToDto();
    }
}
