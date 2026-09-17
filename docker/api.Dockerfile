# syntax=docker/dockerfile:1

# =================================================================================================
# WorkBoard API
#
# Multi-stage, so the shipped image contains the runtime and the published output and nothing else —
# no SDK, no sources, no NuGet cache. The build stage is roughly 900 MB; the result is around 130 MB.
# =================================================================================================

# ------------------------------------------------------------------------------------------ build
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# The project files are copied first, on their own.
#
# This is the one Dockerfile trick that genuinely earns its place: `restore` depends only on these
# files, so its layer is cached and re-used for every build where no package reference changed. If
# the whole source tree were copied first, editing one line of C# would invalidate the cache and
# re-download every package.
#
# .editorconfig is copied with them, and it is not decoration: it carries the analyzer exemptions for
# the generated migrations. Without it the container build fails on CA1861 inside EF-generated code
# while the same `dotnet build` succeeds on a developer machine — a difference that is invisible
# until the image build, because nothing else here depends on it.
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig ./
COPY src/WorkBoard.Domain/WorkBoard.Domain.csproj src/WorkBoard.Domain/
COPY src/WorkBoard.Application/WorkBoard.Application.csproj src/WorkBoard.Application/
COPY src/WorkBoard.Infrastructure/WorkBoard.Infrastructure.csproj src/WorkBoard.Infrastructure/
COPY src/WorkBoard.Api/WorkBoard.Api.csproj src/WorkBoard.Api/

RUN dotnet restore src/WorkBoard.Api/WorkBoard.Api.csproj

COPY src/ src/

# --no-restore because the layer above already did it; without the flag this would silently repeat
# the work the cache was arranged to avoid.
RUN dotnet publish src/WorkBoard.Api/WorkBoard.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------------------- runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

# curl is installed explicitly, as root, before dropping privileges. The ASP.NET runtime image does
# not ship it, and the HEALTHCHECK below needs something that speaks HTTP. This is the reason the
# image is not the chiselled variant: a container that reports "running" while unable to reach the
# database is worse than the ~2 MB this costs.
USER root
RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

# Non-root from here on. The base image ships an `app` user for exactly this; running as root means
# a container escape starts with root in the host namespace.
USER app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    # Suppress the telemetry notice on first run, which otherwise pollutes the first log line.
    DOTNET_NOLOGO=1

EXPOSE 8080

COPY --from=build --chown=app:app /app/publish .

# Hits the real health endpoint, which checks the database connection — so an API that started but
# cannot reach SQL Server is reported unhealthy rather than "running".
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
    CMD ["/usr/bin/curl", "--fail", "--silent", "http://localhost:8080/health"]

ENTRYPOINT ["dotnet", "WorkBoard.Api.dll"]
