using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.DataModels;
using Repository.Tenant.Interfaces;

namespace BusinessLogic.Initializers;

public abstract class BaseInitializer
{
    public static void StartInitialization(IServiceCollection serviceCollection)
    {
        using (var db = new ApplicationDbContext())
        {
            db.Database.Migrate();
        }

        var provider = serviceCollection.BuildServiceProvider();
        {
            using var scope = provider.CreateScope();
            var man = scope.ServiceProvider.GetService<ITenantManager>();
            var comparer = EqualityComparer<TenantEntry>.Create(
                (a, b) => a?.ConnectionString == b?.ConnectionString);
            foreach (var tenantId in man.GetAllTenants()
                         .Where(u => u.ConnectionString != null).Distinct(comparer).Select(u => u.TenantId))
            {
                man.SwitchTenant(tenantId);
                man.GetDbContext().Database.Migrate();
            }
        }
        var currentAssembly = Assembly.GetAssembly(typeof(BaseInitializer));
        if (currentAssembly == null) throw new NullReferenceException();
        foreach (var type in currentAssembly.GetTypes()
                     .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseInitializer))))
        {
            using var scope = provider.CreateScope();
            var man = scope.ServiceProvider.GetService<ITenantManager>();
            foreach (var tenantId in man.GetAllTenants().Select(u => u.TenantId))
            {
                man.SwitchTenant(tenantId);
                ((BaseInitializer)ActivatorUtilities.CreateInstance(scope.ServiceProvider, type)).Initialize();
            }
        }
    }

    protected abstract void Initialize();
}