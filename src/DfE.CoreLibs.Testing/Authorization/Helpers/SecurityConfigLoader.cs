using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Testing.Authorization.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class SecurityConfigLoader
    {
        /// <summary>
        /// Loads expected security config from a json string.
        /// </summary>
        /// <param name="json">The Json string.</param>
        /// <returns></returns>
        public static Dictionary<string, string> LoadFromJson(string json)
        {
            var config = JsonConvert.DeserializeObject<SecurityConfig>(json);

            return config!.Endpoints.ToDictionary(
                e => e.Controller == null ? e.Route! : $"{e.Controller}.{e.Action}",
                e => e.ExpectedSecurity
            );
        }

        public class PageSecurityConfig
        {
            public bool GlobalProtectAllPages { get; set; }
            public required List<EndpointSecurityConfig> Endpoints { get; set; }
        }

        public class SecurityConfig
        {
            public required List<EndpointSecurityConfig> Endpoints { get; set; }
        }

        public class EndpointSecurityConfig
        {
            public string? Route { get; set; }
            public string? Controller { get; set; }
            public string? Action { get; set; }
            public required string ExpectedSecurity { get; set; }
        }
    }
}
