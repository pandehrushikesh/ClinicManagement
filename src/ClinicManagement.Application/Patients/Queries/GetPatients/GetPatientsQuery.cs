namespace ClinicManagement.Application.Patients.Queries.GetPatients;

public record GetPatientsQuery(int AfterId = 0, int PageSize = 20);
