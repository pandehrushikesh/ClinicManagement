using ClinicManagement.Shared.Events;
using MassTransit;

namespace ClinicManagement.NotificationService.Consumers;

public class AppointmentScheduledConsumer : IConsumer<AppointmentScheduledEvent>
{
    private readonly ILogger<AppointmentScheduledConsumer> _logger;

    public AppointmentScheduledConsumer(ILogger<AppointmentScheduledConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<AppointmentScheduledEvent> context)
    {
        var e = context.Message;

        // Stub — replace with real email/SMS in Phase 5+
        _logger.LogInformation(
            "[NOTIFICATION] Appointment #{AppointmentId} scheduled. " +
            "Patient: {PatientName} | Doctor: {DoctorName} | At: {ScheduledAt:f} | Duration: {Duration} min",
            e.AppointmentId,
            e.PatientFullName,
            e.DoctorFullName,
            e.ScheduledAt,
            e.DurationMinutes);

        return Task.CompletedTask;
    }
}
