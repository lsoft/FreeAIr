namespace Dto
{
    /// <summary>
    /// Base of every reply the MCP proxy sends back across the pipe. A non-null
    /// <see cref="ErrorMessage"/> means the call failed - callers check it before touching the rest
    /// of the reply.
    /// </summary>
    public abstract class BaseReply
    {
        public string? ErrorMessage
        {
            get;
            set;
        }

        /// <summary>Builds a reply that carries only a failure, for the common case where a call has nothing else to report.</summary>
        public static T FromError<T>(string error)
            where T : BaseReply, new()
        {
            return new T
            {
                ErrorMessage = error
            };
        }
    }

}
