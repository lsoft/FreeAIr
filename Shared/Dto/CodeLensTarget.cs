using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Shared.Dto
{
    public class CodeLensTarget
    {
        public Guid ProjectGuid
        {
            get;
            set;
        }
        public string FilePath
        {
            get;
            set;
        }
        public Dictionary<string, string> Context
        {
            get;
            set;
        }
        public int? SpanStart
        {
            get;
            set;
        }
        public int? SpanLength
        {
            get;
            set;
        }

        public Guid RoslynProjectIdGuid => Guid.Parse(Context["RoslynProjectIdGuid"]);

        public Guid RoslynDocumentIdGuid => Guid.Parse(Context["RoslynDocumentIdGuid"]);

        public string Name => Context["MethodName"];

        public CodeLensTarget(
            Guid projectGuid,
            string filePath,
            Dictionary<string, string> context,
            int? methodSpanStart,
            int? methodSpanLength
            )
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ProjectGuid = projectGuid;
            FilePath = filePath;
            Context = context;
            SpanStart = methodSpanStart;
            SpanLength = methodSpanLength;
        }
    }
}
