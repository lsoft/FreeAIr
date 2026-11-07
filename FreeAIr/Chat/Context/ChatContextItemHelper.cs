using FreeAIr.Chat.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Llm.Budget;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Chat.Context
{
    /// <summary>
    /// Splits a list of solution files into portions for the batch features - natural language
    /// outlines and natural language search - which send the files one portion at a time with the
    /// same prompt. Each portion fills the context window as far as it goes, counted in the
    /// model's tokens by the chat's <see cref="TokenCounter"/>, the same count the chat's own
    /// history is fitted with.
    /// </summary>
    public static class ChatContextItemHelper
    {
        /// <summary>
        /// Yields the portions of <paramref name="items"/>, each worth at most
        /// <paramref name="budget"/> tokens as it will be sent. With
        /// <paramref name="withRelatedItems"/> every file brings the text files its C# code refers
        /// to, and goes into a portion only together with them - a file whose references do not
        /// fit starts the next portion instead. A file which alone is over the budget still makes
        /// a portion of one; a file which no longer exists is skipped.
        /// </summary>
        public static async IAsyncEnumerable<ContextPortion> SplitIntoPortionsAsync(
            this IReadOnlyList<SolutionItemChatContextItem> items,
            bool withRelatedItems,
            int budget,
            TokenCounter tokenCounter,
            [EnumeratorCancellation] CancellationToken cancellationToken
            )
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            if (tokenCounter is null)
            {
                throw new ArgumentNullException(nameof(tokenCounter));
            }

            var portion = new PortionBudget(budget);
            var portionItems = new List<SolutionItemChatContextItem>();
            var portionRelatedItems = new List<SolutionItemChatContextItem>();

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(item.SelectedIdentifier.FilePath))
                {
                    continue;
                }

                //the file as it will be sent - line numbers, fences and all - plus its path, which
                //the outline prompt lists once more for every file of the portion
                var mainPart = new PortionPart(
                    GetPartKey(item),
                    await CountAsync(item, tokenCounter) + tokenCounter.Scale(TokenEstimator.EstimateText(item.SelectedIdentifier.FilePath))
                    );

                var relatedItems = new Dictionary<string, SolutionItemChatContextItem>(StringComparer.OrdinalIgnoreCase);
                var relatedParts = new List<PortionPart>();
                if (withRelatedItems)
                {
                    foreach (var relatedItem in await item.SearchRelatedContextItemsAsync())
                    {
                        if (FileTypeHelper.GetFileType(relatedItem.SelectedIdentifier.FilePath) != FileTypeEnum.Text)
                        {
                            continue;
                        }

                        var key = GetPartKey(relatedItem);
                        if (relatedItems.ContainsKey(key))
                        {
                            continue;
                        }

                        relatedItems.Add(key, relatedItem);
                        relatedParts.Add(new PortionPart(key, await CountAsync(relatedItem, tokenCounter)));
                    }
                }

                if (!portion.TryAdd(mainPart, relatedParts, out var acceptedParts))
                {
                    yield return new ContextPortion(portionItems, portionRelatedItems);

                    portion.Clear();
                    portionItems = new List<SolutionItemChatContextItem>();
                    portionRelatedItems = new List<SolutionItemChatContextItem>();

                    //an empty portion always takes the file
                    portion.TryAdd(mainPart, relatedParts, out acceptedParts);
                }

                portionItems.Add(item);
                portionRelatedItems.AddRange(acceptedParts.Select(p => relatedItems[p.Key]));
            }

            if (portionItems.Count > 0)
            {
                yield return new ContextPortion(portionItems, portionRelatedItems);
            }
        }

        /// <summary>The tokens one context item takes in the request, in the model's tokens as calibrated so far.</summary>
        private static async Task<int> CountAsync(
            SolutionItemChatContextItem item,
            TokenCounter tokenCounter
            )
        {
            return tokenCounter.Scale(
                TokenEstimator.EstimateMessage(await item.CreateChatMessageAsync())
                );
        }

        /// <summary>What tells two context items apart in a portion: the file, since the batch features send whole files.</summary>
        private static string GetPartKey(
            SolutionItemChatContextItem item
            )
        {
            return Path.GetFullPath(item.SelectedIdentifier.FilePath);
        }
    }

    /// <summary>
    /// One portion of a batch request: the files it is about, and the files they refer to which
    /// go into the context with them.
    /// </summary>
    public sealed class ContextPortion
    {
        /// <summary>The files the prompt of this portion is about, in the order they were given.</summary>
        public IReadOnlyList<SolutionItemChatContextItem> Items
        {
            get;
        }

        /// <summary>The files referred to by <see cref="Items"/>, each once, which fitted beside them.</summary>
        public IReadOnlyList<SolutionItemChatContextItem> RelatedItems
        {
            get;
        }

        public ContextPortion(
            IReadOnlyList<SolutionItemChatContextItem> items,
            IReadOnlyList<SolutionItemChatContextItem> relatedItems
            )
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            RelatedItems = relatedItems ?? throw new ArgumentNullException(nameof(relatedItems));
        }
    }
}
