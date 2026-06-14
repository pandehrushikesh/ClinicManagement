using System.Data;
using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Infrastructure.Persistence;

namespace ClinicManagement.Infrastructure.Repositories;

public class PatientDapperReadRepository : IPatientReadRepository
{
    private readonly AppDbContext _context;

    public PatientDapperReadRepository(AppDbContext context) => _context = context;

    private IDbConnection Connection => new SqlConnection(_context.Database.GetConnectionString());

    public async Task<IReadOnlyList<PatientDto>> GetPagedAsync(int afterId, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, FirstName, LastName,
                   FirstName + ' ' + LastName AS FullName,
                   DateOfBirth, Email, Phone, IsActive, CreatedAt
            FROM Patients
            WHERE Id > @AfterID
            ORDER BY Id
            OFFSET 0 ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        using var conn = Connection;
        var cmd = new CommandDefinition(sql, new { AfterID = afterId, PageSize = pageSize },
            cancellationToken: cancellationToken);

        var results = await conn.QueryAsync<PatientDto>(cmd);
        return results.ToList();
    }

    public async Task<PatientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, FirstName, LastName,
                   FirstName + ' ' + LastName AS FullName,
                   DateOfBirth, Email, Phone, IsActive, CreatedAt
            FROM Patients
            WHERE Id = @Id
            """;

        using var conn = Connection;
        var cmd = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<PatientDto>(cmd);
    }
}
