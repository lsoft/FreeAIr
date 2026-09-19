using System.Text;

namespace FreeAIr.Llm.Streaming
{
    /// <summary>
    /// Turns the events of a streaming answer into the text the chat window shows, folding the
    /// reasoning of a thinking model into a `&lt;think&gt;` block around it.
    ///
    /// That tag rather than a content kind of its own because the markdown renderer already
    /// collapses it: reasoning is one click away instead of burying the answer, a saved chat needs
    /// no new shape, and a model which writes `&lt;think&gt;` into its own text - which is how
    /// DeepSeek-R1 behaves under llama.cpp - ends up looking the same as one whose server sends the
    /// reasoning in a field. <see cref="WithoutReasoning"/> takes it back out for the places which
    /// want the answer alone.
    ///
    /// The assembler is the only thing which decides this. The transports keep reasoning and answer
    /// apart (<see cref="LlmReasoningDeltaEvent"/>), and whether reasoning is shown at all is the
    /// agent's `ShowReasoning` setting, passed to the constructor.
    /// </summary>
    public sealed class AnswerTextAssembler
    {
        /// <summary>The tag a reasoning block opens with, and what the markdown renderer collapses on.</summary>
        public const string ThinkStart = "<think>";

        /// <summary>The tag a reasoning block closes with.</summary>
        public const string ThinkEnd = "</think>";

        private readonly bool _showReasoning;

        /// <summary>Whether a `&lt;think&gt;` has been written which nothing has closed yet.</summary>
        private bool _insideReasoning;

        /// <summary>
        /// Builds an assembler for one turn. <paramref name="showReasoning"/> false drops reasoning
        /// on the floor rather than hiding it, because it is the answer that is kept and a chat
        /// holding megabytes of deliberation nobody asked for is sent back to the model as history.
        /// </summary>
        public AnswerTextAssembler(
            bool showReasoning
            )
        {
            _showReasoning = showReasoning;
        }

        /// <summary>
        /// The text one event adds to the answer, which is empty for most of them. Reasoning opens
        /// the block on its first fragment, and anything which is not reasoning closes it - a model
        /// which reasons, answers, and reasons again gets a block around each run.
        /// </summary>
        public string Append(
            LlmStreamEvent streamEvent
            )
        {
            if (streamEvent is LlmReasoningDeltaEvent reasoningDelta)
            {
                if (!_showReasoning)
                {
                    return string.Empty;
                }

                if (_insideReasoning)
                {
                    return reasoningDelta.Text;
                }

                _insideReasoning = true;
                return ThinkStart + Environment.NewLine + reasoningDelta.Text;
            }

            var closing = Flush();

            if (streamEvent is LlmTextDeltaEvent textDelta)
            {
                return closing + textDelta.Text;
            }

            return closing;
        }

        /// <summary>
        /// Closes an open reasoning block, and returns nothing when there is none. Called when the
        /// stream is over, and when the turn is failed with a message of its own: an answer left
        /// holding an unclosed `&lt;think&gt;` swallows everything appended after it.
        /// </summary>
        public string Flush()
        {
            if (!_insideReasoning)
            {
                return string.Empty;
            }

            _insideReasoning = false;
            return Environment.NewLine + ThinkEnd + Environment.NewLine + Environment.NewLine;
        }

        /// <summary>
        /// The answer with every `&lt;think&gt;...&lt;/think&gt;` block removed, for the history of
        /// the next request.
        ///
        /// Last turn's reasoning is not something either protocol wants back - Anthropic replays a
        /// thinking block only as the signed block it sent, and the servers which send
        /// `reasoning_content` document that it must not be sent again - and as plain text it is
        /// context paid for twice. An unclosed block is left alone: it is the answer that is still
        /// streaming, and cutting it would take the answer with it.
        /// </summary>
        public static string WithoutReasoning(
            string answer
            )
        {
            if (string.IsNullOrEmpty(answer) || answer.IndexOf(ThinkStart, StringComparison.Ordinal) < 0)
            {
                return answer;
            }

            var result = new StringBuilder(answer.Length);
            var index = 0;

            while (index < answer.Length)
            {
                var start = answer.IndexOf(ThinkStart, index, StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                var end = answer.IndexOf(ThinkEnd, start + ThinkStart.Length, StringComparison.Ordinal);
                if (end < 0)
                {
                    break;
                }

                result.Append(answer, index, start - index);
                index = end + ThinkEnd.Length;
            }

            result.Append(answer, index, answer.Length - index);

            return result.ToString().TrimStart('\r', '\n');
        }
    }
}
