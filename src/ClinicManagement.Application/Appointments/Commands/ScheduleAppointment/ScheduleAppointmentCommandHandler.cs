using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Shared.Events;

namespace ClinicManagement.Application.Appointments.Commands.ScheduleAppointment;

public class ScheduleAppointmentCommandHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly IPatientRepository _patients;
    private readonly IDoctorRepository _doctors;
    private readonly IEventPublisher _publisher;

    public ScheduleAppointmentCommandHandler(
        IAppointmentRepository appointments,
        IPatientRepository patients,
        IDoctorRepository doctors,
        IEventPublisher publisher)
    {
        _appointments = appointments;
        _patients = patients;
        _doctors = doctors;
        _publisher = publisher;
    }

    public async Task<AppointmentDto> Handle(ScheduleAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var patient = await _patients.GetByIdAsync(command.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), command.PatientId);

        if (!patient.IsActive)
            throw new DomainException("Cannot schedule an appointment for an inactive patient.");

        var doctor = await _doctors.GetByIdAsync(command.DoctorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Doctor), command.DoctorId);

        if (!doctor.IsActive)
            throw new DomainException("Cannot schedule an appointment with an inactive doctor.");

        var hasConflict = await _appointments.HasConflictAsync(command.DoctorId, command.ScheduledAt, command.DurationMinutes, cancellationToken: cancellationToken);
        if (hasConflict)
            throw new DomainException("The doctor already has an appointment in that time slot.");

        var appointment = Appointment.Schedule(command.PatientId, command.DoctorId, command.ScheduledAt, command.DurationMinutes, command.Notes);

        await _appointments.AddAsync(appointment, cancellationToken);
        await _appointments.SaveChangesAsync(cancellationToken);

        await _publisher.PublishAsync(new AppointmentScheduledEvent(
            appointment.Id,
            patient.Id,
            patient.FullName,
            doctor.Id,
            doctor.FullName,
            appointment.ScheduledAt,
            appointment.DurationMinutes,
            DateTime.UtcNow), cancellationToken);

        return appointment.ToDto();
    }
}
