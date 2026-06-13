using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorRepository _doctors;

    public DoctorsController(IDoctorRepository doctors) => _doctors = doctors;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDoctorRequest request, CancellationToken ct = default)
    {
        var doctor = Doctor.Create(request.FirstName, request.LastName, request.Specialty);
        await _doctors.AddAsync(doctor, ct);
        await _doctors.SaveChangesAsync(ct);

        var dto = new DoctorDto(doctor.Id, doctor.FullName, doctor.Specialty, doctor.IsActive);
        return CreatedAtAction(nameof(Create), new { id = doctor.Id }, new { success = true, data = dto, error = (string?)null });
    }
}

public record CreateDoctorRequest(string FirstName, string LastName, string Specialty);
