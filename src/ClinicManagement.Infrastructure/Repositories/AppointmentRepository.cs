using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;

    public AppointmentRepository(AppDbContext context) => _context = context;

    public async Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
        int afterId, int pageSize, int? patientId, int? doctorId, AppointmentStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsQueryable();

        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId.Value);
        if (doctorId.HasValue) query = query.Where(a => a.DoctorId == doctorId.Value);
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Where(a => a.Id > afterId)
            .OrderBy(a => a.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> HasConflictAsync(
        int doctorId, DateTime scheduledAt, int durationMinutes,
        int? excludeAppointmentId = null, CancellationToken cancellationToken = default)
    {
        var end = scheduledAt.AddMinutes(durationMinutes);

        return await _context.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Id != (excludeAppointmentId ?? -1) &&
            a.Status != AppointmentStatus.Cancelled &&
            a.ScheduledAt < end &&
            a.ScheduledAt.AddMinutes(a.DurationMinutes) > scheduledAt,
            cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => await _context.Appointments.AddAsync(appointment, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
