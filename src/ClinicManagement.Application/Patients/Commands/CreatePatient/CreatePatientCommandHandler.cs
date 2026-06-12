using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.ValueObjects;

namespace ClinicManagement.Application.Patients.Commands.CreatePatient;

public class CreatePatientCommandHandler
{
    private readonly IPatientRepository _repository;

    public CreatePatientCommandHandler(IPatientRepository repository)
    {
        _repository = repository;
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

        return patient.ToDto();
    }
}
