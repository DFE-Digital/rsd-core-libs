using AutoFixture;
using AutoFixture.AutoNSubstitute;
using GovUK.Dfe.CoreLibs.Testing.Mocks.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Testing.Mocks.RazorPages
{
    /// <summary>
    /// Base class that eliminates the boilerplate required to unit-test a Razor PageModel.
    /// Creates a fully wired <see cref="PageContext"/> with an in-memory session, default
    /// HTTP context, model state, view/temp data and AutoFixture configured with NSubstitute.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class RazorPageTestFixture<TPageModel> where TPageModel : PageModel
    {
        protected IFixture Fixture { get; }
        protected DefaultHttpContext HttpContext { get; }
        protected InMemorySession Session { get; }
        protected PageContext PageContext { get; }
        protected ModelStateDictionary ModelState => PageContext.ModelState;

        protected RazorPageTestFixture()
        {
            Fixture = new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });

            Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => Fixture.Behaviors.Remove(b));
            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            new AutoFixture.Customizations.RazorPageCustomization().Customize(Fixture);

            Session = new InMemorySession();
            HttpContext = new DefaultHttpContext
            {
                Session = Session,
                User = new ClaimsPrincipal(new ClaimsIdentity(GetDefaultClaims(), "TestAuth"))
            };

            var actionContext = new ActionContext(HttpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(), new ModelStateDictionary());
            PageContext = new PageContext(actionContext);

            Fixture.Register<ISession>(() => Session);
            Fixture.Register(() => HttpContext);
            Fixture.Register(() => PageContext);

            ConfigureFixture(Fixture);
        }

        /// <summary>
        /// Override to register additional service substitutes or customizations before the page model is created.
        /// </summary>
        protected virtual void ConfigureFixture(IFixture fixture) { }

        /// <summary>
        /// Override to supply custom claims for the test user. Defaults to a basic authenticated identity.
        /// </summary>
        protected virtual IEnumerable<Claim> GetDefaultClaims()
        {
            return new[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
        }

        /// <summary>
        /// Creates the PageModel under test, wiring up <see cref="PageContext"/>,
        /// <see cref="TempData"/> and <see cref="Url"/>.
        /// Call this at the end of your constructor or in a helper after all substitutes are registered.
        /// </summary>
        protected TPageModel CreatePageModel()
        {
            var model = Fixture.Create<TPageModel>();
            model.PageContext = PageContext;
            model.TempData = new TempDataDictionary(HttpContext, NSubstitute.Substitute.For<ITempDataProvider>());
            model.Url = NSubstitute.Substitute.For<IUrlHelper>();
            return model;
        }
    }
}
