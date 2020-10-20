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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            Configure(options);
        }

        public static void Configure(DbContextOptionsBuilder options)
        {
            options.UseLazyLoadingProxies();
            string url = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (string.IsNullOrWhiteSpace(url))
            {
                if (string.IsNullOrWhiteSpace(LocalDatabaseName))
                    LocalDatabaseName = JsonConvert.DeserializeAnonymousType(
                        File.ReadAllText(Path.Combine("..", "WebAPI", "appsettings.json")),
                        new {AppSettings = new AppSettings()}).AppSettings.LocalDatabaseName;
                options.UseNpgsql($"Host=localhost;Port=5432;Database={LocalDatabaseName};Username=user;Password=123");
            }
            else
            {
                url = url.Substring(url.IndexOf("//", StringComparison.Ordinal) + 2);
                string userName = url.Substring(0, url.IndexOf(':'));
                url = url.Substring(url.IndexOf(':') + 1);
                string password = url.Substring(0, url.IndexOf('@'));
                url = url.Substring(url.IndexOf('@') + 1);
                string host = url.Substring(0, url.IndexOf(':'));
                url = url.Substring(url.IndexOf(':') + 1);
                string port = url.Substring(0, url.IndexOf('/'));
                string database = url.Substring(url.IndexOf('/') + 1);
                options.UseNpgsql(
                    $"Host={host};Port={port};Database={database};Username={userName};Password={password};SSLMode=Require;TrustServerCertificate=true");
            }
        }
    }
}