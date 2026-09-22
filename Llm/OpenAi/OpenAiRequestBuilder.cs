using FreeAIr.Llm.Wire;
using OpenAI.Chat;
using System.Collections.Generic;

namespace FreeAIr.Llm.OpenAi
{
    /// <summary>
    /// Turns a protocol-neutral <see cref="LlmRequest"/> into what the OpenAI SDK wants: a list of
    /// <see cref="ChatMessage"/> and a <see cref="ChatCompletionOptions"/>.
    ///
    /// It is separate from the transport so that the shape of the request can be asserted without a
    /// server: what the two halves of this class produce is the whole of what FreeAIr says on the
    /// wire in the OpenAI protocol.
    /// </summary>
    internal static class OpenAiRequestBuilder
    {
        /// <summary>
        /// Flattens the request into the SDK's message list, the system prompt first.
        ///
        /// The system prompt becomes a <see cref="SystemChatMessage"/> explicitly. Appending it as a
        /// bare string, which is how this used to be done, goes through the implicit conversion on
        /// <see cref="ChatMessage"/> - and that conversion builds a <see cref="UserChatMessage"/>,
        /// so the agent's instructions travelled as something the user had said.
        /// </summary>
        public static IReadOnlyList<ChatMessage> BuildMessages(
            LlmRequest request
            )
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var result = new List<ChatMessage>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                result.Add(new SystemChatMessage(request.SystemPrompt));
            }

            foreach (var message in request.Messages)
            {
                switch (message.Role)
                {
                    case LlmRole.User:
                        result.Add(
                            new UserChatMessage(
                                ChatMessageContentPart.CreateTextPart(message.Text ?? string.Empty)
                                )
                            );
                        break;

                    case LlmRole.Assistant:
                        result.Add(BuildAssistantMessage(message));
                        break;

                    case LlmRole.Tool:
                        //this protocol has a role of its own for a tool's answer, so one neutral
                        //message becomes exactly one message here
                        var toolResult = message.ToolResult!;
                        result.Add(
                            new ToolChatMessage(
                                toolResult.ToolCallId,
                                toolResult.Text
                                )
                            );
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// The options of one turn: which tools are on offer, whether the model may reach for them,
        /// and how long the answer may be. The response format is left alone - FreeAIr asks for
        /// json in the prompt rather than through `response_format`, because LM Studio answers 400
        /// to the `json_object` mode.
        /// </summary>
        public static ChatCompletionOptions BuildOptions(
            LlmRequest request
            )
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var options = new ChatCompletionOptions
            {
                ToolChoice = request.ToolChoice == LlmToolChoice.Auto
                    ? ChatToolChoice.CreateAutoChoice()
                    : ChatToolChoice.CreateNoneChoice(),
                ResponseFormat = ChatResponseFormat.CreateTextFormat(),
                MaxOutputTokenCount = request.MaxOutputTokens,
            };

            foreach (var tool in request.Tools)
            {
                options.Tools.Add(
                    ChatTool.CreateFunctionTool(
                        functionName: tool.Name,
                        functionDescription: tool.Description,
                        functionParameters: BinaryData.FromString(
                            ToolSchemaNormalizer.Normalize(tool.ParametersJson)
                            ),
                        functionSchemaIsStrict: true
                        )
                    );
            }

            return options;
        }

        /// <summary>
        /// An assistant turn, with its tool calls when it asked for any. Text and tool calls travel
        /// in one message here, which is what the protocol expects when a model reasons aloud
        /// before reaching for a tool.
        /// </summary>
        private static AssistantChatMessage BuildAssistantMessage(
            LlmMessage message
            )
        {
            if (message.ToolCalls.Count == 0)
            {
                return new AssistantChatMessage(message.Text ?? string.Empty);
            }

            var toolCalls = new List<ChatToolCall>(message.ToolCalls.Count);
            foreach (var toolCall in message.ToolCalls)
            {
                toolCalls.Add(
                    ChatToolCall.CreateFunctionToolCall(
                        toolCall.Id,
                        toolCall.Name,
                        BinaryData.FromString(toolCall.ArgumentsJson)
                        )
                    );
            }

            var result = new AssistantChatMessage(toolCalls);

            if (!string.IsNullOrEmpty(message.Text))
            {
                result.Content.Add(
                    ChatMessageContentPart.CreateTextPart(message.Text)
                    );
            }

            return result;
        }
    }
}
