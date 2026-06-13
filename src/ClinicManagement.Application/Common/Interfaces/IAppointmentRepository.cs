using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Common.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(int afterId, int pageSize, int? patientId, int? doctorId, AppointmentStatus? status, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(int doctorId, DateTime scheduledAt, int durationMinutes, int? excludeAppointmentId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
