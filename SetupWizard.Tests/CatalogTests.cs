using System.Linq;
using FreeAIr.SetupWizard.Catalog;
using Xunit;

namespace FreeAIr.SetupWizard.Tests
{
    public class CatalogTests
    {
        [Fact]
        public void KnownEndpointCatalog_IsNotEmpty()
        {
            Assert.NotEmpty(KnownEndpointCatalog.All);
        }

        [Fact]
        public void KnownEndpointCatalog_HasNoDuplicateDisplayNames()
        {
            var names = KnownEndpointCatalog.All.Select(e => e.DisplayName).ToList();

            Assert.Equal(names.Count, names.Distinct().Count());
        }

        [Fact]
        public void KnownEndpointCatalog_EveryEndpointIsAValidAbsoluteUri()
        {
            Assert.All(KnownEndpointCatalog.All, e => Assert.True(System.Uri.TryCreate(e.Endpoint, System.UriKind.Absolute, out _)));
        }

        [Fact]
        public void KnownEndpointCatalog_KoboldCppEntry_MatchesTheConstant()
        {
            Assert.Contains(KnownEndpointCatalog.All, e => e.Endpoint == KnownEndpointCatalog.KoboldCppEndpoint);
        }
    }
}
