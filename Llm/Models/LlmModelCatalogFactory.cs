namespace FreeAIr.Llm.Models
{
    /// <summary>
    /// Picks the model catalog for a protocol, the way <see cref="LlmTransportFactory"/> picks a
    /// transport. Both pickers are one switch each so that a third protocol is two cases rather
    /// than a search through the UI.
    /// </summary>
    public static class LlmModelCatalogFactory
    {
        /// <summary>
        /// How long the model list is waited for. Short on purpose: this is a picker being filled
        /// in and a reachability check being answered, not an answer being generated.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        /// <summary>Builds the catalog for the given protocol, endpoint and token.</summary>
        public static ILlmModelCatalog Create(
            LlmProtocol protocol,
            Uri endpoint,
            string? token,
            TimeSpan? timeout = null
            )
        {
            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            var effectiveTimeout = timeout ?? DefaultTimeout;

            switch (protocol)
            {
                case LlmProtocol.Anthropic:
                    return new AnthropicModelCatalog(endpoint, token, effectiveTimeout);

                case LlmProtocol.OpenAi:
                default:
                    return new OpenAiModelCatalog(endpoint, token, effectiveTimeout);
            }
        }
    }
}
