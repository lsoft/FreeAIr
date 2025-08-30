using System.Collections.Generic;
using static FreeAIr.Helper.SolutionHelper;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Chat.Context
{
    public static class ChatContextItemHelper
    {
        public static IEnumerable<IReadOnlyList<SolutionItemChatContextItem>> SplitByItemsSize(
            this IReadOnlyList<SolutionItemChatContextItem> contextItems,
            int maxPortionLength
            )
        {
            var portion = new List<SolutionItemChatContextItem>();

            var portionLength = 0;
            for (var itemIndex = 0; itemIndex < contextItems.Count; itemIndex++)
            {
                var contextItem = contextItems[itemIndex];

                var filePath = contextItem.SelectedIdentifier.FilePath;
                if (!System.IO.File.Exists(filePath))
                {
                    continue;
                }

                var body = System.IO.File.ReadAllText(filePath);
                var itemLength = body.Length;

                if (portionLength + itemLength > maxPortionLength && portionLength != 0)
                {
                    yield return portion;

                    portion = new();
                    portionLength = 0;
                }

                portion.Add(contextItem);
                portionLength += itemLength;
            }

            if (portion.Count > 0)
            {
                yield return portion;
            }
        }

        public static IEnumerable<IReadOnlyList<FoundSolutionItem>> SplitByItemsSize(
            this IReadOnlyList<FoundSolutionItem> contextItems,
            int maxPortionLength
            )
        {
            var portion = new List<FoundSolutionItem>();

            var portionLength = 0;
            for (var itemIndex = 0; itemIndex < contextItems.Count; itemIndex++)
            {
                var contextItem = contextItems[itemIndex];

                var filePath = contextItem.SolutionItem.FullPath;
                if (!System.IO.File.Exists(filePath))
                {
                    continue;
                }

                var body = System.IO.File.ReadAllText(filePath);
                var itemLength = body.Length;

                if (portionLength + itemLength > maxPortionLength && portionLength != 0)
                {
                    yield return portion;

                    portion = new();
                    portionLength = 0;
                }

                portion.Add(contextItem);
                portionLength += itemLength;
            }

            if (portion.Count > 0)
            {
                yield return portion;
            }
        }
    }
}
