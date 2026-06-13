using ClinicManagement.API.Middleware;
using Scalar.AspNetCore;
using ClinicManagement.Application.Appointments.Commands.CancelAppointment;
using ClinicManagement.Application.Appointments.Commands.CompleteAppointment;
using ClinicManagement.Application.Appointments.Commands.ConfirmAppointment;
using ClinicManagement.Application.Appointments.Commands.RescheduleAppointment;
using ClinicManagement.Application.Appointments.Commands.ScheduleAppointment;
using ClinicManagement.Application.Appointments.Queries.GetAppointmentById;
using ClinicManagement.Application.Appointments.Queries.GetAppointments;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Patients.Commands.CreatePatient;
using ClinicManagement.Application.Patients.Queries.GetPatientById;
using ClinicManagement.Application.Patients.Queries.GetPatients;
using ClinicManagement.Infrastructure.Persistence;
using ClinicManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// Patient handlers
builder.Services.AddScoped<CreatePatientCommandHandler>();
builder.Services.AddScoped<GetPatientByIdQueryHandler>();
builder.Services.AddScoped<GetPatientsQueryHandler>();

// Appointment handlers
builder.Services.AddScoped<ScheduleAppointmentCommandHandler>();
builder.Services.AddScoped<ConfirmAppointmentCommandHandler>();
builder.Services.AddScoped<CancelAppointmentCommandHandler>();
builder.Services.AddScoped<CompleteAppointmentCommandHandler>();
builder.Services.AddScoped<RescheduleAppointmentCommandHandler>();
builder.Services.AddScoped<GetAppointmentByIdQueryHandler>();
builder.Services.AddScoped<GetAppointmentsQueryHandler>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Clinic Management API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
