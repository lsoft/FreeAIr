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

        [Fact]
        public void KnownEndpointCatalog_AnthropicEntry_MatchesTheConstant()
        {
            Assert.Contains(KnownEndpointCatalog.All, e => e.Endpoint == KnownEndpointCatalog.AnthropicEndpoint);
        }

        [Fact]
        public void KnownEndpointCatalog_OnlyTheAnthropicEntryIsRecognisedAsAnthropic()
        {
            // picking an endpoint offers the protocol that goes with it; getting this wrong leaves
            // the agent speaking the other protocol, which answers with a 404
            Assert.All(
                KnownEndpointCatalog.All,
                e => Assert.Equal(
                    e.Endpoint == KnownEndpointCatalog.AnthropicEndpoint,
                    KnownEndpointCatalog.IsAnthropicEndpoint(e.Endpoint)
                    )
                );
        }

        [Fact]
        public void KnownEndpointCatalog_AnthropicIsRecognisedByHostRatherThanBySpelling()
        {
            // the same server typed by hand, with the api version the OpenAI style bases carry
            Assert.True(KnownEndpointCatalog.IsAnthropicEndpoint("https://api.anthropic.com/v1"));
            Assert.True(KnownEndpointCatalog.IsAnthropicEndpoint("https://API.Anthropic.COM"));

            Assert.False(KnownEndpointCatalog.IsAnthropicEndpoint(null));
            Assert.False(KnownEndpointCatalog.IsAnthropicEndpoint(""));
            Assert.False(KnownEndpointCatalog.IsAnthropicEndpoint("not a uri"));
            // a proxy of one's own is not the vendor's endpoint, and may serve either protocol
            Assert.False(KnownEndpointCatalog.IsAnthropicEndpoint("https://anthropic.example.com/v1"));
        }
    }
}
