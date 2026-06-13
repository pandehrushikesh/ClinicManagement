namespace ClinicManagement.Application.Common.DTOs;

public record DoctorDto(int Id, string FullName, string Specialty, bool IsActive);
