using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.DataModels;
using Repository.Tenant.Interfaces;

namespace BusinessLogic.Initializers;

public abstract class BaseInitializer
{
    /// <summary>
    ///     Set by the test harness: production startup migration is skipped and initialization runs against the database the harness injected. 
    /// </summary> 
    public static bool UseTestingDb = false; 

    public static void StartInitialization(IServiceCollection serviceCollection, bool testingRun = false) 
    { 
        StartInitialization(serviceCollection.BuildServiceProvider(), testingRun); 
    } 

    public static void StartInitialization(IServiceProvider provider, bool testingRun = false) 
    { 
        // A testing run never touches the production providers: the harness already created its own database. 
        if (!testingRun) 
        { 
            using (var db = new ApplicationDbContext()) 
            { 
                db.Database.Migrate(); 
            } 

            using var scope = provider.CreateScope(); 
            var manager = scope.ServiceProvider.GetService<ITenantManager>(); 

            var comparer = EqualityComparer<TenantEntry>.Create( 
                (a, b) => a?.ConnectionString == b?.ConnectionString); 

            foreach (var tenantId in manager.GetAllTenants() 
                         .Where(u => u.ConnectionString != null).Distinct(comparer).Select(u => u.TenantId)) 
            { 
                manager.SwitchTenant(tenantId); 
                manager.GetDbContext().Database.Migrate(); 
            } 
        } 

        var currentAssembly = Assembly.GetAssembly(typeof(BaseInitializer)); 
        if (currentAssembly == null) throw new NullReferenceException(); 

        foreach (var type in currentAssembly.GetTypes() 
                     .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseInitializer)))) 
        { 
            using var scope = provider.CreateScope(); 
            var manager = scope.ServiceProvider.GetService<ITenantManager>(); 

            // A testing run initializes once, against the harness database, through the tenant manager the harness installed. 
            if (testingRun || UseTestingDb) 
            { 
                ((BaseInitializer)ActivatorUtilities.CreateInstance(scope.ServiceProvider, type)).Initialize(); 

                continue; 
            } 

            foreach (var tenantId in manager.GetAllTenants().Select(u => u.TenantId)) 
            { 
                manager.SwitchTenant(tenantId); 

                ((BaseInitializer)ActivatorUtilities.CreateInstance(scope.ServiceProvider, type)).Initialize(); 
            } 
        } 
    } 

    protected abstract void Initialize();
}
