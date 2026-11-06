# Container packaging for the WebAPI project of this template.
#
# Two stages:
#   build   - .NET 10 SDK, restores and publishes the API only
#   runtime - ASP.NET Core 10 runtime image, carries the published output
#
# Deliberately missing, on purpose:
#   * No second publish target for auxiliary projects. Only WebAPI/WebAPI.csproj
#     is restored and published, which is also why UnitTests is not part of the
#     image: a container ships the application, not its test suite.
#   * No cloudflared or any other vendor tunnel glued into the image. The
#     reference design this template was modeled on installed a Cloudflare
#     tunnel inside its runtime stage and passed a tunnel service token on the
#     command line - a credential baked into an image tag. Connectivity and
#     secrets belong to the deployment (orchestrator, secret store,
#     sidecar), never to the application image.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, sources after: the restore layer only has to be rebuilt
# when a dependency graph changes, not on every source edit.
COPY ["Models/Models.csproj", "Models/"]
COPY ["Repository/Repository.csproj", "Repository/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["BusinessLogic/BusinessLogic.csproj", "BusinessLogic/"]
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]

RUN dotnet restore "WebAPI/WebAPI.csproj"

COPY Models/ Models/
COPY Repository/ Repository/
COPY Services/ Services/
COPY BusinessLogic/ BusinessLogic/
COPY WebAPI/ WebAPI/

# UseAppHost=false: no native apphost executable, just the managed assembly,
# which is what the ASP.NET Core runtime image is meant to run.
RUN dotnet publish "WebAPI/WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The runtime image already provides an unprivileged account (uid 1000).
USER $APP_UID

COPY --from=build /app/publish .

# Bind to all interfaces on 8080 so a container orchestrator can reach it.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Configuration stays outside the image. The database is picked up from DATABASE_URL when
# present (Host=...;Port=...;Database=... style, or a postgres:// URL) and otherwise from the
# Postgres section of appsettings.json; the JWT signing secret, mail credentials and telemetry
# endpoints live in appsettings or a secret store. A deployment overrides them - the committed
# values in this template are placeholders that say so out loud.

ENTRYPOINT ["dotnet", "WebAPI.dll"]
