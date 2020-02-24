using BusinessLogic.Implementations;
using BusinessLogic.Initializers;
using BusinessLogic.Interfaces;

using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Models;
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

using WebAPI.GenericControllerCreator;
using WebAPI.Middleware;

namespace WebAPI
{
    public class Startup
    {
        private readonly ILogger _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddControllers().AddNewtonsoftJson();

            services.AddMvc(opt =>
                {
                    opt.Filters.Add(typeof(ValidatorActionFilter));
                    opt.Filters.Add(typeof(RoleActionFilter));
                    opt.EnableEndpointRouting = false;
                })
                .AddFluentValidation()
                .ConfigureApplicationPartManager(p => p.FeatureProviders.Add(new GenericControllerFeatureProvider()));

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();
            ApplicationDbContext.LocalDatabaseName = appSettings.LocalDatabaseName;
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddDbContext<ApplicationDbContext>(ApplicationDbContext.Configure);


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
                    new IAuthorizationRequirement[] {new AccountRequirement()},
                    options.DefaultPolicy.AuthenticationSchemes
                );
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = appSettings.SiteData.Name,
                    Description = appSettings.SiteData.APIDescription,
                    Contact = new OpenApiContact {Name = appSettings.Contact.Name, Email = appSettings.Contact.Email}
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

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(ICachedRepository<>), typeof(CachedRepository<>));
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


            services.AddSingleton<IPasswordManager, RFC2898PasswordManager>();


            if (appSettings.ValidateRolesFromToken) services.AddSingleton<IRoleValidator, TokenRoleValidator>();
            else services.AddScoped<IRoleValidator, DbRoleValidator>();

            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                db.Database.Migrate();
            }

            new BaseInitializer(services.BuildServiceProvider());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            else app.UseHsts();
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseMvc(routes => { routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}"); });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json",
                    $"{Configuration.GetSection("AppSettings").Get<AppSettings>().SiteData.Name} API V1");
            });
        }
    }
}