namespace GovUK.Dfe.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Factory interface for creating UserTokenService instances with different configurations.
    /// Enables multiple token generation configurations within the same application.
    /// </summary>
    public interface IUserTokenServiceFactory
    {
        /// <summary>
        /// Gets a UserTokenService instance configured with the specified named configuration.
        /// </summary>
        /// <param name="configurationName">The name of the configuration to use.</param>
        /// <returns>An IUserTokenService instance configured with the specified settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the specified configuration name is not found.</exception>
        IUserTokenService GetService(string configurationName);
    }
}

