namespace FreeAIr.Llm
{
    /// <summary>
    /// A request which did not become an answer, carrying the body the endpoint sent with it.
    ///
    /// The body is the point of this type. An HTTP client's own message is only the status line -
    /// `Service request failed. Status: 400` - while the sentence the user can act on is inside the
    /// response: vLLM names the parameter it did not like, Anthropic names the missing `max_tokens`,
    /// a proxy names the exhausted quota. <see cref="ServerMessage"/> is that sentence, dug out of
    /// whichever envelope the provider wrapped it in.
    /// </summary>
    public sealed class LlmTransportException : Exception
    {
        /// <summary>
        /// The complaint of the endpoint, already unwrapped from `error.message` or a top level
        /// `message`, or the trimmed body when it is not JSON at all. Null when the failure carried
        /// no readable body.
        /// </summary>
        public string? ServerMessage
        {
            get;
        }

        /// <summary>The HTTP status code, when the failure got that far.</summary>
        public int? StatusCode
        {
            get;
        }

        public LlmTransportException(
            string message,
            string? serverMessage = null,
            int? statusCode = null,
            Exception? innerException = null
            )
            : base(message, innerException)
        {
            ServerMessage = serverMessage;
            StatusCode = statusCode;
        }
    }
}
