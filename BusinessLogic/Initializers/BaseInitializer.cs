using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

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
            ((BaseInitializer)ActivatorUtilities.CreateInstance(provider, type)).Initialize();
        }
    }

    protected abstract void Initialize();
}