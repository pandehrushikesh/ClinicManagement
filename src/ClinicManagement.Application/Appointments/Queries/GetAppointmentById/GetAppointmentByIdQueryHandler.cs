using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Queries.GetAppointmentById;

public class GetAppointmentByIdQueryHandler
{
    private readonly IAppointmentReadRepository _readRepository;
    private readonly ICacheService _cache;

    public GetAppointmentByIdQueryHandler(IAppointmentReadRepository readRepository, ICacheService cache)
    {
        _readRepository = readRepository;
        _cache = cache;
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery query, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"appointments:{query.Id}";

        var cached = await _cache.GetAsync<AppointmentDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var appointment = await _readRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), query.Id);

        await _cache.SetAsync(cacheKey, appointment, TimeSpan.FromMinutes(5), cancellationToken);
        return appointment;
    }
}
