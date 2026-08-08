namespace FreeAIr.SetupWizard.Catalog
{
    /// <summary>One entry of <see cref="KnownEndpointCatalog"/>: a human name and the endpoint URL it stands for.</summary>
    public sealed class KnownEndpoint
    {
        public string DisplayName { get; }

        public string Endpoint { get; }

        public KnownEndpoint(string displayName, string endpoint)
        {
            DisplayName = displayName;
            Endpoint = endpoint;
        }
    }
}
