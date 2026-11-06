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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSwagger();

app.Run();

// Exposed so the WebApplicationFactory based harness can boot this exact application.
public partial class Program
{
}