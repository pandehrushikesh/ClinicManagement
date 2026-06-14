using ClinicManagement.Application.Common.DTOs;

namespace ClinicManagement.Application.Common.Interfaces;

public interface IPatientReadRepository
{
    Task<IReadOnlyList<PatientDto>> GetPagedAsync(int afterId, int pageSize, CancellationToken cancellationToken = default);
    Task<PatientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
