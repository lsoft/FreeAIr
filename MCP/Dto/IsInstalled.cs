namespace Dto
{
    /// <summary>Asks the proxy whether a server's dependency (an npm package, a docker image, ...) is already installed, before offering to install it.</summary>
    public sealed class IsInstalledRequest : BaseRequest
    {
        public IsInstalledRequest()
        {
        }

        public IsInstalledRequest(
            string mcpServerName,
            IReadOnlyDictionary<string, string>? parameters = null
            ) : base(mcpServerName, parameters)
        {
        }
    }

    /// <summary>The answer to <see cref="IsInstalledRequest"/>.</summary>
    public sealed class IsInstalledReply : BaseReply
    {
        public bool IsInstalled
        {
            get;
            set;
        }

        public IsInstalledReply()
        {
        }

        public IsInstalledReply(bool isInstalled)
        {
            IsInstalled = isInstalled;
        }
    }
}
