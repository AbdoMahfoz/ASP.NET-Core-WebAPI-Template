using BusinessLogic.Implementations;
using BusinessLogic.Initializers;
using BusinessLogic.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Models.Helpers;
using Repository;
using Repository.ExtendedRepositories;
using Services.DTOs;
using Services.Helpers.MailService;
using Services.RoleSystem;
using Services.RoleSystem.Implementations;
using Services.RoleSystem.Interfaces;
using Services.Validators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BusinessLogic.Implementations.Cache;
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

services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();

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
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "ApiKey", // to be rechecked
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

services.AddScoped<ITenantManager, TenantManager>();
services.AddSingleton<ITenantResolver, TokenTenantResolver>();
services.AddSingleton<ICache, MemoryCache>();
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
services.AddTransient<IValidator<UserAuthenticationRequest>, UserAuthenticationRequestValidator>();
services.AddTransient<IMailService, SmtpMailService>();
services.AddTransient<IRolesAndPermissionsManager, RolesAndPermissionsManager>();


services.AddSingleton<IPasswordManager, Rfc2898PasswordManager>();


if (appSettings.ValidateRolesFromToken) services.AddSingleton<IRoleValidator, TokenRoleValidator>();
else services.AddScoped<IRoleValidator, DbRoleValidator>();

BaseInitializer.StartInitialization(services);

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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSwagger();

app.Run();