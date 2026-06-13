using ClinicManagement.Application.Common.DTOs;
using ClinicManagement.Application.Patients.Commands.CreatePatient;
using ClinicManagement.Application.Patients.Queries.GetPatientById;
using ClinicManagement.Application.Patients.Queries.GetPatients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly CreatePatientCommandHandler _createHandler;
    private readonly GetPatientByIdQueryHandler _getByIdHandler;
    private readonly GetPatientsQueryHandler _getPatientsHandler;

    public PatientsController(
        CreatePatientCommandHandler createHandler,
        GetPatientByIdQueryHandler getByIdHandler,
        GetPatientsQueryHandler getPatientsHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _getPatientsHandler = getPatientsHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int afterId = 0, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _getPatientsHandler.Handle(new GetPatientsQuery(afterId, pageSize), ct);
        return Ok(new { success = true, data = result, error = (string?)null });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var patient = await _getByIdHandler.Handle(new GetPatientByIdQuery(id), ct);
        return Ok(new { success = true, data = patient, error = (string?)null });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientCommand command, CancellationToken ct = default)
    {
        var patient = await _createHandler.Handle(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, new { success = true, data = patient, error = (string?)null });
    }
}
