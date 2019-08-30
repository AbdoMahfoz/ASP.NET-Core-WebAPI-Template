using System;
using System.Reflection;
using System.Linq;

namespace BusinessLogic.Initializers
{
    public class BaseInitializer
    {
        public virtual void Initialize()
        {
            foreach (Type type in (from type in Assembly.GetAssembly(typeof(BaseInitializer)).GetTypes()
                                   where type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseInitializer))
                                   select type))
            {
                ((BaseInitializer)Activator.CreateInstance(type)).Initialize();
            }
        }
    }
}
