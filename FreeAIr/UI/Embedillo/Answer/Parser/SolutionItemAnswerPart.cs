using FreeAIr.BLogic.Context;
using System.IO;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class SolutionItemAnswerPart : IAnswerPart
    {
        public string FilePath
        {
            get;
        }

        public SolutionItemAnswerPart(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
        }

        public string AsPromptString()
        {
            return FilePath;
        }

        public bool IsFileExists()
        {
            return File.Exists(FilePath);
        }

        public IChatContextItem? TryCreateChatContextItem()
        {
            if (!IsFileExists())
            {
                return null;
            }

            return
                new SolutionItemChatContextItem(FilePath);
        }
    }
}
