using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _context;

    public DoctorRepository(AppDbContext context) => _context = context;

    public async Task<Doctor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
        => await _context.Doctors.AddAsync(doctor, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
