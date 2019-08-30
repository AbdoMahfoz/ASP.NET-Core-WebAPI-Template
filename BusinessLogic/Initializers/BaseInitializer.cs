using System;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Initializers
{
    public class BaseInitializer
    {
        private readonly IServiceProvider Provider;
        public BaseInitializer(IServiceProvider Provider)
        {
            this.Provider = Provider;
        }
        protected BaseInitializer() { }
        public virtual void Initialize()
        {
            foreach (Type type in (from type in Assembly.GetAssembly(typeof(BaseInitializer)).GetTypes()
                                   where type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseInitializer))
                                   select type))
            {
                ((BaseInitializer)ActivatorUtilities.CreateInstance(Provider, type)).Initialize();
            }
        }
    }
}
