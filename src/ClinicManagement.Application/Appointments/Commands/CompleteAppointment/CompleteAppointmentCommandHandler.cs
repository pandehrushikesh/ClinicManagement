using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Commands.CompleteAppointment;

public class CompleteAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly ICacheService _cache;

    public CompleteAppointmentCommandHandler(IAppointmentRepository appointments, ICacheService cache)
    {
        _appointments = appointments;
        _cache = cache;
    }

    public async Task<AppointmentDto> Handle(CompleteAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), command.AppointmentId);

        appointment.Complete();
        await _appointments.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(
            _cache.RemoveByPrefixAsync("appointments:", cancellationToken),
            _cache.RemoveAsync($"appointments:{command.AppointmentId}", cancellationToken));

        return appointment.ToDto();
    }
}
