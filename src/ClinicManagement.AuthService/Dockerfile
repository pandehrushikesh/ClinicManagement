FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY src/ClinicManagement.Shared/ClinicManagement.Shared.csproj src/ClinicManagement.Shared/
COPY src/ClinicManagement.AuthService/ClinicManagement.AuthService.csproj src/ClinicManagement.AuthService/
COPY src/ClinicManagement.Domain/ClinicManagement.Domain.csproj src/ClinicManagement.Domain/
COPY src/ClinicManagement.Application/ClinicManagement.Application.csproj src/ClinicManagement.Application/
COPY src/ClinicManagement.Infrastructure/ClinicManagement.Infrastructure.csproj src/ClinicManagement.Infrastructure/
COPY src/ClinicManagement.NotificationService/ClinicManagement.NotificationService.csproj src/ClinicManagement.NotificationService/
COPY src/ClinicManagement.Gateway/ClinicManagement.Gateway.csproj src/ClinicManagement.Gateway/
COPY src/ClinicManagement.API/ClinicManagement.API.csproj src/ClinicManagement.API/
RUN dotnet restore src/ClinicManagement.AuthService/ClinicManagement.AuthService.csproj

COPY src/ src/

RUN dotnet publish src/ClinicManagement.AuthService/ClinicManagement.AuthService.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClinicManagement.AuthService.dll"]
