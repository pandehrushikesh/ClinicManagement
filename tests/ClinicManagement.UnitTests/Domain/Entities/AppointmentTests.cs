using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;
using FluentAssertions;

namespace ClinicManagement.UnitTests.Domain.Entities;

public class AppointmentTests
{
    private static Appointment Scheduled() =>
        Appointment.Schedule(1, 2, DateTime.UtcNow.AddDays(1), 30);

    private static Appointment Confirmed()
    {
        var a = Scheduled();
        a.Confirm();
        return a;
    }

    // ── Schedule ─────────────────────────────────────────────────────────────

    [Fact]
    public void Schedule_ValidData_ReturnsScheduledAppointment()
    {
        var at = DateTime.UtcNow.AddDays(1);
        var appointment = Appointment.Schedule(1, 2, at, 30, "Follow-up");

        appointment.PatientId.Should().Be(1);
        appointment.DoctorId.Should().Be(2);
        appointment.ScheduledAt.Should().Be(at);
        appointment.DurationMinutes.Should().Be(30);
        appointment.Notes.Should().Be("Follow-up");
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Schedule_PastTime_ThrowsDomainException()
    {
        var act = () => Appointment.Schedule(1, 2, DateTime.UtcNow.AddMinutes(-1), 30);
        act.Should().Throw<DomainException>().WithMessage("*future*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(481)]
    public void Schedule_InvalidDuration_ThrowsDomainException(int duration)
    {
        var act = () => Appointment.Schedule(1, 2, DateTime.UtcNow.AddDays(1), duration);
        act.Should().Throw<DomainException>().WithMessage("*Duration*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Schedule_InvalidPatientOrDoctor_ThrowsDomainException(int id)
    {
        var act = () => Appointment.Schedule(id, 2, DateTime.UtcNow.AddDays(1), 30);
        act.Should().Throw<DomainException>();
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_WhenScheduled_MovesToConfirmed()
    {
        var appointment = Scheduled();
        appointment.Confirm();
        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        appointment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ThrowsDomainException()
    {
        var appointment = Confirmed();
        var act = () => appointment.Confirm();
        act.Should().Throw<DomainException>().WithMessage("*Confirmed*");
    }

    [Fact]
    public void Confirm_WhenCancelled_ThrowsDomainException()
    {
        var appointment = Scheduled();
        appointment.Cancel("No longer needed");
        var act = () => appointment.Confirm();
        act.Should().Throw<DomainException>();
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_WhenConfirmed_MovesToCompleted()
    {
        var appointment = Confirmed();
        appointment.Complete();
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WhenScheduled_ThrowsDomainException()
    {
        var appointment = Scheduled();
        var act = () => appointment.Complete();
        act.Should().Throw<DomainException>().WithMessage("*Confirmed*");
    }

    [Fact]
    public void Complete_WhenCancelled_ThrowsDomainException()
    {
        var appointment = Scheduled();
        appointment.Cancel("Test");
        var act = () => appointment.Complete();
        act.Should().Throw<DomainException>();
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenScheduled_MovesToCancelled()
    {
        var appointment = Scheduled();
        appointment.Cancel("Patient request");
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancellationReason.Should().Be("Patient request");
        appointment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenConfirmed_MovesToCancelled()
    {
        var appointment = Confirmed();
        appointment.Cancel("Doctor unavailable");
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsDomainException()
    {
        var appointment = Confirmed();
        appointment.Complete();
        var act = () => appointment.Cancel("Too late");
        act.Should().Throw<DomainException>().WithMessage("*Completed*");
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsDomainException()
    {
        var appointment = Scheduled();
        appointment.Cancel("First reason");
        var act = () => appointment.Cancel("Second reason");
        act.Should().Throw<DomainException>().WithMessage("*Cancelled*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cancel_EmptyReason_ThrowsDomainException(string? reason)
    {
        var appointment = Scheduled();
        var act = () => appointment.Cancel(reason!);
        act.Should().Throw<DomainException>().WithMessage("*reason*");
    }

    // ── Reschedule ────────────────────────────────────────────────────────────

    [Fact]
    public void Reschedule_WhenScheduled_UpdatesTimeAndResetsToScheduled()
    {
        var appointment = Scheduled();
        var newTime = DateTime.UtcNow.AddDays(3);
        appointment.Reschedule(newTime, 60);

        appointment.ScheduledAt.Should().Be(newTime);
        appointment.DurationMinutes.Should().Be(60);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reschedule_WhenConfirmed_ResetsToScheduled()
    {
        var appointment = Confirmed();
        appointment.Reschedule(DateTime.UtcNow.AddDays(2), 45);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
    }

    [Fact]
    public void Reschedule_WhenCompleted_ThrowsDomainException()
    {
        var appointment = Confirmed();
        appointment.Complete();
        var act = () => appointment.Reschedule(DateTime.UtcNow.AddDays(1), 30);
        act.Should().Throw<DomainException>().WithMessage("*Completed*");
    }

    [Fact]
    public void Reschedule_WhenCancelled_ThrowsDomainException()
    {
        var appointment = Scheduled();
        appointment.Cancel("Cancelled");
        var act = () => appointment.Reschedule(DateTime.UtcNow.AddDays(1), 30);
        act.Should().Throw<DomainException>().WithMessage("*Cancelled*");
    }

    [Fact]
    public void Reschedule_PastTime_ThrowsDomainException()
    {
        var appointment = Scheduled();
        var act = () => appointment.Reschedule(DateTime.UtcNow.AddMinutes(-1), 30);
        act.Should().Throw<DomainException>().WithMessage("*future*");
    }
}
