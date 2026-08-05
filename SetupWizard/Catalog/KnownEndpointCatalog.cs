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

        public static IReadOnlyList<KnownEndpoint> All { get; } = new List<KnownEndpoint>
        {
            new KnownEndpoint("LM Studio (local)", "http://localhost:1234/v1"),
            new KnownEndpoint("KoboldCpp (local)", KoboldCppEndpoint),
            new KnownEndpoint("Ollama (local)", "http://localhost:11434/v1"),
            new KnownEndpoint("text-generation-webui (local)", "http://localhost:5000/v1"),
            new KnownEndpoint("OpenRouter", "https://openrouter.ai/api/v1"),
            new KnownEndpoint("Yandex Cloud", "https://llm.api.cloud.yandex.net/v1"),
        };
    }
}
