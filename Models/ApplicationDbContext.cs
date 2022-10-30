using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // add is deleted query filter for entities having an existing IsDeleted property
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var isDeletedProperty = entityType.FindProperty("IsDeleted");
                if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
                {
                    var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var methodInfo = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool))!;
                    var efPropertyCall = Expression.Call(null, methodInfo, parameter, Expression.Constant("IsDeleted"));
                    var body = Expression.MakeBinary(ExpressionType.Equal, efPropertyCall, Expression.Constant(false));
                    var expression = Expression.Lambda(body, parameter);
                    entityBuilder.HasQueryFilter(expression);
                }
            }

        }

        public override int SaveChanges()
        {
            foreach (var entry in ChangeTracker.Entries<BaseModel>().ToList())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.AddedDate = DateTime.UtcNow;
                }
                if(entry.State == EntityState.Modified && entry.Entity.IsDeleted == true)
                {
                    entry.Entity.DeletedDate = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedDate = DateTime.UtcNow;
                }
            }
            var result = base.SaveChanges();
            return result;
        }
    }
}