using System;

namespace FreeAIr.Shared.Dto
{
    /// <summary>DTO describing one method/unit found in the workspace, returned by <see cref="ICodeLensListener.GetUnitInformationAsync"/> to feed the CodeLens indicator.</summary>
    public class UnitInfo
    {
        /// <summary>Roslyn project id of the project containing this unit.</summary>
        public Guid ProjectGuid
        {
            get;
            set;
        }
        /// <summary>Roslyn document id of the file containing this unit.</summary>
        public Guid DocumentGuid
        {
            get;
            set;
        }
        /// <summary>Absolute path to the file containing this unit.</summary>
        public string FilePath
        {
            get;
            set;
        }
        /// <summary>Method or member name.</summary>
        public string Name
        {
            get;
            set;
        }
        /// <summary>Source text of the unit's body.</summary>
        public string Body
        {
            get;
            set;
        }
        /// <summary>Character offset of the unit's span within the file, if known.</summary>
        public int? SpanStart
        {
            get;
            set;
        }
        /// <summary>Length of the unit's span within the file, if known.</summary>
        public int? SpanLength
        {
            get;
            set;
        }

        /// <summary>Creates a <see cref="UnitInfo"/> from the location and body of a discovered unit.</summary>
        public UnitInfo(
            Guid projectGuid,
            Guid documentGuid,
            string filePath,
            string name,
            string body,
            int? spanStart,
            int? spanLength
            )
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            ProjectGuid = projectGuid;
            DocumentGuid = documentGuid;
            FilePath = filePath;
            Name = name;
            Body = body;
            SpanStart = spanStart;
            SpanLength = spanLength;
        }
    }
}
