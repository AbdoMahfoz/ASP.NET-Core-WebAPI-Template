using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Models.DataModels;
using Models.Helpers;
using Newtonsoft.Json;

namespace Models
{
    public class ApplicationDbContext : DbContext
    {
        public static string LocalDatabaseName { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ActionRole> ActionRoles { get; set; }
        public DbSet<ActionPermission> ActionPermissions { get; set; }
        
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            Configure(options);
        }

        public static void Configure(DbContextOptionsBuilder options)
        {
            options.UseLazyLoadingProxies();
            string file;
            try
            {
                file = File.ReadAllText(Path.Combine("..", "WebAPI", "appsettings.json"));
            }
            catch (Exception)
            {
                file = File.ReadAllText("appsettings.json");
            }
            var appSettings = JsonConvert.DeserializeAnonymousType(file,
                new { AppSettings = new AppSettings() })!.AppSettings;
            if (appSettings.Postgres.Use)
            {
                var url = Environment.GetEnvironmentVariable("DATABASE_URL");
                if (string.IsNullOrWhiteSpace(url))
                {
                    options.UseNpgsql(
                        $"Host={appSettings.Postgres.Host};" +
                        $"Port={appSettings.Postgres.Port};" +
                        $"Database={appSettings.Postgres.DatabaseName};" +
                        $"Username={appSettings.Postgres.Username};" +
                        $"Password={appSettings.Postgres.Password}");
                }
                else
                {
                    url = url[(url.IndexOf("//", StringComparison.Ordinal) + 2)..];
                    var userName = url[..url.IndexOf(':')];
                    url = url[(url.IndexOf(':') + 1)..];
                    var password = url[..url.IndexOf('@')];
                    url = url[(url.IndexOf('@') + 1)..];
                    string host = url[..url.IndexOf(':')];
                    url = url[(url.IndexOf(':') + 1)..];
                    string port = url[..url.IndexOf('/')];
                    string database = url[(url.IndexOf('/') + 1)..];
                    options.UseNpgsql(
                        $"Host={host};Port={port};Database={database};Username={userName};" +
                        $"Password={password};SSLMode=Require;TrustServerCertificate=true");
                }
            }
            else if (appSettings.MSSQL.Use)
            {
                options.UseSqlServer(appSettings.MSSQL.ConnectionString);
            }
        }
    }
}