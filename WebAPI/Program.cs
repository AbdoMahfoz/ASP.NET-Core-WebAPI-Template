using BusinessLogic.Implementations;
using BusinessLogic.Initializers;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Models.Helpers;
using Repository;
using Repository.ExtendedRepositories;
using Services.DTOs;
using Services.Helpers.MailService;
using Services.RoleSystem;
using Services.RoleSystem.Implementations;
using Services.RoleSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using WebAPI.Extensions;
using Microsoft.Extensions.Logging;
using Repository.ExtendedRepositories.RoleSystem;
using Repository.Tenant.Implementations;
using Repository.Tenant.Implementations.TenantStore;
using Repository.Tenant.Interfaces;
using WebAPI.Filters;
using WebAPI.GenericControllerCreator;
using WebAPI.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.Logging.AddConfiguration(configuration.GetSection("Logging"))
    .AddConsole()
    .AddDebug()
    .AddEventSourceLogger();
var services = builder.Services;

services.AddControllers();

services.AddMvc(opt =>
    {
        opt.Filters.Add(typeof(TenantActionFilter));
        opt.Filters.Add(typeof(ValidatorActionFilter));
        opt.Filters.Add(typeof(RoleActionFilter));
        opt.EnableEndpointRouting = false;
    })
    .ConfigureApplicationPartManager(p => p.FeatureProviders.Add(new GenericControllerFeatureProvider()));

// ValidatorActionFilter owns model-validation failures: without this, [ApiController] short-circuits an invalid
// ModelState into its own BadRequestObjectResult before any action filter runs.
services.Configure<ApiBehaviorOptions>(opt => opt.SuppressModelStateInvalidFilter = true);

var appSettingsSection = configuration.GetSection("AppSettings");
services.Configure<AppSettings>(appSettingsSection);
var appSettings = appSettingsSection.Get<AppSettings>();

// ---------------------------------------------------------------------------
// Observability: what this process says about itself.
//
// AppSettings:Observability decides which exporters carry spans and metrics out
// of the process - OTLP when a collector is configured, console as the fallback
// so a developer sees something without standing up infrastructure. The tracing
// pipeline itself (the activity source, the ASP.NET Core and EF Core
// instrumentation) is always on: turning off an exporter must not turn off the
// spans, or a debugger or a test tracer attached to the process would see
// nothing at all. In the Testing environment no exporter runs - a test run would
// either dial a collector that is not there or flood the test output with spans.
// ---------------------------------------------------------------------------
var observability = appSettings.Observability ?? new AppSettings.ObservabilityObject();
var activitySourceName = string.IsNullOrWhiteSpace(observability.ServiceName) ? "asp-template-API" : observability.ServiceName;

services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: activitySourceName, serviceVersion: observability.ServiceVersion))
    .WithTracing(tracing =>
    {
        tracing.AddSource(activitySourceName)
            .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
            // The EF Core instrumentation already emits the database attributes it can derive on its own. The executed command text is attached through the enrichment hook because a statement is what makes a slow trace legible. Parameter values stay off deliberately - in this template a parameter would carry a password or personal data straight into a trace, and the package's own documentation warns about exactly that.
            .AddEntityFrameworkCoreInstrumentation(opt =>
            {
                // Query parameter values are left alone: this package only records them when explicitly asked to, and in this template a parameter would carry a password or personal data straight into a trace.
                opt.EnrichWithIDbCommand = (activity, command) => activity.SetTag("db.statement", command.CommandText);
            });

        if (!builder.Environment.IsEnvironment("Testing"))
        {
            if (observability.UseOtlpExporter && Uri.TryCreate(observability.OtlpEndpoint, UriKind.Absolute, out var otlpEndpoint)) tracing.AddOtlpExporter(opt => opt.Endpoint = otlpEndpoint);
            else if (observability.UseConsoleExporter) tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(activitySourceName).AddAspNetCoreInstrumentation();

        if (!builder.Environment.IsEnvironment("Testing") && observability.UseOtlpExporter && Uri.TryCreate(observability.OtlpEndpoint, UriKind.Absolute, out var metricsEndpoint)) metrics.AddOtlpExporter(opt => opt.Endpoint = metricsEndpoint);
    });

// Repository<T>, the cache and the account logic take this tracer in their constructors. It is the same named activity source AddSource above listens to, which is why a span opened in the data layer lands inside the same trace as the HTTP request that triggered it.
services.AddSingleton(TracerProvider.Default.GetTracer(activitySourceName));

// Forwarded headers are only as good as the proxy list behind them. Since ASP.NET Core 8.0.17 a header asserted by a proxy that is not in KnownProxies/KnownNetworks is ignored outright, so an address a client makes up cannot become the recorded client address. TrustedProxyCidrs is where an operator names the proxies allowed to speak for a client; loopback is trusted by default, which covers a reverse proxy on the same host.
services.Configure<ForwardedHeadersOptions>(options =>
{
    if (Enum.TryParse(observability.ForwardedHeaders, ignoreCase: true, out ForwardedHeaders forwardedHeaders)) options.ForwardedHeaders = forwardedHeaders;

    foreach (var cidr in observability.TrustedProxyCidrs ?? []) if (System.Net.IPNetwork.TryParse(cidr, out var network)) options.KnownIPNetworks.Add(network);
});
var key = Encoding.ASCII.GetBytes(appSettings.Secret);


services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicy(
        new IAuthorizationRequirement[] { new AccountRequirement() },
        options.DefaultPolicy.AuthenticationSchemes
    );
});

services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = appSettings.SiteData.Name,
        Description = appSettings.SiteData.ApiDescription,
        Contact = new OpenApiContact { Name = appSettings.Contact.Name, Email = appSettings.Contact.Email }
    });
    c.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter into field the word 'Bearer' following by space and Access token",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        }
    );
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

services.AddScoped<ITenantManager, TenantManager>();
services.AddSingleton<ITenantResolver, TokenTenantResolver>();
services.AddConfiguredCache(appSettings);
if (appSettings.Tenant.UseDbStore)
{
    services.AddScoped<ITenantStore, DbTenantStore>();
}
else
{
    services.AddSingleton<ITenantStore, ReadOnlyMemoryTenantStore>();
}

services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
services.AddScoped(typeof(IGenericLogic<,,>), typeof(GenericLogic<,,>));


services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IPermissionsRepository, PermissionsRepository>();
services.AddScoped<IRolesRepository, RolesRepository>();

services.AddScoped<IAuthorizationHandler, LoginHandler>();
services.AddScoped<IActionPermissionRepository, ActionPermissionRepository>();
services.AddScoped<IActionRolesRepository, ActionRolesRepository>();
services.AddScoped<IActionRoleManager, ActionRoleManager>();

services.AddTransient<IAuth, JwtAuthorization>();
services.AddTransient<IAccountLogic, AccountLogic>();
services.AddTransient<IMailService, SmtpMailService>();
services.AddTransient<IRolesAndPermissionsManager, RolesAndPermissionsManager>();


services.AddSingleton<IPasswordManager, Rfc2898PasswordManager>();


if (appSettings.ValidateRolesFromToken) services.AddSingleton<IRoleValidator, TokenRoleValidator>();
else services.AddScoped<IRoleValidator, DbRoleValidator>();

// The test harness runs initialization against its own database with testingRun: true, so production migration only happens outside a harness run.
if (!BaseInitializer.UseTestingDb) BaseInitializer.StartInitialization(services);

var app = builder.Build();
if (builder.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
else app.UseMiddleware(typeof(ErrorHandlingMiddleware));
// Address and scheme corrections happen before anything else reads them: CORS, the request logger and the tenant resolver all look at where the request came from.
app.UseForwardedHeaders();
//app.UseHttpsRedirection();
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json",
        $"{configuration.GetSection("AppSettings").Get<AppSettings>().SiteData.Name} API V1");
});
// Typed exception -> status mapping. Tenant and role resolution run in MVC filters further in the pipeline,
// so this handler wraps them; anything it does not map falls through to ErrorHandlingMiddleware.
app.UseMiddleware(typeof(CommonExceptionsMiddleware));
app.UseRouting();
// After routing so MVC's chosen controller and action are known, and before authentication so every request - answered, refused or thrown - gets exactly one log line with its status and duration.
app.UseMiddleware(typeof(RequestResponseLoggingMiddleware));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSwagger();

app.Run();

// Exposed so the WebApplicationFactory based harness can boot this exact application.
public partial class Program
{
}