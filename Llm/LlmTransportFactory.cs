using FreeAIr.Llm.Anthropic;
using FreeAIr.Llm.OpenAi;

namespace FreeAIr.Llm
{
    /// <summary>
    /// Picks the transport for an agent. The only place in the product which maps a configured
    /// protocol onto an implementation, so that adding a third one is one case here rather than a
    /// search through the chat code.
    /// </summary>
    public static class LlmTransportFactory
    {
        /// <summary>
        /// Builds the transport for the given protocol, endpoint and token.
        /// </summary>
        public static ILlmTransport Create(
            LlmProtocol protocol,
            Uri endpoint,
            string? token
            )
        {
            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            switch (protocol)
            {
                case LlmProtocol.Anthropic:
                    return new AnthropicMessagesTransport(endpoint, token);

                case LlmProtocol.OpenAi:
                default:
                    return new OpenAiChatTransport(endpoint, token);
            }
        }
    }
}
