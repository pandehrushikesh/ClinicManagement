using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;

namespace ClinicManagement.Application.Patients.Queries.GetPatients;

public record PagedResult<T>(IReadOnlyList<T> Items);

public class GetPatientsQueryHandler
{
    private readonly IPatientReadRepository _readRepository;
    private readonly ICacheService _cache;

    public GetPatientsQueryHandler(IPatientReadRepository readRepository, ICacheService cache)
    {
        _readRepository = readRepository;
        _cache = cache;
    }

    public async Task<PagedResult<PatientDto>> Handle(GetPatientsQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var cacheKey = $"patients:page:after={query.AfterId}:size={pageSize}";

        var cached = await _cache.GetAsync<List<PatientDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return new PagedResult<PatientDto>(cached);

        var items = await _readRepository.GetPagedAsync(query.AfterId, pageSize, cancellationToken);
        await _cache.SetAsync(cacheKey, items.ToList(), TimeSpan.FromMinutes(2), cancellationToken);

        return new PagedResult<PatientDto>(items);
    }
}
