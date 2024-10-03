using Microsoft.AspNetCore.Authorization;

namespace DfE.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Represents a custom authorization requirement with a unique type.
    /// </summary>
    public interface ICustomAuthorizationRequirement : IAuthorizationRequirement
    {
        string Type { get; }
    }
}
