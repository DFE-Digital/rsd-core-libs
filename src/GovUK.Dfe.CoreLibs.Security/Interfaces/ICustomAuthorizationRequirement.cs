using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Represents a custom authorization requirement with a unique type.
    /// </summary>
    public interface ICustomAuthorizationRequirement : IAuthorizationRequirement
    {
        string Type { get; }
    }
}
