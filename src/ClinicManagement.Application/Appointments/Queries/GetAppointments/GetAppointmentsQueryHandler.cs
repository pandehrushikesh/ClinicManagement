using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Patients.Queries.GetPatients;

namespace ClinicManagement.Application.Appointments.Queries.GetAppointments;

public class GetAppointmentsQueryHandler
{
    private readonly IAppointmentRepository _appointments;

    public GetAppointmentsQueryHandler(IAppointmentRepository appointments)
    {
        _appointments = appointments;
    }

    public async Task<PagedResult<AppointmentDto>> Handle(GetAppointmentsQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _appointments.GetPagedAsync(query.AfterId, pageSize, query.PatientId, query.DoctorId, query.Status, cancellationToken);
        return new PagedResult<AppointmentDto>(items.Select(a => a.ToDto()).ToList(), total);
    }
}
