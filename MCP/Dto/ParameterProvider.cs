namespace Dto
{
    /// <summary>The read side of a <see cref="BaseRequest"/>'s parameter pool - what a server implementation queries by name (e.g. `MCPServerFolderPath`, `githubToken`) without needing to know it is a <see cref="BaseRequest"/> at all.</summary>
    public interface IParameterProvider
    {
        /// <summary>The value for <paramref name="key"/>; throws if it is missing.</summary>
        string this[string key]
        {
            get;
        }

        /// <summary>The value for <paramref name="key"/>, or false if it was never supplied.</summary>
        bool TryGetValue(string key, out string? value);
    }

    /// <summary>A stand-in for calls that need no parameters, so <see cref="Proxy.Server.Servers.UpdateExternalServersAsync"/> and similar callers do not have to special-case a missing pool.</summary>
    public sealed class FakeParameterProvider : IParameterProvider
    {
        /// <summary>The single shared instance, since the type carries no state of its own.</summary>
        public static readonly FakeParameterProvider Instance = new();

        /// <summary>Always throws - this provider stands in for a request that takes no parameters.</summary>
        public string this[string key] => throw new InvalidOperationException("Not applicable");

        /// <summary>Always throws - this provider stands in for a request that takes no parameters.</summary>
        public bool TryGetValue(string key, out string? value)
        {
            throw new InvalidOperationException("Not applicable");
        }
    }
}
