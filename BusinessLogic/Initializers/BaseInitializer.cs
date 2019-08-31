using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Initializers
{
    public class BaseInitializer
    {
        public BaseInitializer(IServiceProvider Provider)
        {
            foreach (Type type in (from type in Assembly.GetAssembly(typeof(BaseInitializer)).GetTypes()
                                   where type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseInitializer))
                                   select type))
            {
                ((BaseInitializer)ActivatorUtilities.CreateInstance(Provider, type)).Initialize();
            }
        }
        protected BaseInitializer() { }
        public virtual void Initialize()
        {
        }
    }
}
