using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Repository.Tenant.Interfaces;

namespace BusinessLogic.Initializers;

public abstract class BaseInitializer
{
    public static void StartInitialization(IServiceCollection serviceCollection)
    {
        var provider = serviceCollection.BuildServiceProvider();
        var currentAssembly = Assembly.GetAssembly(typeof(BaseInitializer));
        if (currentAssembly == null) throw new NullReferenceException();
        foreach (var type in currentAssembly.GetTypes()
                     .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseInitializer))))
        {
            using var scope = provider.CreateScope();
            var man = scope.ServiceProvider.GetService<ITenantManager>();
            foreach (var tenantId in man.GetAllTenants())
            {
                man.SwitchTenant(tenantId);
                ((BaseInitializer)ActivatorUtilities.CreateInstance(scope.ServiceProvider, type)).Initialize();
            }
        }
    }

    protected abstract void Initialize();
}