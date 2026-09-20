# Sohba MVC application image.
#
# Multi-stage build: the SDK stage publishes the app, the runtime stage ships only
# the published output plus curl, which the container health probe uses against
# the /healthz endpoint mapped in Program.cs.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore before copying the rest of the sources so the NuGet layer is reused
# whenever only application code changes.
COPY Sohba.Domain/Sohba.Domain.csproj Sohba.Domain/
COPY Sohba.Application/Sohba.Application.csproj Sohba.Application/
COPY Sohba.Infrastructure/Sohba.Infrastructure.csproj Sohba.Infrastructure/
COPY Sohba/Sohba.csproj Sohba/
RUN dotnet restore Sohba/Sohba.csproj

COPY . .
RUN dotnet publish Sohba/Sohba.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

# Runtime-writable locations: uploaded media (wwwroot/uploads), protected story media,
# the Serilog file sink (logs/sohba-*.log, relative to the content root) and the Data
# Protection key ring under the app user's home. docker-compose.yml mounts a named volume
# on each, so the directories must exist and be owned by the app user; Docker copies that
# ownership into the volume the first time it is initialised.
RUN mkdir -p /app/wwwroot/uploads /app/ProtectedUploads /app/logs /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID /app /home/app

USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=45s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Sohba.dll"]