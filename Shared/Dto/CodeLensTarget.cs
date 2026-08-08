using System;
using System.Collections.Generic;

namespace FreeAIr.Shared.Dto
{
    /// <summary>
    /// Identifies the method a CodeLens indicator is attached to: cross-process DTO sent from the
    /// CodeLens data-point host to <see cref="ICodeLensListener.GetUnitInformationAsync"/> so the
    /// package can locate the same method in its own copy of the workspace.
    /// </summary>
    public class CodeLensTarget
    {
        /// <summary>Roslyn project id of the project containing the target method.</summary>
        public Guid ProjectGuid
        {
            get;
            set;
        }
        /// <summary>Absolute path to the file containing the target method.</summary>
        public string FilePath
        {
            get;
            set;
        }
        /// <summary>Extra key/value context the CodeLens data point attaches to the target, including <c>RoslynProjectIdGuid</c>, <c>RoslynDocumentIdGuid</c> and <c>MethodName</c>.</summary>
        public Dictionary<string, string> Context
        {
            get;
            set;
        }
        /// <summary>Character offset of the method's span within the file, if known.</summary>
        public int? SpanStart
        {
            get;
            set;
        }
        /// <summary>Length of the method's span within the file, if known.</summary>
        public int? SpanLength
        {
            get;
            set;
        }

        /// <summary>The Roslyn project id, round-tripped through <see cref="Context"/> since <c>ProjectId</c> itself isn't serializable across the CodeLens process boundary.</summary>
        public Guid RoslynProjectIdGuid => Guid.Parse(Context["RoslynProjectIdGuid"]);

        /// <summary>The Roslyn document id, round-tripped through <see cref="Context"/> for the same reason as <see cref="RoslynProjectIdGuid"/>.</summary>
        public Guid RoslynDocumentIdGuid => Guid.Parse(Context["RoslynDocumentIdGuid"]);

        /// <summary>The method name CodeLens is showing an indicator for.</summary>
        public string Name => Context["MethodName"];

        /// <summary>Creates a <see cref="CodeLensTarget"/> identifying one method by project, file and span.</summary>
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
