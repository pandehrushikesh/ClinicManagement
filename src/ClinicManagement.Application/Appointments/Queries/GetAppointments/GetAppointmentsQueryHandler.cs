using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Patients.Queries.GetPatients;

namespace ClinicManagement.Application.Appointments.Queries.GetAppointments;

public class GetAppointmentsQueryHandler
{
    private readonly IAppointmentReadRepository _readRepository;
    private readonly ICacheService _cache;

    public GetAppointmentsQueryHandler(IAppointmentReadRepository readRepository, ICacheService cache)
    {
        _readRepository = readRepository;
        _cache = cache;
    }

    public async Task<PagedResult<AppointmentDto>> Handle(GetAppointmentsQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var cacheKey = $"appointments:page:after={query.AfterId}:size={pageSize}:p={query.PatientId}:d={query.DoctorId}:s={query.Status}";

        var cached = await _cache.GetAsync<List<AppointmentDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return new PagedResult<AppointmentDto>(cached);

        var items = await _readRepository.GetPagedAsync(query.AfterId, pageSize, query.PatientId, query.DoctorId, query.Status, cancellationToken);
        await _cache.SetAsync(cacheKey, items.ToList(), TimeSpan.FromMinutes(2), cancellationToken);

        return new PagedResult<AppointmentDto>(items);
    }
}
