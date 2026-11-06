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
///     Tenant manager used by the test harness. Every consumer resolves the harness-owned database instead of a tenant-store connection string, so no code path can reach Postgres or MSSQL during a test run. The context is resolved lazily because the harness recreates the database between tests.  
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


    public void SwitchTenant(int tenantId) => TenantId = tenantId == 0 ? 1 : tenantId;


    /// <summary>Tenant resolution is fixed for a test run: every request is served by the harness tenant.</summary>  
    public void ResolveTenant(HttpContext context) => TenantId = 1;


    /// <summary>Seeds a tenant row so endpoints that read the tenant table behave like a real deployment.</summary>  
    public static void SeedTenant(ApplicationDbContext db, int tenantId = 1)

    {

        if (db.Tenants.Any(t => t.TenantId == tenantId)) return;


        db.Tenants.Add(new TenantEntry { TenantId = tenantId, ConnectionString = null });


        db.SaveChanges(); // The harness writes this directly: it is scaffolding around the test, not an operation under test.  
    }
}


/// <summary>
///     The WebApplicationFactory harness for this template. It boots the real WebAPI pipeline - middleware chain, action filters, authentication, role guards - against an in-memory SQLite database, and replays the application's own initializers so roles, permissions and the seeded administrator account exist exactly as they do at startup. Call Setup() before using it.  
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


    public MainWebApplicationFactory() => CreateDatabase();


    /// <summary>One DI scope against the application's own container, so seeding and token issuing see exactly the registrations a request sees.</summary>  
    protected IServiceScope Scope() => base.Services.CreateScope();


    /// <summary>
    ///     Replaces or adds services before the application under test is built, so a test can substitute a real business service instead of hand-wiring middleware. Call before Setup() or before the first request.  
    /// </summary>
    public void OverrideServices(Action<IServiceCollection> configure) => _serviceOverrides.Add(configure);


    private void CreateDatabase()

    {

        _testingConnection?.Dispose();


        // A SQLite connection kept open for the lifetime of the factory: an in-memory database disappears with its last connection.  
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


        // The same initialization the application runs on startup, but against the harness database and its container. Initializers save through their own repositories, so nothing is left for this context to write: calling SaveChanges here would race with an initializer still finishing on the same context instance.  
        BaseInitializer.StartInitialization(base.Services, testingRun: true);


        Db.ChangeTracker.Clear();
        
    }


    /// <summary>Creates a role through the application's own role manager when no role of that name exists yet, so a test can build a world with roles beyond the seeded set. Returns the role id.</summary>  
    public int SeedRole(string roleName)

    {

        using var scope = Scope();


        var roles = scope.ServiceProvider
            .GetRequiredService<Repository.ExtendedRepositories.RoleSystem.IRolesRepository>();


        // The identity comes from the persisted role rather than RoleDto, whose Id travels as a string in this template's DTO shape.
        var existing = roles.GetRole(roleName);

        if (existing != null) return existing.Id;


        // Creation goes through the same manager the role-admin endpoint uses, so a harness role is created the way an operator would create one.
        scope.ServiceProvider.GetRequiredService<IRolesAndPermissionsManager>().InsertRole(roleName);


        return roles.GetRole(roleName).Id;
        
    }


    /// <summary>Registers an account with one or more role names and returns its user id. The first role is the one handed to registration; any extra role is attached afterwards through the same manager the admin API uses, which is how a real account ends up carrying several roles.</summary>  
    public int SeedUser(string userName, string password, params string[] roleNames)

    {

        using var scope = Scope();


        var roleId = scope.ServiceProvider
            .GetRequiredService<Repository.ExtendedRepositories.RoleSystem.IRolesRepository>()
            .GetRole(roleNames[0])?.Id;



        var accountLogic = scope.ServiceProvider.GetRequiredService<IAccountLogic>();



        if (accountLogic.Register(new Services.DTOs.Requests.CreateUserRequest { UserName = userName, Password = password }, roleId).GetAwaiter().GetResult() == null)
        
            throw new InvalidOperationException($"Harness could not register user '{userName}' with roles {string.Join(", ", roleNames)}.");
        


        var userId = scope.ServiceProvider
            .GetRequiredService<IRepository<User>>()
            .GetAll()
            .Single(u => u.UserName == userName)
            .Id;



        var roleManager = scope.ServiceProvider.GetRequiredService<IRolesAndPermissionsManager>();



        foreach (var extraRole in roleNames.Skip(1)) roleManager.AssignRoleToUser(extraRole, userId);



        return userId;
        
    }


    /// <summary>Registers a uniquely named account for the calling test and returns its credentials.</summary>  
    public (string UserName, string Password) SeedUniqueUser(string prefix, params string[] roleNames)

    {

        var userName = $"{prefix}-{Interlocked.Increment(ref _accountIndex)}";


        SeedUser(userName, HarnessUserPassword, roleNames);


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


    /// <summary>Issues a token through the application's own auth stack for a freshly seeded account. Extra role names are attached to that account before the token is issued, so the token carries every role claim the guard checks.</summary>  
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


    /// <summary>An HTTP client carrying a bearer token produced by the application's own auth stack.</summary>  
    public HttpClient CreateAuthenticatedClient(string roleName, string prefix = "harness-user")

    {

        var client = CreateClient();


        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateAuthenticatedToken(roleName, prefix));


        return client;
        
    }


    /// <summary>An HTTP client whose account carries several roles at once - the shape a role guard needs when a test asserts that any one of several roles is enough.</summary>  
    public HttpClient CreateAuthenticatedClientWithRoles(string prefix, params string[] roleNames)

    {

        var (userName, password) = SeedUniqueUser(prefix, roleNames);


        using var scope = Scope();


        var auth = scope.ServiceProvider.GetRequiredService<IAuth>();


        var (token, _) = auth.Authenticate(new Services.DTOs.UserAuthenticationRequest { Username = userName, Password = password });



        if (string.IsNullOrEmpty(token))
        
            throw new InvalidOperationException($"Harness could not authenticate '{userName}' with roles {string.Join(", ", roleNames)}.");
        


        var client = CreateClient();


        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


        return client;
        
    }
}
