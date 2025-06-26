using Community.VisualStudio.Toolkit;
using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Tree
{
    public readonly struct OutlineDescriptor
    {
        public Guid Id
        {
            get;
        }

        public OutlineKindEnum Kind
        {
            get;
        }

        public string FullPath
        {
            get;
        }

        public string OutlineText
        {
            get;
        }

        public OutlineDescriptor(
            OutlineKindEnum kind,
            string fullPath,
            string outlineText
            )
        {
            Id = Guid.NewGuid();
            Kind = kind;
            FullPath = fullPath;
            OutlineText = outlineText;
        }
    }

    public enum OutlineKindEnum
    {
        Solution = 1,
        Project = 2,
        File = 3,
        ClassOrSimilarEntity = 4,
        MethodOfClassOrSimilarPart = 5
    }
}
