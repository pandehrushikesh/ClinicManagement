using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;

namespace ClinicManagement.Application.Patients.Queries.GetPatients;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);

public class GetPatientsQueryHandler
{
    private readonly IPatientRepository _repository;

    public GetPatientsQueryHandler(IPatientRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<PatientDto>> Handle(GetPatientsQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(query.AfterId, pageSize, cancellationToken);
        return new PagedResult<PatientDto>(items.Select(p => p.ToDto()).ToList(), total);
    }
}
