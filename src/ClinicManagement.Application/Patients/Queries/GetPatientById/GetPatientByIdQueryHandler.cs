using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Patients.Queries.GetPatientById;

public class GetPatientByIdQueryHandler
{
    private readonly IPatientRepository _repository;

    public GetPatientByIdQueryHandler(IPatientRepository repository)
    {
        _repository = repository;
    }

    public async Task<PatientDto> Handle(GetPatientByIdQuery query, CancellationToken cancellationToken = default)
    {
        var patient = await _repository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), query.Id);

        return patient.ToDto();
    }
}
