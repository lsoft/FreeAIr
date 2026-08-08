namespace Dto
{
    /// <summary>Asks the proxy whether a server's dependency (an npm package, a docker image, ...) is already installed, before offering to install it.</summary>
    public sealed class IsInstalledRequest : BaseRequest
    {
        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public IsInstalledRequest()
        {
        }

        /// <summary>Creates a check for whether <paramref name="mcpServerName"/>'s dependency is installed.</summary>
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
        /// <summary>Whether the server's dependency was found already installed.</summary>
        public bool IsInstalled
        {
            get;
            set;
        }

        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public IsInstalledReply()
        {
        }

        /// <summary>Wraps the install-check outcome.</summary>
        public IsInstalledReply(bool isInstalled)
        {
            IsInstalled = isInstalled;
        }
    }
}
