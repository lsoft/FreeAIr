namespace FreeAIr.Llm.Wire
{
    /// <summary>
    /// Builds the exception a refused request becomes.
    ///
    /// The message names the protocol, the endpoint and the model, because that is the first thing
    /// worth knowing when a report says "it stopped working" - and the most likely cause of a 404
    /// or a 400 is exactly that triple being wrong for each other, an agent pointed at Anthropic
    /// while still set to speak the other protocol above all. None of it is in the status line the
    /// HTTP stack produces, and none of it is anywhere else in the log.
    ///
    /// The server's own sentence stays in <see cref="LlmTransportException.ServerMessage"/> rather
    /// than being folded in here: the chat prints the two separately, and the reader compares them
    /// to avoid saying the same thing twice.
    /// </summary>
    internal static class TransportFailure
    {
        /// <summary>Wraps a refused request, keeping the body the provider sent with it.</summary>
        public static LlmTransportException Create(
            LlmProtocol protocol,
            Uri endpoint,
            string model,
            int? statusCode,
            string? body,
            Exception? innerException = null
            )
        {
            var status = statusCode.HasValue
                ? $" Status: {statusCode.Value}."
                : string.Empty;

            return new LlmTransportException(
                $"The {protocol} endpoint {endpoint} refused the request for model '{model}'.{status}",
                ServerErrorMessageReader.Read(body),
                statusCode,
                innerException
                );
        }
    }
}
