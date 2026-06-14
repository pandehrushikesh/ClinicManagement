using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.ValueObjects;

namespace ClinicManagement.Application.Patients.Commands.CreatePatient;

public class CreatePatientCommandHandler
{
    private readonly IPatientRepository _repository;
    private readonly ICacheService _cache;

    public CreatePatientCommandHandler(IPatientRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<PatientDto> Handle(CreatePatientCommand command, CancellationToken cancellationToken = default)
    {
        var patient = Patient.Create(
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            new Email(command.Email),
            new PhoneNumber(command.Phone));

        await _repository.AddAsync(patient, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync("patients:", cancellationToken);

        return patient.ToDto();
    }
}
