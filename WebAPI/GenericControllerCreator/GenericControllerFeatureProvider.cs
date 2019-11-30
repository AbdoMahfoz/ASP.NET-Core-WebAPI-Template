using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Models.Helpers;

namespace WebAPI.GenericControllerCreator
{
    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var entityType in IncludedEntities.Types)
            {
                var typeName = entityType.Item1.Name + "Controller";

                // Check to see if there is a missing controller for this class
                if (!feature.Controllers.Any(t => t.Name == typeName))
                {
                    // Create a generic controller for this type
                    var controllerType = typeof(GenericController<,,>).MakeGenericType(entityType.Item1, entityType.Item2, entityType.Item3).GetTypeInfo();
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }
}