using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using BusinessLogic.Initializers;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Models;
using Models.DataModels;
using Models.DataModels.SimpleModels;
using Repository;
using Repository.Tenant.Interfaces;

namespace UnitTests;

/// <summary>
///     Tenant manager used by the harness: every consumer gets the harness-owned database instead of a tenant-store connection string, so no code path can reach Postgres/MSSQL during a test run. The context is resolved lazily so the harness can recreate the database between tests. 
/// </summary> 
public sealed class HarnessTenantManager(Func<ApplicationDbContext> contextProvider) : ITenantManager 
{ 
    public Semaphore ManipulationQueue { get; } = new(1, int.MaxValue); 

    public int TenantId { get; private set; } = 1; 

    public ApplicationDbContext GetDbContext() => contextProvider(); 

    public IEnumerable<TenantEntry> GetAllTenants() 
    { 
        yield return new TenantEntry { TenantId = 1, ConnectionString = null }; 
    } 

    public void SwitchTenant(int tenantId) 
    { 
        TenantId = tenantId == 0 ? 1 : tenantId; 
    } 

    /// <summary>Tenant resolution is fixed for a test run: requests are served by the harness tenant.</summary> 
    public void ResolveTenant(HttpContext context) 
    { 
        TenantId = 1; 
    } 

    /// <summary>Seeds a tenant row so endpoints that read the tenant table behave like a real deployment.</summary> 
    public static void SeedTenant(ApplicationDbContext db, int tenantId = 1) 
    { 
        if (db.Tenants.Any(t => t.TenantId == tenantId)) return; 

        db.Tenants.Add(new TenantEntry { TenantId = tenantId, ConnectionString = null }); 
        db.SaveChanges(); 
    } 
} 

/// <summary>
///     WebApplicationFactory harness for this template: boots the real WebAPI pipeline (filters, middleware, authentication, role guards) against an in-memory SQLite database and replays the application's own initializers so roles, permissions and the seeded Admin account exist. Call Setup() before use.     
/// </summary> 
public class MainWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class 
{ 
    private const string HarnessUserPassword = "Test123123"; 

    private SqliteConnection _testingConnection; 
    private HarnessTenantManager _tenantManager; 
    private int _accountIndex; 

    private readonly List<Action<IServiceCollection>> _serviceOverrides = new(); 

    /// <summary>The harness database every repository under test resolves through HarnessTenantManager.</summary>     
    public ApplicationDbContext Db { get; private set; } 

    public MainWebApplicationFactory()     
    {         
        CreateDatabase();     
    }     

    /// <summary>One DI scope against the application's own container, so seeding and token issuing see exactly the registrations requests see.</summary>     
    protected IServiceScope Scope() => base.Services.CreateScope();     

    /// <summary>
    ///     Replaces or adds services before the application under test is built, so a test can inject a fault into a real business service instead of hand-wiring middleware. Call before Setup() or the first request.     
    /// </summary>     
    public void OverrideServices(Action<IServiceCollection> configure)     
    {         
        _serviceOverrides.Add(configure);     
    }     

    private void CreateDatabase()     
    {         
        _testingConnection?.Dispose();         

        _testingConnection = new SqliteConnection("Filename=:memory:");         
        _testingConnection.Open();         

        Db = new ApplicationDbContext(_testingConnection);         
        Db.Database.EnsureCreated();         

        if (_tenantManager == null) _tenantManager = new HarnessTenantManager(() => Db);         
        else HarnessTenantManager.SeedTenant(Db);     
    }     

    protected override void ConfigureWebHost(IWebHostBuilder builder)     
    {         
        // Startup must not migrate production providers when the harness owns the database.         
        BaseInitializer.UseTestingDb = true;         

        builder.ConfigureTestServices(services =>         
        {             
            services.RemoveAll<ITenantManager>();             
            services.AddSingleton<ITenantManager>(_tenantManager);             

            // Test-owned substitutions run after the harness replacements so a test can override either.             
            foreach (var configure in _serviceOverrides) configure(services);         

        });         

        builder.UseEnvironment("Testing");     
    }     

    /// <summary>Recreates the harness database and replays the application initializers against it.</summary>     
    public void Setup()     
    {         
        CreateDatabase();         

        HarnessTenantManager.SeedTenant(Db);         

        // The same initialization the application runs on startup, but against the harness database and its container.         
        BaseInitializer.StartInitialization(base.Services, testingRun: true);         

        Db.SaveChanges();     
    }     

    /// <summary>Registers a user with the given role name and returns its user id.</summary>     
    public int SeedUser(string userName, string password, string roleName)     
    {         
        using var scope = Scope();         

        var accountLogic = scope.ServiceProvider.GetRequiredService<IAccountLogic>();         
        if (!accountLogic.Register(new Services.DTOs.UserAuthenticationRequest { Username = userName, Password = password }, roleName))             
            throw new InvalidOperationException($"Harness could not register user '{userName}' with role '{roleName}'.");         

        using var dbScope = Scope();         

        return dbScope.ServiceProvider.GetRequiredService<IRepository<User>>()             
            .GetAll().Single(u => u.UserName == userName).Id;     
    }     

    /// <summary>Registers a uniquely named user for the calling test and returns its credentials.</summary>     
    public (string UserName, string Password) SeedUniqueUser(string prefix, string roleName)     
    {         
        var userName = $"{prefix}-{Interlocked.Increment(ref _accountIndex)}";         

        SeedUser(userName, HarnessUserPassword, roleName);         

        return (userName, HarnessUserPassword);     
    }     

    /// <summary>Adds a DemoModel row and returns it once the repository has assigned its id.</summary>     
    public DemoModel SeedDemoModel(string name)     
    {         
        using var scope = Scope();         

        var repository = scope.ServiceProvider.GetRequiredService<IRepository<DemoModel>>();         
        repository.Insert(new DemoModel { Name = name }).GetAwaiter().GetResult();         

        return repository.GetAll().Single(u => u.Name == name);     
    }     

    /// <summary>Issues a token through the application's own auth stack for a freshly seeded user.</summary>     
    public string CreateAuthenticatedToken(string roleName, string prefix = "harness-user")     
    {         
        var (userName, password) = SeedUniqueUser(prefix, roleName);         

        using var scope = Scope();         
        var auth = scope.ServiceProvider.GetRequiredService<IAuth>();         

        var (token, _) = auth.Authenticate(new Services.DTOs.UserAuthenticationRequest { Username = userName, Password = password });         

        if (string.IsNullOrEmpty(token))             
            throw new InvalidOperationException($"Harness could not authenticate '{userName}'.");         

        return token;     
    }     

    /// <summary>An HTTP client carrying a bearer token produced by the application's auth stack.</summary>     
    public HttpClient CreateAuthenticatedClient(string roleName, string prefix = "harness-user")     
    {         
        var client = CreateClient();         
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateAuthenticatedToken(roleName, prefix));         
        return client;     
    } 
} 
