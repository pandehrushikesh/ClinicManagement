using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Appointment
{
    public int Id { get; private set; }
    public int PatientId { get; private set; }
    public int DoctorId { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Notes { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public Patient Patient { get; private set; } = default!;
    public Doctor Doctor { get; private set; } = default!;

    private Appointment() { }

    public static Appointment Schedule(int patientId, int doctorId, DateTime scheduledAt, int durationMinutes, string? notes = null)
    {
        if (patientId <= 0) throw new DomainException("Invalid patient.");
        if (doctorId <= 0) throw new DomainException("Invalid doctor.");
        if (scheduledAt <= DateTime.UtcNow) throw new DomainException("Appointment must be scheduled in the future.");
        if (durationMinutes <= 0 || durationMinutes > 480) throw new DomainException("Duration must be between 1 and 480 minutes.");

        return new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ScheduledAt = scheduledAt,
            DurationMinutes = durationMinutes,
            Notes = notes?.Trim(),
            Status = AppointmentStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new DomainException($"Only a Scheduled appointment can be confirmed. Current status: {Status}.");

        Status = AppointmentStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new DomainException($"Only a Confirmed appointment can be completed. Current status: {Status}.");

        Status = AppointmentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new DomainException($"Cannot cancel an appointment with status: {Status}.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required.");

        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newScheduledAt, int newDurationMinutes)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new DomainException($"Cannot reschedule an appointment with status: {Status}.");
        if (newScheduledAt <= DateTime.UtcNow)
            throw new DomainException("Rescheduled time must be in the future.");
        if (newDurationMinutes <= 0 || newDurationMinutes > 480)
            throw new DomainException("Duration must be between 1 and 480 minutes.");

        ScheduledAt = newScheduledAt;
        DurationMinutes = newDurationMinutes;
        Status = AppointmentStatus.Scheduled;
        UpdatedAt = DateTime.UtcNow;
    }
}
