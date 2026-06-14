using System.Data;
using System.Text;
using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Enums;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Infrastructure.Persistence;

namespace ClinicManagement.Infrastructure.Repositories;

public class AppointmentDapperReadRepository : IAppointmentReadRepository
{
    private readonly AppDbContext _context;

    public AppointmentDapperReadRepository(AppDbContext context) => _context = context;

    private IDbConnection Connection => new SqlConnection(_context.Database.GetConnectionString());

    public async Task<IReadOnlyList<AppointmentDto>> GetPagedAsync(
        int afterId, int pageSize,
        int? patientId = null, int? doctorId = null, AppointmentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder("""
            SELECT a.Id, a.PatientId,
                   p.FirstName + ' ' + p.LastName AS PatientFullName,
                   a.DoctorId,
                   d.FirstName + ' ' + d.LastName AS DoctorFullName,
                   a.ScheduledAt, a.DurationMinutes, a.Status, a.Notes, a.CancellationReason, a.CreatedAt
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Doctors d ON d.Id = a.DoctorId
            WHERE a.Id > @AfterID
            """);

        if (patientId.HasValue) sql.Append(" AND a.PatientId = @PatientId");
        if (doctorId.HasValue) sql.Append(" AND a.DoctorId = @DoctorId");
        if (status.HasValue) sql.Append(" AND a.Status = @Status");

        sql.Append(" ORDER BY a.Id OFFSET 0 ROWS FETCH NEXT @PageSize ROWS ONLY");

        using var conn = Connection;
        var cmd = new CommandDefinition(sql.ToString(),
            new { AfterID = afterId, PageSize = pageSize, PatientId = patientId, DoctorId = doctorId, Status = status },
            cancellationToken: cancellationToken);

        var results = await conn.QueryAsync<AppointmentDto>(cmd);
        return results.ToList();
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT a.Id, a.PatientId,
                   p.FirstName + ' ' + p.LastName AS PatientFullName,
                   a.DoctorId,
                   d.FirstName + ' ' + d.LastName AS DoctorFullName,
                   a.ScheduledAt, a.DurationMinutes, a.Status, a.Notes, a.CancellationReason, a.CreatedAt
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Doctors d ON d.Id = a.DoctorId
            WHERE a.Id = @Id
            """;

        using var conn = Connection;
        var cmd = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<AppointmentDto>(cmd);
    }
}
