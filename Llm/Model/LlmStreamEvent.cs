namespace FreeAIr.Llm
{
    /// <summary>
    /// One thing that happened while the answer was streaming in.
    ///
    /// The events are deliberately as raw as the wire: text arrives in pieces and so do the
    /// arguments of a tool call, and it is <see cref="Streaming.ToolCallAccumulator"/> that puts the
    /// pieces back together. A transport which assembled them itself would have to buffer the whole
    /// answer before saying anything, and the live-updating chat window is the reason this is a
    /// stream in the first place.
    /// </summary>
    public abstract class LlmStreamEvent
    {
        private protected LlmStreamEvent()
        {
        }
    }

    /// <summary>A piece of the answer text, to be appended to what has arrived so far.</summary>
    public sealed class LlmTextDeltaEvent : LlmStreamEvent
    {
        /// <summary>The fragment of text. Never null, may be any length.</summary>
        public string Text
        {
            get;
        }

        public LlmTextDeltaEvent(
            string text
            )
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }

    /// <summary>
    /// The model has started asking for a tool: its id and name are known, its arguments are not
    /// yet. Every later fragment of the same call repeats <see cref="Index"/>.
    /// </summary>
    public sealed class LlmToolCallOpenedEvent : LlmStreamEvent
    {
        /// <summary>
        /// What ties the fragments of one call together. It is the tool call index in the OpenAI
        /// protocol and the content block index in the Anthropic one; both are stable for the
        /// duration of a response, which is all the accumulator needs.
        /// </summary>
        public int Index
        {
            get;
        }

        /// <summary>The provider's id for this call, echoed back with the result.</summary>
        public string Id
        {
            get;
        }

        /// <summary>The name of the tool being asked for.</summary>
        public string Name
        {
            get;
        }

        public LlmToolCallOpenedEvent(
            int index,
            string id,
            string name
            )
        {
            Index = index;
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
        }
    }

    /// <summary>
    /// A fragment of the JSON arguments of the call opened under the same <see cref="Index"/>.
    /// A fragment on its own is not valid JSON and must not be parsed until the stream is over.
    /// </summary>
    public sealed class LlmToolCallArgumentsDeltaEvent : LlmStreamEvent
    {
        /// <summary>Which open tool call this fragment belongs to.</summary>
        public int Index
        {
            get;
        }

        /// <summary>The fragment of the arguments JSON, to be concatenated with its siblings.</summary>
        public string PartialJson
        {
            get;
        }

        public LlmToolCallArgumentsDeltaEvent(
            int index,
            string partialJson
            )
        {
            Index = index;
            PartialJson = partialJson ?? string.Empty;
        }
    }

    /// <summary>The turn is over, and this is why. The last event of a healthy stream.</summary>
    public sealed class LlmFinishedEvent : LlmStreamEvent
    {
        /// <summary>Why the model stopped.</summary>
        public LlmFinishReason Reason
        {
            get;
        }

        public LlmFinishedEvent(
            LlmFinishReason reason
            )
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// The endpoint replied with something which is not a completion at all - an error page, a
    /// quota message, a gateway dump. There is nothing further to read and the chat is failed
    /// rather than left waiting; the text is what the user is shown.
    /// </summary>
    public sealed class LlmProtocolFaultEvent : LlmStreamEvent
    {
        /// <summary>What to show the user in place of an answer.</summary>
        public string Message
        {
            get;
        }

        public LlmProtocolFaultEvent(
            string message
            )
        {
            Message = message ?? string.Empty;
        }
    }
}
