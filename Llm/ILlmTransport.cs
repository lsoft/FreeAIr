using System.Collections.Generic;
using System.Threading;

namespace FreeAIr.Llm
{
    /// <summary>
    /// The single seam between FreeAIr and a large language model. One implementation per wire
    /// protocol - <see cref="OpenAi.OpenAiChatTransport"/> for OpenAI chat completions,
    /// <see cref="Anthropic.AnthropicMessagesTransport"/> for the Anthropic messages API - and
    /// nothing above this interface knows which of them is answering.
    ///
    /// Obtain one through <see cref="LlmTransportFactory"/>, which reads the protocol from the
    /// agent's configuration.
    /// </summary>
    public interface ILlmTransport
    {
        /// <summary>
        /// Sends one request and streams the answer back as it arrives.
        ///
        /// Everything the endpoint says which is not a completion - an error status, an HTML
        /// gateway page, a truncated stream - is reported as an
        /// <see cref="LlmProtocolFaultEvent"/> or thrown as an <see cref="LlmTransportException"/>;
        /// the caller is entitled to treat any other exception as a bug.
        /// </summary>
        IAsyncEnumerable<LlmStreamEvent> StreamAsync(
            LlmRequest request,
            CancellationToken cancellationToken
            );
    }
}
