using System;

namespace FreeAIr.Antlr.Answer.Parts
{
    [Flags]
    public enum PartTypeEnum
    {
        Text = 1,
        Xml = 2,
        Url = 4,
        Header = 8,
        CodeBlock = 16,
        CodeLine = 32,
        Image = 64,
    }
}
