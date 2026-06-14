using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Common.Interfaces;

public interface IAppointmentReadRepository
{
    Task<IReadOnlyList<AppointmentDto>> GetPagedAsync(int afterId, int pageSize,
        int? patientId = null, int? doctorId = null, AppointmentStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
