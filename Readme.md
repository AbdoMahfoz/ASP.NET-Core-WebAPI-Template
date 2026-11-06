# ASP.NET Core Web API template

A jump-start template for an ASP.NET Core Web API on .NET 10: JWT authentication,
a typed exception contract with middleware that maps it to statuses, a generic
roles-and-permissions system, a tenant-scoped repository layer with paging, a
self-refreshing cache that can run on Redis, OpenTelemetry tracing with
source-generated request logging, Swagger documentation, an email service, and a
test project that boots the real pipeline through `WebApplicationFactory`.

## What is in the solution

| Project | Role |
| --- | --- |
| `Models` | EF Core entities, `ApplicationDbContext`, migrations, `AppSettings` |
| `Repository` | `IRepository<T>` / `Repository<T>` (tenant scoped, span named `Repository<Model>::Method`), role repositories |
| `Services` | DTOs, validation via DataAnnotations + `IValidatableObject`, typed exceptions and `FailureReasons`, paging helpers |
| `BusinessLogic` | Accounts and sessions (`IAccountLogic`, `IAuth`), roles and permissions manager, cache (`ICache`, `MemoryCache`, `RedisCache`) |
| `WebAPI` | Controllers, filters, middleware chain, Swagger, OpenTelemetry wiring |
| `UnitTests` | xunit suite plus the `WebApplicationFactory` harness in `UnitTests/Base.cs` |

Nothing here is scoped to a business domain: there is no site concept anywhere in
the template. Rows are filtered by the tenant each request resolves.

## Getting started

```bash
dotnet restore WebAPI.sln
dotnet build WebAPI.sln                       # net10.0 across all six projects
dotnet run --project WebAPI/WebAPI.csproj     # Swagger at /swagger
```

Configuration lives in `WebAPI/appsettings.json`. Every credential in it is a
placeholder - point them at your own database, mail box and secret store (or a
secret manager) before running anything real. Notable sections:

- `AppSettings:Secret` - JWT signing key. HS256 needs at least 32 bytes. Keep it out of the repository. The committed value is a placeholder that says so.
- `AppSettings:AccountLockThreshold` - failed sign-ins allowed before an account is locked (default 5). A value of zero or less falls back to 5 so lockout cannot be disabled by accident.
- `AppSettings:Redis` - set `Use: true` to swap the in-process cache for the Redis backend (`StackExchange.Redis`). The connection is created lazily by a factory registration, so nothing dials a server until the cache is actually used. A test run never needs a live Redis.
- `AppSettings:Observability` - tracing and logging knobs: service name/version stamped on spans, whether to export over OTLP (`OtlpEndpoint`) with the console exporter as fallback, which forwarded headers may be honoured, which CIDR ranges may assert a client address (`TrustedProxyCidrs`), which headers carry that address (`ClientIpHeaders`), and whether request or response bodies may be logged at all - off by default here because bodies carry passwords and access tokens.

## Branches, tests and CI

The template's flow is two long-lived branches plus short-lived feature branches:

1. Work on a branch off `master`, open a pull request into it.
2. `.github/workflows/pr_tester.yml` runs on every pull request into `master`: restore, build on .NET 10 (`setup-dotnet` with `10.0.x`) on `ubuntu-latest`, then run the suite with coverage collected by coverlet, converted by ReportGenerator into a summary plus an HTML report uploaded as a build artifact.
3. When the pull request merges into `master`, `.github/workflows/cd.yml` merges `master` into the `deploy` branch and pushes it - the same thing the old one-line `deployScript` did by hand, but authorized by the workflow's own short-lived token instead of a credential committed in a script file. If you prefer to do it locally, the sequence is:

```bash
git checkout master && git pull origin master
git checkout deploy && git pull origin deploy
git merge master && git push origin deploy
git checkout master
```

`.github/workflows/cd.yml` creates the `deploy` branch from `master` if it does
not exist yet, and fails loudly when master and deploy cannot be merged -
nothing gets deployed that nobody reviewed.

### Running the tests

```bash
dotnet test UnitTests/UnitTests.csproj

# with coverage (same shape CI uses)
dotnet test UnitTests/UnitTests.csproj --collect:"XPlat Code Coverage" --results-directory TestResults
```

The harness (`UnitTests/Base.cs`, `MainWebApplicationFactory<Program>`) boots the
real WebAPI pipeline - middleware chain, action filters, authentication, role
guards - against an in-memory SQLite database, and replays the application's own
initializers so roles, permissions and the seeded administrator exist. It exposes
`Db`, `Setup()`, `SeedRole`, `SeedUser`, `SeedUniqueUser`, `SeedDemoModel`,
`CreateAuthenticatedToken(roleName)` and `CreateAuthenticatedClient(roleName)`.
Tests must not run in parallel (`UnitTests/AssemblyInfo.cs`) because each factory
owns one harness database and the repository layer serializes writes through one
tenant queue. No test needs Postgres, MSSQL or a live Redis server.

Because the suite drives real endpoints through the factory, it doubles as the
smoke test for WebAPI: registration, login, refresh, logout across all devices,
lockout after repeated failures, activation/deactivation and role-guarded 403s
are all exercised over HTTP.

## Container packaging

See `Dockerfile`: a multi-stage build that publishes only `WebAPI/WebAPI.csproj`
with the .NET 10 SDK image and runs it on the ASP.NET Core 10 runtime image. No
test project, no vendor tunnel glue. `.dockerignore` keeps build output out of the
build context.

```bash
docker build -t asp-template-api .
docker run --rm -p 8080:8080 asp-template-api
```

The image listens on 8080 (`ASPNETCORE_URLS` is set in the image) and runs as the
runtime image's unprivileged account. Nothing that a real deployment should own is
baked in: no database credentials, no signing secret, no telemetry endpoint, no
tunnel sidecar. Point the API at a database either with `DATABASE_URL` or by editing
the Postgres section of `appsettings.json`, and pass the signing secret, mail
credentials and OTLP endpoint through your own configuration or secret store.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. Keep new infrastructure domain-neutral: this template is meant to be forked into unrelated products.

## License

[MIT](https://choosealicense.com/licenses/mit/)
