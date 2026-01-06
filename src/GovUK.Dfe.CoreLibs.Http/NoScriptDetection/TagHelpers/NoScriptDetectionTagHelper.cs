using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Internal;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace GovUK.Dfe.CoreLibs.Http.NoScriptDetection.TagHelpers
{
    [HtmlTargetElement("noscript-detection")]
    public sealed class NoScriptDetectionTagHelper : TagHelper
    {
        public override void Process(
            TagHelperContext context,
            TagHelperOutput output)
        {
            output.TagName = "noscript";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.Content.SetHtmlContent($"""
                                           <img src="{Constants.EndpointPath}"
                                                alt=""
                                                width="1"
                                                height="1"
                                                style="display:none" />
                                           """);
        }
    }
}
