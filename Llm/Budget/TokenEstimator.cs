using System.Collections.Generic;

namespace FreeAIr.Llm.Budget
{
    /// <summary>
    /// Guesses how many tokens a text, a message or a tool definition takes, without knowing the
    /// tokenizer. The model behind an endpoint is whatever the server says it is, so no tokenizer
    /// can be picked; the only exact count is the `usage` the server reports after the fact, and
    /// <see cref="TokenCounter"/> scales these guesses to match it.
    ///
    /// The guess is by character class rather than by length, because the ratio of characters to
    /// tokens differs several times over between English prose, code and Cyrillic. The weights are
    /// on the generous side on purpose: before the first answer this is all there is, and a
    /// request which overflows the context window fails, while one which leaves room only loses
    /// some old history.
    /// </summary>
    public static class TokenEstimator
    {
        /// <summary>
        /// What every message costs on top of its text: the role and the separators of the chat
        /// template the server wraps it in.
        /// </summary>
        public const int PerMessageOverhead = 4;

        /// <summary>
        /// What a tool call, a tool result or an advertised tool costs on top of its text: the id,
        /// the json around the arguments, the template the server renders the definition into.
        /// </summary>
        public const int PerToolOverhead = 10;

        /// <summary>
        /// The estimate for a piece of text. Latin letters and digits run about three to four
        /// characters per token, punctuation - dense in code - about two, Cyrillic and the other
        /// alphabets about two, CJK one per character.
        /// </summary>
        public static int EstimateText(
            string? text
            )
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var tokens = 0.0;
            foreach (var c in text!)
            {
                tokens += EstimateChar(c);
            }

            return (int)Math.Ceiling(tokens);
        }

        /// <summary>
        /// The estimate for one message of the transcript, whatever it carries: text, the calls of
        /// an assistant turn, or the result of a tool.
        /// </summary>
        public static int EstimateMessage(
            LlmMessage message
            )
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var result = PerMessageOverhead + EstimateText(message.Text);

            foreach (var toolCall in message.ToolCalls)
            {
                result += PerToolOverhead + EstimateText(toolCall.Name) + EstimateText(toolCall.ArgumentsJson);
            }

            if (message.ToolResult is not null)
            {
                result += PerToolOverhead + EstimateText(message.ToolResult.Text);
            }

            return result;
        }

        /// <summary>The estimate for a run of messages - one turn of the dialogue, say.</summary>
        public static int EstimateMessages(
            IEnumerable<LlmMessage> messages
            )
        {
            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var result = 0;
            foreach (var message in messages)
            {
                result += EstimateMessage(message);
            }

            return result;
        }

        /// <summary>
        /// The estimate for a tool on offer. Its json schema travels with every request, and a
        /// dozen MCP tools easily take thousands of tokens before the user has said anything.
        /// </summary>
        public static int EstimateTool(
            LlmToolDefinition tool
            )
        {
            if (tool is null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            return PerToolOverhead
                + EstimateText(tool.Name)
                + EstimateText(tool.Description)
                + EstimateText(tool.ParametersJson)
                ;
        }

        /// <summary>
        /// What the fixed part of every request costs: the system prompt and the tool definitions,
        /// which are sent whole however short the dialogue is.
        /// </summary>
        public static int EstimatePreamble(
            string? systemPrompt,
            IEnumerable<LlmToolDefinition> tools
            )
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            var result = string.IsNullOrEmpty(systemPrompt)
                ? 0
                : PerMessageOverhead + EstimateText(systemPrompt);

            foreach (var tool in tools)
            {
                result += EstimateTool(tool);
            }

            return result;
        }

        /// <summary>The share of a token one character is worth, by its class.</summary>
        private static double EstimateChar(
            char c
            )
        {
            if (c < 128)
            {
                if (c == '\n')
                {
                    //a line break usually merges with the indentation after it, but not with text
                    return 0.5;
                }
                if (char.IsWhiteSpace(c))
                {
                    //a space mostly travels inside the token of the word after it
                    return 0.1;
                }
                if (char.IsLetter(c))
                {
                    return 0.3;
                }
                if (char.IsDigit(c))
                {
                    //tokenizers split numbers into short groups or single digits
                    return 0.4;
                }

                return 0.5;
            }

            if (char.IsSurrogate(c))
            {
                //emoji and the rest of the supplementary planes: a pair of these is a token or more
                return 0.75;
            }
            if (c >= 0x2E80)
            {
                //CJK and everything after it in the basic plane
                return 1.0;
            }
            if (char.IsLetter(c))
            {
                //Cyrillic, Greek, accented Latin and the other alphabets
                return 0.5;
            }

            return 0.7;
        }
    }
}
