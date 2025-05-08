using System;

namespace FreeAIr.Shared.Dto
{
    public class UnitInfo
    {
        public Guid ProjectGuid
        {
            get;
            set;
        }
        public Guid DocumentGuid
        {
            get;
            set;
        }
        public string FilePath
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public string Body
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
