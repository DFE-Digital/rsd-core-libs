using GovUK.Dfe.CoreLibs.Testing.Authorization.Exceptions;
using DfE.CoreLibs.Utilities.Helpers;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace GovUK.Dfe.CoreLibs.Testing.Authorization.Helpers
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

            return ParseExpectedEndpointSecurityData(assembly, expectedSecurityConfig);
        }

        /// <summary>
        /// Loads page security test data from a JSON string.
        /// </summary>
        /// <param name="jsonContent">The JSON content as a string.</param>
        /// <param name="endpoints">The list of available endpoints to validate global protection.</param>
        /// <param name="globalAuthorizationEnabled">Indicate whether all pages are secured globally.</param>
        /// <returns>An enumerable of object arrays containing test data.</returns>
        public static IEnumerable<object[]> GetPageSecurityTestData(string jsonContent, IEnumerable<RouteEndpoint> endpoints, bool globalAuthorizationEnabled)
        {
            var expectedSecurityConfig = SecurityConfigLoader.LoadFromJson(jsonContent);

            return ParseExpectedPageSecurityData(expectedSecurityConfig, endpoints, globalAuthorizationEnabled);

        }

        /// <summary>
        /// Loads page security test data from a JSON file.
        /// </summary>
        /// <param name="configFilePath">The path to the JSON configuration file.</param>
        /// <param name="endpoints">The list of available endpoints to validate global protection.</param>
        /// <param name="globalAuthorizationEnabled">Indicate whether all pages are secured globally.</param>
        /// <returns>An enumerable of object arrays containing test data.</returns>
        public static IEnumerable<object[]> GetPageSecurityTestDataFromFile(string configFilePath, IEnumerable<RouteEndpoint> endpoints, bool globalAuthorizationEnabled)
        {
            var jsonContent = File.ReadAllText(configFilePath);

            return GetPageSecurityTestData(jsonContent, endpoints, globalAuthorizationEnabled);
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
            return ParseExpectedEndpointSecurityData(assembly, expectedSecurityConfig);
        }

        private static IEnumerable<object[]> ParseExpectedEndpointSecurityData(Assembly assembly, Dictionary<string, string> expectedSecurityConfig)
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

        private static IEnumerable<object[]> ParseExpectedPageSecurityData(
            Dictionary<string, string> expectedSecurityConfig,
            IEnumerable<RouteEndpoint> endpoints,
            bool globalAuthorizationEnabled)
        {
            var globalProtectionRoutes = endpoints != null
                ? endpoints
                    .Where(x => x.DisplayName != null)
                    .Select(route => new object[] { "/" + route.DisplayName?.Trim('/'), "GlobalProtection" })
                    .ToList()
                : new List<object[]>();

            if (!globalAuthorizationEnabled)
            {
                return expectedSecurityConfig
                    .Where(entry => globalProtectionRoutes.Exists(route => route[0]?.ToString() == entry.Key))
                    .Select(entry => new object[] { entry.Key, entry.Value });
            }

            foreach (var entry in expectedSecurityConfig)
            {
                var matchingRoute = globalProtectionRoutes
                    .FirstOrDefault(route => route[0].ToString()!.Equals(entry.Key, StringComparison.OrdinalIgnoreCase));

                if (matchingRoute != null)
                {
                    globalProtectionRoutes.Remove(matchingRoute);
                }
            }

            var configuredSecurityRoutes = expectedSecurityConfig
                .Select(entry => new object[] { entry.Key, entry.Value });

            return globalProtectionRoutes.Concat(configuredSecurityRoutes);
        }

    }
}
