using ClinicManagement.API.Middleware;
using ClinicManagement.Application.Patients.Commands.CreatePatient;
using ClinicManagement.Application.Patients.Queries.GetPatientById;
using ClinicManagement.Application.Patients.Queries.GetPatients;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Infrastructure.Persistence;
using ClinicManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<CreatePatientCommandHandler>();
builder.Services.AddScoped<GetPatientByIdQueryHandler>();
builder.Services.AddScoped<GetPatientsQueryHandler>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
