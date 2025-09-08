using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Testing.Authorization.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class MissingSecurityConfigurationException(string message) : Exception(message);
    [ExcludeFromCodeCoverage]
    public class ExtraConfigurationException(string message) : Exception(message);
}
