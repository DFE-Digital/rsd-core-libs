using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace DfE.CoreLibs.Utilities.Helpers
{
    /// <summary>
    /// Retrieves all the controllers and their methods from the provided assembly.
    /// </summary>
    public static class ControllerHelper
    {
        public static IEnumerable<(Type Controller, MethodInfo Method)> GetAllControllerMethodsTuples(Assembly assembly)
        {
            var controllers = assembly.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type));

            foreach (var controller in controllers)
            {
                var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsPublic && !m.IsDefined(typeof(NonActionAttribute)));

                foreach (var method in methods)
                {
                    yield return (controller, method);
                }
            }
        }
    }
}