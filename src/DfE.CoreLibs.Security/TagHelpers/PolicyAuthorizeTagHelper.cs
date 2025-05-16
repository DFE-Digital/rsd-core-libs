using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DfE.CoreLibs.Security.TagHelpers
{
    /// <summary>
    /// A TagHelper that conditionally renders its child content based on the result of an authorization policy.
    /// </summary>
    /// <remarks>
    /// The <c>resource</c> attribute provides the resource identifier passed to <see cref="IAuthorizationService.AuthorizeAsync"/>,
    /// and the <c>policy</c> attribute specifies the name of the authorization policy to evaluate.
    /// If authorization fails or the user is not authenticated, the content is suppressed.
    /// </remarks>
    [HtmlTargetElement("authorize", Attributes = "resource,policy")]
    public class PolicyAuthorizeTagHelper(
        IAuthorizationService authService,
        IHttpContextAccessor ctx)
        : TagHelper
    {
        /// <summary>
        /// Gets or sets the resource identifier that will be passed to the authorization policy.
        /// </summary>
        [HtmlAttributeName("resource")]
        public string Resource { get; set; } = "";

        /// <summary>
        /// Gets or sets the name of the policy to evaluate for the specified resource.
        /// </summary>
        [HtmlAttributeName("policy")]
        public string Policy { get; set; } = "";

        /// <summary>
        /// Called to process the TagHelper. Suppresses output if the authorization policy does not succeed.
        /// </summary>
        /// <param name="context">The TagHelper context containing information about the current request.</param>
        /// <param name="output">The TagHelper output used to write or suppress content.</param>
        public override async Task ProcessAsync(
            TagHelperContext context,
            TagHelperOutput output)
        {
            var user = ctx.HttpContext?.User;
            if (user == null ||
                !(await authService.AuthorizeAsync(user, Resource, Policy)).Succeeded)
            {
                output.SuppressOutput();
            }
        }
    }
}