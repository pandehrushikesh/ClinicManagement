using System.Text;
using ClinicManagement.API.Middleware;
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
using ClinicManagement.Infrastructure.Caching;
using ClinicManagement.Infrastructure.Messaging;
using ClinicManagement.Infrastructure.Persistence;
using ClinicManagement.Infrastructure.Repositories;
using ClinicManagement.Shared;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT validation — tokens are ISSUED by AuthService, only VALIDATED here
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization();

// Messaging — publish-only, no consumers in this service
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var user = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var pass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });

        cfg.ConfigureEndpoints(ctx);
    });
});
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

// Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Repositories
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IPatientReadRepository, PatientDapperReadRepository>();
builder.Services.AddScoped<IAppointmentReadRepository, AppointmentDapperReadRepository>();

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

// Auto-migrate on startup so Docker containers self-initialize
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
