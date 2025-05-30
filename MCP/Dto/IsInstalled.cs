using System;
using System.Collections.Generic;
using System.Text;

namespace Dto
{
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
