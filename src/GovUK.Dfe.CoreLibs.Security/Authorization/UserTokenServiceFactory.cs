using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Security.Authorization
{
    /// <summary>
    /// Factory for creating UserTokenService instances with different configurations.
    /// Allows multiple token generation strategies within a single application.
    /// </summary>
    public class UserTokenServiceFactory : IUserTokenServiceFactory
    {
        private readonly IOptionsMonitor<TokenSettings> _tokenSettingsMonitor;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTokenServiceFactory"/> class.
        /// </summary>
        /// <param name="tokenSettingsMonitor">Monitor for accessing named TokenSettings configurations.</param>
        /// <param name="loggerFactory">Factory for creating loggers.</param>
        public UserTokenServiceFactory(
            IOptionsMonitor<TokenSettings> tokenSettingsMonitor,
            ILoggerFactory loggerFactory)
        {
            _tokenSettingsMonitor = tokenSettingsMonitor;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public IUserTokenService GetService(string configurationName)
        {
            var tokenSettings = _tokenSettingsMonitor.Get(configurationName);
            var logger = _loggerFactory.CreateLogger<UserTokenService>();
            
            return new UserTokenService(
                Options.Create(tokenSettings),
                logger);
        }
    }
}

