using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using OpenAI.Chat;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public sealed class ChatOptions
    {
        public static async Task<ChatOptions> GetDefaultAsync(AgentJson? chosenAgent) =>
            new ChatOptions(
                ChatToolChoice.CreateAutoChoice(),
                await FreeAIrOptions.DeserializeAgentCollectionAsync(),
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
                false
                );

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

        public static async Task<ChatOptions> NoToolAutoProcessedJsonResponseAsync(
            AgentJson? chosenAgent
            )
        {
            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();

            return new ChatOptions(
                ChatToolChoice.CreateNoneChoice(),
                agentCollection,
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat(),
                true
                );
        }

        public ChatToolChoice ToolChoice
        {
            get;
        }

        public AgentCollectionJson ChatAgents
        {
            get;
        }

        public AgentJson ChosenAgent
        {
            get;
            private set;
        }

        public OpenAI.Chat.ChatResponseFormat ResponseFormat
        {
            get;
        }

        public bool AutomaticallyProcessed
        {
            get;
        }

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
