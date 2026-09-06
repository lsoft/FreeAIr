using System.Collections.Generic;
using System.Text;

namespace FreeAIr.Llm.Streaming
{
    /// <summary>
    /// Rebuilds whole tool calls out of the fragments a streaming answer delivers.
    ///
    /// An endpoint is free to announce the id and the name of a call in one chunk and to send its
    /// arguments as a series of deltas in the chunks that follow; the protocol only promises that
    /// the fragments of one call share their index. Taking a single fragment therefore yields a
    /// call whose arguments are empty or cut in half - the tool then runs without arguments, and
    /// the broken fragment is carried back to the server with the next request, which LM Studio
    /// answers with an Internal Server Error.
    ///
    /// Both protocols stream tool calls this way, which is why this sits next to the transports
    /// rather than inside one of them.
    /// </summary>
    public sealed class ToolCallAccumulator
    {
        /// <summary>The fragments seen so far, keyed by the index the stream assigns each call.</summary>
        private readonly Dictionary<int, ToolCallParts> _byIndex = new();

        /// <summary>
        /// The indices in the order the model opened them, so that the tool calls are offered to
        /// the user in the order they were asked for rather than in hash order.
        /// </summary>
        private readonly List<int> _order = new();

        /// <summary>
        /// Feeds one stream event in. Events which say nothing about tool calls are ignored, so a
        /// reader can pass the whole stream through here without sorting it first.
        /// </summary>
        public void Append(
            LlmStreamEvent streamEvent
            )
        {
            switch (streamEvent)
            {
                case LlmToolCallOpenedEvent opened:
                    var openedParts = GetOrAdd(opened.Index);
                    //everything but the arguments is sent once, by whichever chunk opens the call
                    if (!string.IsNullOrEmpty(opened.Id))
                    {
                        openedParts.Id = opened.Id;
                    }
                    if (!string.IsNullOrEmpty(opened.Name))
                    {
                        openedParts.Name = opened.Name;
                    }
                    break;

                case LlmToolCallArgumentsDeltaEvent delta:
                    if (!string.IsNullOrEmpty(delta.PartialJson))
                    {
                        GetOrAdd(delta.Index).Arguments.Append(delta.PartialJson);
                    }
                    break;
            }
        }

        /// <summary>
        /// The complete calls, in the order the model opened them. A fragment group which never
        /// received a name is not a call the chat could execute and is dropped here rather than
        /// passed on as a tool nobody can find.
        /// </summary>
        public IReadOnlyList<LlmToolCall> Build()
        {
            var result = new List<LlmToolCall>();

            foreach (var index in _order)
            {
                var parts = _byIndex[index];
                if (string.IsNullOrEmpty(parts.Name))
                {
                    continue;
                }

                result.Add(
                    new LlmToolCall(
                        parts.Id ?? string.Empty,
                        parts.Name!,
                        //a call without arguments still has to carry a json object: an empty
                        //string is not one, and the endpoints reject it
                        parts.Arguments.ToString()
                        )
                    );
            }

            return result;
        }

        private ToolCallParts GetOrAdd(
            int index
            )
        {
            if (!_byIndex.TryGetValue(index, out var parts))
            {
                parts = new ToolCallParts();
                _byIndex.Add(index, parts);
                _order.Add(index);
            }

            return parts;
        }

        /// <summary>The fragments collected so far for one tool call, before they are merged into a complete <see cref="LlmToolCall"/>.</summary>
        private sealed class ToolCallParts
        {
            /// <summary>The call's id, sent once by the chunk that opens the call.</summary>
            public string? Id;

            /// <summary>The name of the tool being called, sent once by the chunk that opens the call.</summary>
            public string? Name;

            /// <summary>The call's arguments, accumulated across the deltas the endpoint streams for this index.</summary>
            public readonly StringBuilder Arguments = new();
        }
    }
}
