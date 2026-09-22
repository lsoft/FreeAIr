namespace FreeAIr.Llm
{
    /// <summary>
    /// Which wire protocol an agent's endpoint speaks. This is the one thing about a provider
    /// FreeAIr cannot guess reliably and therefore keeps in the agent's configuration: two
    /// endpoints on the same host may speak either, and a local server answers a request in the
    /// wrong dialect with a 404 that reads like a broken installation.
    /// </summary>
    public enum LlmProtocol
    {
        /// <summary>
        /// OpenAI chat completions (`POST /chat/completions`). The default, and what every local
        /// server and most gateways implement.
        /// </summary>
        OpenAi = 0,

        /// <summary>
        /// The Anthropic messages API (`POST /v1/messages`). Claude models served by Anthropic
        /// itself speak only this, and a tool call in it looks nothing like an OpenAI one.
        /// </summary>
        Anthropic = 1
    }
}
