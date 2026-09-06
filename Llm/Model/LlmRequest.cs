using System.Collections.Generic;

namespace FreeAIr.Llm
{
    /// <summary>
    /// One completion request, in the shape both wire protocols can be built from. The chat
    /// assembles this and hands it to an <see cref="ILlmTransport"/>; nothing above the transport
    /// knows which protocol will carry it.
    ///
    /// The system prompt is a field of the request rather than the first message on purpose. It is
    /// what Anthropic requires - there the system prompt is a top level `system` member and there
    /// is no system role at all - and it removes a whole class of mistake on the OpenAI side, where
    /// a system prompt appended to a `List&lt;ChatMessage&gt;` as a bare string silently becomes a
    /// user message.
    /// </summary>
    public sealed class LlmRequest
    {
        /// <summary>The model to ask, as the provider names it.</summary>
        public string Model
        {
            get;
        }

        /// <summary>
        /// The agent's system prompt, with the answer language already substituted, or null when
        /// the agent has none.
        /// </summary>
        public string? SystemPrompt
        {
            get;
        }

        /// <summary>The transcript in chronological order, without the system prompt.</summary>
        public IReadOnlyList<LlmMessage> Messages
        {
            get;
        }

        /// <summary>
        /// The tools offered for this turn - the MCP tools enabled in this chat, and nothing else.
        /// May be empty, in which case <see cref="ToolChoice"/> is forced to
        /// <see cref="LlmToolChoice.None"/>.
        /// </summary>
        public IReadOnlyList<LlmToolDefinition> Tools
        {
            get;
        }

        /// <summary>Whether the model may call one of <see cref="Tools"/>.</summary>
        public LlmToolChoice ToolChoice
        {
            get;
        }

        /// <summary>
        /// The cap on the answer length. Optional for OpenAI, required by Anthropic, which is why
        /// the transports differ in what they do when it is not set.
        /// </summary>
        public int? MaxOutputTokens
        {
            get;
        }

        /// <summary>
        /// Assembles a request, collapsing the tool choice to
        /// <see cref="LlmToolChoice.None"/> when no tool is on offer: some providers answer 400 to
        /// a request which asks for `auto` over an empty tool list.
        /// </summary>
        public LlmRequest(
            string model,
            string? systemPrompt,
            IReadOnlyList<LlmMessage> messages,
            IReadOnlyList<LlmToolDefinition>? tools = null,
            LlmToolChoice toolChoice = LlmToolChoice.Auto,
            int? maxOutputTokens = null
            )
        {
            if (string.IsNullOrEmpty(model))
            {
                throw new ArgumentException($"'{nameof(model)}' cannot be null or empty.", nameof(model));
            }

            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            Model = model;
            SystemPrompt = systemPrompt;
            Messages = messages;
            Tools = tools ?? Array.Empty<LlmToolDefinition>();
            ToolChoice = Tools.Count > 0
                ? toolChoice
                : LlmToolChoice.None;
            MaxOutputTokens = maxOutputTokens;
        }
    }
}
