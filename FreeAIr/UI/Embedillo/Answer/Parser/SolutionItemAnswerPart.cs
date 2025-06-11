using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Item;
using System.IO;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class SolutionItemAnswerPart : IParsedPart
    {
        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }

        public SolutionItemAnswerPart(
            string solutionItemText
            )
        {
            if (solutionItemText is null)
            {
                throw new ArgumentNullException(nameof(solutionItemText));
            }

            SelectedIdentifier = SelectedIdentifier.Parse(solutionItemText);
        }

        public Task<string> AsPromptStringAsync()
        {
            return Task.FromResult(SelectedIdentifier.ToString());
        }

        public bool IsFileExists()
        {
            return File.Exists(SelectedIdentifier.FilePath);
        }

        public IChatContextItem? TryCreateChatContextItem()
        {
            if (!IsFileExists())
            {
                return null;
            }

            return
                new SolutionItemChatContextItem(
                    SelectedIdentifier,
                    false,
                    AddLineNumbersMode.NotRequired
                    );
        }

    }
}
