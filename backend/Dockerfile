# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached unless a .csproj changes.
COPY src/VetPlatform.Domain/VetPlatform.Domain.csproj src/VetPlatform.Domain/
COPY src/VetPlatform.Application/VetPlatform.Application.csproj src/VetPlatform.Application/
COPY src/VetPlatform.Infrastructure/VetPlatform.Infrastructure.csproj src/VetPlatform.Infrastructure/
COPY src/VetPlatform.Api/VetPlatform.Api.csproj src/VetPlatform.Api/
RUN dotnet restore src/VetPlatform.Api/VetPlatform.Api.csproj

COPY src/ src/
RUN dotnet publish src/VetPlatform.Api/VetPlatform.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# curl only for docker-compose's healthcheck to call GET /health
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VetPlatform.Api.dll"]
