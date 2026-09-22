using System;
using System.Collections.Generic;

namespace FreeAIr.SetupWizard.Catalog
{
    /// <summary>
    /// The endpoints offered in the agent editor's endpoint picker: well-known local servers plus a
    /// couple of common cloud providers. The user can always type their own instead - this is a
    /// starting point, not a restriction.
    /// </summary>
    public static class KnownEndpointCatalog
    {
        /// <summary>Also <c>AgentTechnical</c>'s own default endpoint, so a brand new agent and this catalog agree.</summary>
        public const string KoboldCppEndpoint = "http://localhost:5001/v1";

        /// <summary>
        /// Anthropic's own API. The only entry which is not OpenAI compatible, and the reason a
        /// known endpoint carries a protocol at all.
        /// </summary>
        public const string AnthropicEndpoint = "https://api.anthropic.com";

        public static IReadOnlyList<KnownEndpoint> All { get; } = new List<KnownEndpoint>
        {
            new KnownEndpoint("LM Studio (local)", "http://localhost:1234/v1"),
            new KnownEndpoint("KoboldCpp (local)", KoboldCppEndpoint),
            new KnownEndpoint("Ollama (local)", "http://localhost:11434/v1"),
            new KnownEndpoint("text-generation-webui (local)", "http://localhost:5000/v1"),
            new KnownEndpoint("OpenRouter", "https://openrouter.ai/api/v1"),
            new KnownEndpoint("Yandex Cloud", "https://llm.api.cloud.yandex.net/v1"),
            new KnownEndpoint("Anthropic", AnthropicEndpoint),
        };

        /// <summary>
        /// Whether an endpoint is one that speaks the Anthropic messages API rather than the OpenAI
        /// one. Everything else in this catalog, and nearly everything outside it, is OpenAI
        /// compatible.
        ///
        /// The answer is a host comparison rather than a flag on the entry, so that it holds for an
        /// endpoint the user typed by hand as well as for one they picked here - and it is only ever
        /// used to *offer* a protocol when the endpoint changes, never to overrule the one the agent
        /// is set to.
        ///
        /// This is a plain string check on purpose: the wizard's logic is a leaf assembly, and
        /// pulling the whole LLM stack into it for the sake of one enum value is not a trade worth
        /// making.
        /// </summary>
        public static bool IsAnthropicEndpoint(
            string? endpoint
            )
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return false;
            }

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return string.Compare(uri.Host, "api.anthropic.com", StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
