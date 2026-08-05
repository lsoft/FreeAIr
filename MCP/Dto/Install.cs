namespace Dto
{
    /// <summary>Asks the proxy to install a server's dependency once <see cref="IsInstalledRequest"/> has reported it missing.</summary>
    public sealed class InstallRequest : BaseRequest
    {
        public InstallRequest()
        {
        }

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
