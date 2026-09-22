using System.Collections.Generic;

namespace FreeAIr.Llm.Anthropic
{
    /// <summary>
    /// Rearranges the chat's transcript into the message list the Anthropic API accepts.
    ///
    /// This is the one part of the protocol which is not a rename. FreeAIr records a tool call and
    /// its answer as a pair, so two tools of one turn produce assistant/result/assistant/result;
    /// here every `tool_use` of a turn belongs to a single assistant message and every `tool_result`
    /// to the single user message which answers it. On top of that the conversation has to begin
    /// with the user, and a run of user messages - which is what attaching context documents
    /// produces on every single request - is better sent as one message of several blocks.
    ///
    /// Getting this wrong does not degrade the answer, it fails the request outright, which is why
    /// it is a class of its own with tests rather than a loop inside the writer.
    /// </summary>
    internal static class AnthropicTranscript
    {
        /// <summary>One message as this protocol sees it: a role and the blocks under it.</summary>
        public sealed class Message
        {
            /// <summary>`user` or `assistant` - the only two roles the protocol has.</summary>
            public string Role
            {
                get;
            }

            /// <summary>The content blocks, in the order they are written.</summary>
            public List<Block> Blocks
            {
                get;
            } = new();

            public Message(string role)
            {
                Role = role;
            }
        }

        /// <summary>One content block. Which members matter is decided by <see cref="Kind"/>.</summary>
        public sealed class Block
        {
            public BlockKind Kind;
            public string? Text;
            public string? ToolCallId;
            public string? ToolName;
            public string? ArgumentsJson;
            public bool IsError;

            public static Block CreateText(string text) =>
                new() { Kind = BlockKind.Text, Text = text };

            public static Block CreateToolUse(LlmToolCall toolCall) =>
                new()
                {
                    Kind = BlockKind.ToolUse,
                    ToolCallId = toolCall.Id,
                    ToolName = toolCall.Name,
                    ArgumentsJson = toolCall.ArgumentsJson,
                };

            public static Block CreateToolResult(LlmToolResult toolResult) =>
                new()
                {
                    Kind = BlockKind.ToolResult,
                    ToolCallId = toolResult.ToolCallId,
                    Text = toolResult.Text,
                    IsError = toolResult.IsError,
                };
        }

        /// <summary>The three block kinds FreeAIr ever sends.</summary>
        public enum BlockKind
        {
            Text,
            ToolUse,
            ToolResult
        }

        private const string RoleUser = "user";
        private const string RoleAssistant = "assistant";

        /// <summary>
        /// Turns the neutral transcript into this protocol's messages, collapsing each turn's tool
        /// calls and their results, merging runs of one role and dropping anything before the first
        /// thing the user said.
        /// </summary>
        public static IReadOnlyList<Message> Build(
            IReadOnlyList<LlmMessage> messages
            )
        {
            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var collapsed = CollapseToolTurns(messages);
            var merged = MergeConsecutiveRoles(collapsed);

            return DropUntilFirstUser(merged);
        }

        /// <summary>
        /// Walks the transcript, and wherever a run of tool calls and tool results stands, emits one
        /// assistant message carrying every call of that run followed by one user message carrying
        /// every result. Such a run is exactly one turn: the chat does not ask the model again until
        /// every tool of the turn has finished.
        /// </summary>
        private static List<Message> CollapseToolTurns(
            IReadOnlyList<LlmMessage> messages
            )
        {
            var result = new List<Message>();

            var index = 0;
            while (index < messages.Count)
            {
                var message = messages[index];

                if (message.Role == LlmRole.Assistant && message.ToolCalls.Count > 0)
                {
                    var calls = new Message(RoleAssistant);
                    var results = new Message(RoleUser);

                    while (index < messages.Count && IsPartOfToolTurn(messages[index]))
                    {
                        var current = messages[index];
                        if (current.Role == LlmRole.Tool)
                        {
                            results.Blocks.Add(Block.CreateToolResult(current.ToolResult!));
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(current.Text))
                            {
                                calls.Blocks.Add(Block.CreateText(current.Text!));
                            }

                            foreach (var toolCall in current.ToolCalls)
                            {
                                calls.Blocks.Add(Block.CreateToolUse(toolCall));
                            }
                        }

                        index++;
                    }

                    result.Add(calls);
                    if (results.Blocks.Count > 0)
                    {
                        result.Add(results);
                    }

                    continue;
                }

                if (message.Role == LlmRole.Tool)
                {
                    //a result whose call is not in the transcript any more answers nothing; sending
                    //it names a tool_use_id the model never issued and fails the whole request
                    LlmDiagnostics.Report(
                        $"The result of tool call {message.ToolResult?.ToolCallId} was dropped from the request: "
                        + "no assistant message in the transcript asks for that call. "
                        + "The model will not see what the tool answered."
                        );

                    index++;
                    continue;
                }

                if (!string.IsNullOrEmpty(message.Text))
                {
                    var plain = new Message(message.Role == LlmRole.Assistant ? RoleAssistant : RoleUser);
                    plain.Blocks.Add(Block.CreateText(message.Text!));
                    result.Add(plain);
                }

                index++;
            }

            return result;
        }

        /// <summary>Whether this message belongs to a run of tool calls and their answers.</summary>
        private static bool IsPartOfToolTurn(
            LlmMessage message
            )
        {
            return
                message.Role == LlmRole.Tool
                || (message.Role == LlmRole.Assistant && message.ToolCalls.Count > 0)
                ;
        }

        /// <summary>
        /// Joins neighbouring messages of the same role into one. A tool-result message followed by
        /// the user's next prompt merges this way too, which puts the results first inside it -
        /// exactly where the protocol wants them.
        /// </summary>
        private static List<Message> MergeConsecutiveRoles(
            List<Message> messages
            )
        {
            var result = new List<Message>();

            foreach (var message in messages)
            {
                if (message.Blocks.Count == 0)
                {
                    continue;
                }

                if (result.Count > 0 && result[result.Count - 1].Role == message.Role)
                {
                    result[result.Count - 1].Blocks.AddRange(message.Blocks);
                    continue;
                }

                result.Add(message);
            }

            return result;
        }

        /// <summary>
        /// Drops everything before the first user message. The API rejects a conversation which
        /// opens with the assistant, and archiving can leave the transcript starting anywhere.
        /// </summary>
        private static List<Message> DropUntilFirstUser(
            List<Message> messages
            )
        {
            var first = messages.FindIndex(m => m.Role == RoleUser);
            if (first <= 0)
            {
                return first == 0
                    ? messages
                    : new List<Message>();
            }

            return messages.GetRange(first, messages.Count - first);
        }
    }
}
