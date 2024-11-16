using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace DfE.CoreLibs.Security.Authorization.Events
{
    /// <summary>
    /// Custom cookie authentication events that reject the session cookie when the user's account is not found in the token cache.
    /// This ensures that the user is signed out if their authentication session is no longer valid.
    /// </summary>
    public class RejectSessionCookieWhenAccountNotInCacheEvents : CookieAuthenticationEvents
    {
        /// <summary>
        /// The authentication scheme used to authenticate the user.
        /// </summary>
        private string AuthenticationScheme { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RejectSessionCookieWhenAccountNotInCacheEvents"/> class.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme used by OpenID Connect.</param>
        public RejectSessionCookieWhenAccountNotInCacheEvents(string authenticationScheme)
        {
            AuthenticationScheme = authenticationScheme;
        }

        /// <summary>
        /// Invoked to validate the session cookie. If the user's account is not found in the token cache,
        /// the principal is rejected to sign out the user.
        /// </summary>
        /// <param name="context">The context containing information about the authentication session.</param>
        /// <returns>A task that represents the completion of the operation.</returns>
        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            try
            {
                var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
                await tokenAcquisition.GetAccessTokenForUserAsync(
                    scopes: ["profile"],
                    user: context.Principal,
                    authenticationScheme: AuthenticationScheme);
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
                when (AccountDoesNotExistInTokenCache(ex))
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
                logger.LogInformation(ex, "User not found in token cache during ValidatePrincipal. Rejecting principal to sign out the user.");

                // Reject the principal to sign out the user
                context.RejectPrincipal();
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
                logger.LogError(ex, "An unexpected error occurred in ValidatePrincipal.");

                throw;
            }
        }

        /// <summary>
        /// Is the exception thrown because there is no account in the token cache?
        /// </summary>
        /// <param name="ex">Exception thrown by <see cref="ITokenAcquisition"/>.GetTokenForXX methods.</param>
        /// <returns>A boolean telling if the exception was about not having an account in the cache</returns>
        private static bool AccountDoesNotExistInTokenCache(MicrosoftIdentityWebChallengeUserException ex)
        {
            return ex.InnerException is MsalUiRequiredException msalEx
                   && msalEx.ErrorCode == "user_null";
        }
    }
}
