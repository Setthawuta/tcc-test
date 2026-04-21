# syntax=docker/dockerfile:1.7

# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AuthSystem.sln ./
COPY src/AuthSystem.Domain/AuthSystem.Domain.csproj src/AuthSystem.Domain/
COPY src/AuthSystem.Application/AuthSystem.Application.csproj src/AuthSystem.Application/
COPY src/AuthSystem.Infrastructure/AuthSystem.Infrastructure.csproj src/AuthSystem.Infrastructure/
COPY src/AuthSystem.Api/AuthSystem.Api.csproj src/AuthSystem.Api/

RUN dotnet restore src/AuthSystem.Api/AuthSystem.Api.csproj

COPY . .

RUN dotnet publish src/AuthSystem.Api/AuthSystem.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# -------- Runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthSystem.Api.dll"]
