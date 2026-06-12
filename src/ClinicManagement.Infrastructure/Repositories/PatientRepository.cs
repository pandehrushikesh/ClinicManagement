using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;

    public PatientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(
        int afterId, int pageSize, CancellationToken cancellationToken = default)
    {
        var total = await _context.Patients.CountAsync(cancellationToken);
        var items = await _context.Patients
            .Where(p => p.Id > afterId)
            .OrderBy(p => p.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
        => await _context.Patients.AddAsync(patient, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
