using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Models.DataModels;
using Models.DataModels.RoleSystem;
using Models.Helpers;
using Newtonsoft.Json;

namespace Models;

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
    public DbSet<TenantEntry> Tenants { get; set; }

    public int TenantId { get; private init; } = 1;
    private string _connectionString;

    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(int tenantId = 1, string connectionString = null)
    {
        TenantId = tenantId;
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
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
            if (_connectionString != null)
            {
                options.UseNpgsql(_connectionString);
            }
            else
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
                    var host = url[..url.IndexOf(':')];
                    url = url[(url.IndexOf(':') + 1)..];
                    var port = url[..url.IndexOf('/')];
                    var database = url[(url.IndexOf('/') + 1)..];
                    options.UseNpgsql(
                        $"Host={host};Port={port};Database={database};Username={userName};" +
                        $"Password={password};SSLMode=Require;TrustServerCertificate=true");
                }
            }
        }
        else if (appSettings.Mssql.Use)
        {
            if (_connectionString == null)
            {
                options.UseSqlServer(appSettings.Mssql.ConnectionString);
            }
            else
            {
                options.UseSqlServer(_connectionString);
            }
        }
    }

    private static LambdaExpression MakeEqualityComparison<T>(IReadOnlyTypeBase entityType, string property,
        Expression value)
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var methodInfo = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(T))!;
        var efPropertyCall = Expression.Call(null, methodInfo, parameter, Expression.Constant(property));
        var body = Expression.MakeBinary(ExpressionType.Equal, efPropertyCall, value);
        return Expression.Lambda(body, parameter);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // add is deleted query filter for entities having an existing IsDeleted property
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isDeletedProperty = entityType.FindProperty(nameof(BaseModel.IsDeleted));
            if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
            {
                var expression = MakeEqualityComparison<bool>(entityType, nameof(BaseModel.IsDeleted),
                    Expression.Constant(false));
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(expression);
            }

            /*
            var tenantIdProperty = entityType.FindProperty(nameof(BaseModel.TenantId));
            if (tenantIdProperty != null && tenantIdProperty.ClrType == typeof(int))
            {
                var expression = MakeEqualityComparison<int>(entityType, nameof(BaseModel.TenantId),
                    Expression.Property(Expression.Constant(this), nameof(TenantId)));
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(expression);
            }
            */
        }
    }

    private void SaveHelper()
    {
        foreach (var entry in ChangeTracker.Entries<BaseModel>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.AddedDate = DateTime.UtcNow;
                    entry.Entity.TenantId = TenantId;
                    break;
                case EntityState.Modified when entry.Entity.IsDeleted &&
                                               !entry.OriginalValues.GetValue<bool>(nameof(entry.Entity.IsDeleted)):
                    entry.Entity.DeletedDate = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedDate = DateTime.UtcNow;
                    break;
            }
        }
    }

    public override int SaveChanges()
    {
        SaveHelper();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SaveHelper();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new CancellationToken())
    {
        SaveHelper();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        SaveHelper();
        return base.SaveChangesAsync(cancellationToken);
    }
}