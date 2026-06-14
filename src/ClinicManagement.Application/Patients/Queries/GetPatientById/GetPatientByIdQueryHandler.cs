using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Patients.Queries.GetPatientById;

public class GetPatientByIdQueryHandler
{
    private readonly IPatientReadRepository _readRepository;
    private readonly ICacheService _cache;

    public GetPatientByIdQueryHandler(IPatientReadRepository readRepository, ICacheService cache)
    {
        _readRepository = readRepository;
        _cache = cache;
    }

    public async Task<PatientDto> Handle(GetPatientByIdQuery query, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"patients:{query.Id}";

        var cached = await _cache.GetAsync<PatientDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var patient = await _readRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), query.Id);

        await _cache.SetAsync(cacheKey, patient, TimeSpan.FromMinutes(5), cancellationToken);
        return patient;
    }
}
