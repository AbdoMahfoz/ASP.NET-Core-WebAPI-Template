using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace WebAPI.GenericControllerCreator
{
    //used to simplify the controller name in navigation 
    [AttributeUsage(AttributeTargets.Class)]
    public class GenericControllerNameAttribute : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.GetGenericTypeDefinition() == typeof(GenericController<,,>))
            {
                var entityType = controller.ControllerType.GenericTypeArguments[0];
                controller.ControllerName = entityType.Name;
            }
        }
    }
}