using DfE.CoreLibs.Utilities.Helpers;
using System.Reflection;
using DfE.CoreLibs.Testing.Authorization.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Testing.Authorization.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class EndpointTestDataProvider
    {
        /// <summary>
        /// Loads endpoint test data from a JSON string.
        /// </summary>
        /// <param name="assembly">The assembly containing controllers.</param>
        /// <param name="jsonContent">The JSON content as a string.</param>
        /// <returns>An enumerable of object arrays containing test data.</returns>
        public static IEnumerable<object[]> GetEndpointTestData(Assembly assembly, string jsonContent)
        {
            var expectedSecurityConfig = SecurityConfigLoader.LoadFromJson(jsonContent);

            return ParseExpectedSecurityData(assembly, expectedSecurityConfig);

        }

        /// <summary>
        /// Loads endpoint test data from a JSON file.
        /// </summary>
        /// <param name="assembly">The assembly containing controllers.</param>
        /// <param name="configFilePath">The path to the JSON configuration file.</param>
        /// <returns>An enumerable of object arrays containing test data.</returns>
        public static IEnumerable<object[]> GetEndpointTestDataFromFile(Assembly assembly, string configFilePath)
        {
            var jsonContent = File.ReadAllText(configFilePath);
            return GetEndpointTestData(assembly, jsonContent);
        }

        /// <summary>
        /// Loads endpoint test data from an in-memory dictionary.
        /// </summary>
        /// <param name="assembly">The assembly containing controllers.</param>
        /// <param name="expectedSecurityConfig">A dictionary of expected security configurations.</param>
        /// <returns>An enumerable of object arrays containing test data.</returns>
        public static IEnumerable<object[]> GetEndpointTestDataFromDictionary(Assembly assembly, Dictionary<string, string> expectedSecurityConfig)
        {
            return ParseExpectedSecurityData(assembly, expectedSecurityConfig);
        }

        private static IEnumerable<object[]> ParseExpectedSecurityData(Assembly assembly, Dictionary<string, string> expectedSecurityConfig)
        {
            var endpoints = ControllerHelper.GetAllControllerMethodsTuples(assembly);

            // ReSharper disable once CollectionNeverQueried.Local
            var definedEndpoints = new HashSet<string>();

            var valueTuples = endpoints as (Type Controller, MethodInfo Method)[] ?? endpoints.ToArray();

            foreach (var (controller, method) in valueTuples)
            {
                var key = $"{controller.Name}.{method.Name}";
                if (expectedSecurityConfig.TryGetValue(key, out var expectedSecurity))
                {
                    definedEndpoints.Add(key);
                }
                else
                {
                    expectedSecurity = null;
                }

#pragma warning disable CS8601 // Possible null reference assignment.
                yield return new object[] { controller.Name, method.Name, expectedSecurity };
#pragma warning restore CS8601 // Possible null reference assignment.
            }

            // Check for any extra entries in the configuration that don't correspond to actual endpoints
            var actualEndpointKeys = valueTuples.Select(e => $"{e.Controller.Name}.{e.Method.Name}");
            var extraConfigEntries = expectedSecurityConfig.Keys.Except(actualEndpointKeys);

            var configEntries = extraConfigEntries as string[] ?? extraConfigEntries.ToArray();
            if (configEntries.Any())
            {
                throw new ExtraConfigurationException($"The following endpoints are defined in the configuration but do not exist in the assembly: {string.Join(", ", configEntries)}");
            }
        }
    }
}
