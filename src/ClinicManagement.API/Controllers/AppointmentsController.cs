using ClinicManagement.Application.Appointments.Commands.CancelAppointment;
using ClinicManagement.Application.Appointments.Commands.CompleteAppointment;
using ClinicManagement.Application.Appointments.Commands.ConfirmAppointment;
using ClinicManagement.Application.Appointments.Commands.RescheduleAppointment;
using ClinicManagement.Application.Appointments.Commands.ScheduleAppointment;
using ClinicManagement.Application.Appointments.Queries.GetAppointmentById;
using ClinicManagement.Application.Appointments.Queries.GetAppointments;
using ClinicManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ScheduleAppointmentCommandHandler _scheduleHandler;
    private readonly ConfirmAppointmentCommandHandler _confirmHandler;
    private readonly CancelAppointmentCommandHandler _cancelHandler;
    private readonly CompleteAppointmentCommandHandler _completeHandler;
    private readonly RescheduleAppointmentCommandHandler _rescheduleHandler;
    private readonly GetAppointmentByIdQueryHandler _getByIdHandler;
    private readonly GetAppointmentsQueryHandler _getAppointmentsHandler;

    public AppointmentsController(
        ScheduleAppointmentCommandHandler scheduleHandler,
        ConfirmAppointmentCommandHandler confirmHandler,
        CancelAppointmentCommandHandler cancelHandler,
        CompleteAppointmentCommandHandler completeHandler,
        RescheduleAppointmentCommandHandler rescheduleHandler,
        GetAppointmentByIdQueryHandler getByIdHandler,
        GetAppointmentsQueryHandler getAppointmentsHandler)
    {
        _scheduleHandler = scheduleHandler;
        _confirmHandler = confirmHandler;
        _cancelHandler = cancelHandler;
        _completeHandler = completeHandler;
        _rescheduleHandler = rescheduleHandler;
        _getByIdHandler = getByIdHandler;
        _getAppointmentsHandler = getAppointmentsHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int afterId = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? patientId = null,
        [FromQuery] int? doctorId = null,
        [FromQuery] AppointmentStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _getAppointmentsHandler.Handle(new GetAppointmentsQuery(afterId, pageSize, patientId, doctorId, status), ct);
        return Ok(new { success = true, data = result, error = (string?)null });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var result = await _getByIdHandler.Handle(new GetAppointmentByIdQuery(id), ct);
        return Ok(new { success = true, data = result, error = (string?)null });
    }

    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] ScheduleAppointmentCommand command, CancellationToken ct = default)
    {
        var result = await _scheduleHandler.Handle(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { success = true, data = result, error = (string?)null });
    }

    [HttpPut("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct = default)
    {
        var result = await _confirmHandler.Handle(new ConfirmAppointmentCommand(id), ct);
        return Ok(new { success = true, data = result, error = (string?)null });
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct = default)
    {
        var result = await _completeHandler.Handle(new CompleteAppointmentCommand(id), ct);
        return Ok(new { success = true, data = result, error = (string?)null });
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelAppointmentCommand command, CancellationToken ct = default)
    {
        var result = await _cancelHandler.Handle(command with { AppointmentId = id }, ct);
        return Ok(new { success = true, data = result, error = (string?)null });
    }

    [HttpPut("{id:int}/reschedule")]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleAppointmentCommand command, CancellationToken ct = default)
    {
        var result = await _rescheduleHandler.Handle(command with { AppointmentId = id }, ct);
        return Ok(new { success = true, data = result, error = (string?)null });
    }
}
