﻿using Microsoft.AspNetCore.Http;

namespace DfE.CoreLibs.Security.Cypress
{
    public class CypressAwareAntiForgeryOptions
    {
        /// <summary>
        /// A function that, given the current <see cref="HttpContext"/>,
        /// returns <c>true</c> if antiforgery should be skipped, or <c>false</c> otherwise.
        /// </summary>
        public Func<HttpContext, bool> ShouldSkipAntiforgery { get; set; }
            = _ => false; // Default: never skip
    }
}
