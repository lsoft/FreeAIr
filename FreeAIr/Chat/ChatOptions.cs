using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using OpenAI.Chat;
using System.Threading.Tasks;

namespace FreeAIr.Chat
{
    /// <summary>
    /// How a chat behaves: which agent answers, whether tools may be called, in which format the
    /// answer is expected, and whether the chat was started by the user or by FreeAIr itself.
    ///
    /// Use the named factory methods rather than composing the flags by hand — they encode the
    /// three combinations FreeAIr actually needs.
    /// </summary>
    public sealed class ChatOptions
    {
        /// <summary>
        /// An ordinary chat the user talks to: tools allowed, plain text answer.
        /// </summary>
        public static async Task<ChatOptions> GetDefaultAsync(AgentJson? chosenAgent) =>
            new ChatOptions(
                ChatToolChoice.CreateAutoChoice(),
                await FreeAIrOptions.DeserializeAgentCollectionAsync(),
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
                false
                );

        /// <summary>
        /// A service chat FreeAIr runs on the user's behalf (commit message, dictation clean-up,
        /// whole line completion, ...): no tools, plain text answer, and the chat is hidden from
        /// the chat list unless the user asks to see automatic chats.
        /// </summary>
        public static async Task<ChatOptions> NoToolAutoProcessedTextResponseAsync(
            AgentJson? chosenAgent
            )
        {
            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();

            return new ChatOptions(
                ChatToolChoice.CreateNoneChoice(),
                agentCollection,
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
                true
                );
        }

        /// <summary>
        /// Same as <see cref="NoToolAutoProcessedTextResponseAsync"/>, but the answer has to be
        /// parsed rather than shown — natural language outlines, search results and the like.
        ///
        /// The json is asked for in the prompt rather than through `response_format`: LM Studio
        /// accepts only `text` and `json_schema` there and answers 400 Bad Request to the
        /// `json_object` mode, while the shapes FreeAIr expects are too loose to be pinned down by
        /// a schema (the outline reader takes both an object and a bare array). Nothing is lost —
        /// every caller runs the answer through
        /// <see cref="FreeAIr.Helper.AnswerHelper.CleanupFromQuotesAndThinks"/> first, which
        /// strips the markdown fences and the reasoning blocks the mode was guarding against.
        /// </summary>
        public static async Task<ChatOptions> NoToolAutoProcessedJsonResponseAsync(
            AgentJson? chosenAgent
            )
        {
            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();

            return new ChatOptions(
                ChatToolChoice.CreateNoneChoice(),
                agentCollection,
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
                true
                );
        }

        /// <summary>Whether and how the model may call tools while answering in this chat.</summary>
        public ChatToolChoice ToolChoice
        {
            get;
        }

        /// <summary>The agents configured for the workspace, from which <see cref="ChosenAgent"/> is picked.</summary>
        public AgentCollectionJson ChatAgents
        {
            get;
        }

        /// <summary>The agent that currently answers prompts in this chat.</summary>
        public AgentJson ChosenAgent
        {
            get;
            private set;
        }

        /// <summary>The response shape requested from the model: plain text or JSON.</summary>
        public OpenAI.Chat.ChatResponseFormat ResponseFormat
        {
            get;
        }

        /// <summary>
        /// True for chats FreeAIr started on its own. Such chats are dimmed in the chat list,
        /// hidden when "show only user chats" is on, and the user cannot type into them.
        /// </summary>
        public bool AutomaticallyProcessed
        {
            get;
        }

        /// <summary>
        /// Validates and stores the option set; use the named factory methods above rather than
        /// calling this directly.
        /// </summary>
        private ChatOptions(
            ChatToolChoice toolChoice,
            AgentCollectionJson chatAgents,
            AgentJson? chosenAgent,
            OpenAI.Chat.ChatResponseFormat responseFormat,
            bool automaticallyProcessed
            )
        {
            if (toolChoice is null)
            {
                throw new ArgumentNullException(nameof(toolChoice));
            }

            if (chatAgents is null)
            {
                throw new ArgumentNullException(nameof(chatAgents));
            }

            if (responseFormat is null)
            {
                throw new ArgumentNullException(nameof(responseFormat));
            }

            ToolChoice = toolChoice;
            ChatAgents = chatAgents;
            ChosenAgent = chosenAgent ?? chatAgents.Agents[0];
            ResponseFormat = responseFormat;
            AutomaticallyProcessed = automaticallyProcessed;
        }

        /// <summary>Switches which agent answers subsequent prompts in this chat.</summary>
        public void ChangeChosenAgent(AgentJson chosenAgent)
        {
            if (chosenAgent is null)
            {
                throw new ArgumentNullException(nameof(chosenAgent));
            }

            ChosenAgent = chosenAgent;
        }
    }
}
