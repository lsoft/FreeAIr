namespace Dto
{
    /// <summary>Asks the proxy to install a server's dependency once <see cref="IsInstalledRequest"/> has reported it missing.</summary>
    public sealed class InstallRequest : BaseRequest
    {
        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public InstallRequest()
        {
        }

        /// <summary>Creates an install request for <paramref name="mcpServerName"/>'s dependency.</summary>
        public InstallRequest(
            string mcpServerName,
            IReadOnlyDictionary<string, string> parameters
            ) : base(mcpServerName, parameters)
        {
        }
    }

    /// <summary>The result of an <see cref="InstallRequest"/>; success is simply the absence of <see cref="BaseReply.ErrorMessage"/>.</summary>
    public sealed class InstallReply : BaseReply
    {
    }
}
