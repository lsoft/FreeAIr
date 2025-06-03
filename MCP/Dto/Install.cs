namespace Dto
{
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

    public sealed class InstallReply : BaseReply
    {
    }
}
