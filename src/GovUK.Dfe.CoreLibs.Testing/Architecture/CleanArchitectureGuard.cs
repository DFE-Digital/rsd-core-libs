using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NetArchTest.Rules;

namespace GovUK.Dfe.CoreLibs.Testing.Architecture
{
    /// <summary>
    /// Reusable clean-architecture dependency assertions.
    /// Each method returns a list of violations (empty = pass) so consuming
    /// tests can use their preferred assertion framework.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class CleanArchitectureGuard
    {
        /// <summary>
        /// Asserts that types in <paramref name="sourceAssembly"/> do not reference any
        /// of the <paramref name="forbiddenNamespaces"/>.
        /// Returns the list of failing type names, or an empty list if the rule passes.
        /// </summary>
        public static IReadOnlyList<string> AssertNoForbiddenDependencies(
            Assembly sourceAssembly,
            params string[] forbiddenNamespaces)
        {
            var failures = new List<string>();
            foreach (var ns in forbiddenNamespaces)
            {
                var result = Types.InAssembly(sourceAssembly)
                    .ShouldNot()
                    .HaveDependencyOn(ns)
                    .GetResult();

                if (!result.IsSuccessful && result.FailingTypeNames is not null)
                    failures.AddRange(result.FailingTypeNames);
            }
            return failures;
        }

        /// <summary>
        /// Asserts that types in the given <paramref name="namespace"/> within
        /// the calling (or specified) assembly do not reference any of the
        /// <paramref name="forbiddenNamespaces"/>.
        /// </summary>
        public static IReadOnlyList<string> AssertNamespaceDoesNotDependOn(
            string @namespace,
            params string[] forbiddenNamespaces)
        {
            var failures = new List<string>();
            foreach (var ns in forbiddenNamespaces)
            {
                var result = Types.InNamespace(@namespace)
                    .ShouldNot()
                    .HaveDependencyOn(ns)
                    .GetResult();

                if (!result.IsSuccessful && result.FailingTypeNames is not null)
                    failures.AddRange(result.FailingTypeNames);
            }
            return failures;
        }

        /// <summary>
        /// Standard four-layer validation: Domain depends on nothing, Application depends
        /// only on Domain, Infrastructure/Web can depend on Application and Domain.
        /// Returns all violations as a flat list of "<c>Layer: TypeName</c>" strings.
        /// </summary>
        public static IReadOnlyList<string> ValidateCleanLayers(
            Assembly domainAssembly,
            Assembly applicationAssembly,
            string domainNamespace,
            string applicationNamespace,
            string infrastructureNamespace,
            string webNamespace)
        {
            var violations = new List<string>();

            void Collect(string layer, IReadOnlyList<string> failures)
            {
                foreach (var f in failures)
                    violations.Add($"{layer}: {f}");
            }

            Collect("Domain", AssertNoForbiddenDependencies(
                domainAssembly, applicationNamespace, infrastructureNamespace, webNamespace));

            Collect("Application", AssertNoForbiddenDependencies(
                applicationAssembly, infrastructureNamespace, webNamespace));

            return violations;
        }
    }
}
