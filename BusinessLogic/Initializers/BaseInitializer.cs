using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Initializers
{
    public abstract class BaseInitializer
    {
        static public void StartInitialization(IServiceCollection serviceCollection)
        {
            var provider = serviceCollection.BuildServiceProvider();
            var currentAssembly = Assembly.GetAssembly(typeof(BaseInitializer));
            if (currentAssembly == null) throw new NullReferenceException();
            foreach (Type type in (from type in currentAssembly.GetTypes()
                where type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseInitializer))
                select type))
            {
                ((BaseInitializer)ActivatorUtilities.CreateInstance(provider, type)).Initialize();
            }
        }
        protected virtual void Initialize()
        {
        }
    }
}
