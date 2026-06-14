using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Commands.CancelAppointment;

public class CancelAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly ICacheService _cache;

    public CancelAppointmentCommandHandler(IAppointmentRepository appointments, ICacheService cache)
    {
        _appointments = appointments;
        _cache = cache;
    }

    public async Task<AppointmentDto> Handle(CancelAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), command.AppointmentId);

        appointment.Cancel(command.Reason);
        await _appointments.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(
            _cache.RemoveByPrefixAsync("appointments:", cancellationToken),
            _cache.RemoveAsync($"appointments:{command.AppointmentId}", cancellationToken));

        return appointment.ToDto();
    }
}
