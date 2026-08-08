using FreeAIr.Shared.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Helpers for walking the Visual Studio Solution Explorer tree (via the Community Toolkit
    /// <see cref="SolutionItem"/> API), locating items by name or path, reading their current
    /// (possibly unsaved) text, and collecting the current editor selection for context building.
    /// </summary>
    public static class SolutionHelper
    {
        /// <summary>
        /// Search first solution item by its name or full path.
        /// </summary>
        public static async Task<FoundSolutionItem> FindItemByNameOrFilePathAsync(
            string nameOrPathOfItem,
            CancellationToken cancellationToken
            )
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                return null;
            }

            var items = await solution.ProcessDownRecursivelyForAsync(
                item => !item.IsNonVisibleItem && (StringComparer.InvariantCultureIgnoreCase.Compare(item.Text, nameOrPathOfItem) == 0 || StringComparer.InvariantCultureIgnoreCase.Compare(item.FullPath, nameOrPathOfItem) == 0),
                false,
                cancellationToken
                );
            var item = items.FirstOrDefault(i => !i.SolutionItem.IsNonVisibleItem);
            if (item is null)
            {
                return null;
            }

            return item;
        }

        /// <summary>
        /// If file is opened inside VS, then return text of the document vuew, even the text is not saved.
        /// Otherwise, read the document body from the disk.
        /// </summary>
        public static async Task<string> GetActualItemBodyAsync(
            string fullPath
            )
        {
            var openedDocument = await VS.Documents.GetDocumentViewAsync(
                fullPath
                );
            if (openedDocument is not null)
            {
                return openedDocument.Document.TextBuffer.CurrentSnapshot.GetText();
            }

            return System.IO.File.ReadAllText(fullPath);
        }


        /// <summary>
        /// Gets the currently open Visual Studio solution, returning <c>false</c> when no
        /// solution is open or the loaded solution has no name.
        /// </summary>
        public static bool TryGetSolution(out Solution solution)
        {
            solution = VS.Solutions.GetCurrentSolution();
            if (solution is null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(solution.Name))
            {
                solution = null;
                return false;
            }

            return true;
        }


        /// <summary>
        /// Runs <see cref="ProcessDownRecursivelyForAsync(SolutionItem, Predicate{SolutionItem}, bool, CancellationToken)"/>
        /// over every item currently selected in the Solution Explorer window, merging the results.
        /// Used to build the working set of files/selections a chat request should operate on.
        /// </summary>
        public static async System.Threading.Tasks.Task<List<FoundSolutionItem>> ProcessDownRecursivelyForSelectedAsync(
            Predicate<SolutionItem> predicate,
            bool includeSelection,
            CancellationToken cancellationToken
            )
        {
            var result = new List<FoundSolutionItem>();

            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = (await sew.GetSelectionAsync()).ToList();
            if (selections.Count == 0)
            {
                return result;
            }

            foreach (var selection in selections)
            {
                var selectionChildren = await selection.ProcessDownRecursivelyForAsync(
                    predicate,
                    includeSelection,
                    cancellationToken
                    );
                result.AddRange(selectionChildren);
            }

            return result;
        }

        /// <summary>
        /// Recursively converts a <see cref="SolutionItem"/> subtree into a caller-defined tree
        /// type <typeparamref name="T"/>, skipping non-member (not-visible) items and attaching
        /// converted children with <paramref name="childAdder"/>.
        /// </summary>
        public static T ConvertRecursivelyFor<T>(
            this SolutionItem item,
            Func<SolutionItem, T?> converter,
            Action<T, T> childAdder,
            CancellationToken cancellationToken
            )
            where T : class
        {
            //https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/issues/401
            item.GetItemInfo(out IVsHierarchy hierarchy, out uint itemID, out _);
            if (HierarchyUtilities.TryGetHierarchyProperty(hierarchy, itemID, (int)__VSHPROPID.VSHPROPID_IsNonMemberItem, out bool isNonMemberItem))
            {
                if (isNonMemberItem)
                {
                    // The item is not usually visible. Skip it.
                    return null;
                }
            }

            var root = converter(item);
            if (root is null)
            {
                return null;
            }

            foreach (var child in item.Children)
            {
                if (child == null)
                {
                    continue;
                }

                var cChild = ConvertRecursivelyFor(
                    child,
                    converter,
                    childAdder,
                    cancellationToken
                    );
                if (cChild is null)
                {
                    continue;
                }

                childAdder(root, cChild);
            }

            return root;
        }



        /// <summary>
        /// Recursively collects the solution items under <paramref name="item"/> that match one of
        /// <paramref name="types"/> and, when given, an exact <paramref name="fullPath"/>.
        /// </summary>
        public static async Task<List<FoundSolutionItem>> ProcessDownRecursivelyForAsync(
            this SolutionItem item,
            SolutionItemType[] types,
            string? fullPath,
            bool includeSelection,
            CancellationToken cancellationToken
            )
        {
            var foundItems = new FoundSolutionItems();
            await ProcessDownRecursivelyForAsync(
                foundItems,
                item,
                item => item.Type.In(types) && (string.IsNullOrEmpty(fullPath) || fullPath == item.FullPath),
                includeSelection,
                cancellationToken
                );
            return foundItems.Result;
        }

        /// <summary>
        /// Recursively collects the solution items under <paramref name="item"/> that satisfy
        /// <paramref name="predicate"/>, optionally including the current text selection of any
        /// matching open document, with the selected/open item placed first.
        /// </summary>
        public static async Task<List<FoundSolutionItem>> ProcessDownRecursivelyForAsync(
            this SolutionItem item,
            Predicate<SolutionItem> predicate,
            bool includeSelection,
            CancellationToken cancellationToken
            )
        {
            var foundItems = new FoundSolutionItems();
            await ProcessDownRecursivelyForAsync(foundItems, item, predicate, includeSelection, cancellationToken);
            return foundItems.Result;
        }

        /// <summary>
        /// Depth-first worker that walks the solution tree accumulating matches into
        /// <paramref name="foundItems"/>; shared by the public <c>ProcessDownRecursivelyForAsync</c>
        /// overloads.
        /// </summary>
        private static async Task ProcessDownRecursivelyForAsync(
            FoundSolutionItems foundItems,
            SolutionItem item,
            Predicate<SolutionItem> predicate,
            bool includeSelection,
            CancellationToken cancellationToken
            )
        {
            //https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/issues/401
            item.GetItemInfo(out IVsHierarchy hierarchy, out uint itemID, out _);
            if (HierarchyUtilities.TryGetHierarchyProperty(hierarchy, itemID, (int)__VSHPROPID.VSHPROPID_IsNonMemberItem, out bool isNonMemberItem))
            {
                if (isNonMemberItem)
                {
                    // The item is not usually visible. Skip it.
                    return;
                }
            }

            if (predicate(item))
            {
                cancellationToken.ThrowIfCancellationRequested();

                //check for selection for this file
                DocumentView? documentView = null;
                if (item.FullPath is not null)
                {
                    documentView = await VS.Documents.GetDocumentViewAsync(
                        item.FullPath
                        );
                }

                //if the document is selected, put it in the head of the list
                if (documentView is not null)
                {
                    foundItems.Insert(
                        0,
                        new (item, null)
                        );
                }
                else
                {
                    foundItems.Add(
                        new (item, null)
                        );
                }

                if (includeSelection)
                {
                    var selection = documentView?.TextView?.Selection;
                    if (selection is not null
                        && selection.SelectedSpans.Count == 1
                        )
                    {
                        var sspan = selection.SelectedSpans[0];
                        if (!sspan.IsEmpty)
                        {
                            foundItems.Insert(
                                0,
                                new(
                                    item,
                                    new UI.Embedillo.Answer.Parser.SelectedSpan(
                                        sspan.Span.Start,
                                        sspan.Span.Length
                                        )
                                    )
                                );
                        }
                    }
                }
            }

            foreach (var child in item.Children)
            {
                if (child == null)
                {
                    continue;
                }

                await ProcessDownRecursivelyForAsync(
                    foundItems,
                    child,
                    predicate,
                    includeSelection,
                    cancellationToken
                    );
            }
        }

        /// <summary>
        /// Accumulator used while walking the solution tree that keeps the found items ordered
        /// and de-duplicated (by type, path and selection) as they are added or inserted.
        /// </summary>
        private sealed class FoundSolutionItems
        {
            /// <summary>
            /// The found items collected so far, in discovery order (with selected/open items
            /// moved to the front).
            /// </summary>
            public List<FoundSolutionItem> Result
            {
                get;
            }

            /// <summary>
            /// Set mirroring <see cref="Result"/>, used to skip items already added.
            /// </summary>
            public HashSet<FoundSolutionItem> Uniqueness
            {
                get;
            }

            /// <summary>
            /// Creates an empty accumulator.
            /// </summary>
            public FoundSolutionItems()
            {
                Result = new List<FoundSolutionItem>();
                Uniqueness = new HashSet<FoundSolutionItem>();
            }

            /// <summary>
            /// Appends the item to the end of the result list unless an equal item is already present.
            /// </summary>
            public void Add(FoundSolutionItem item)
            {
                if (Uniqueness.Contains(item))
                {
                    return;
                }

                Uniqueness.Add(item);
                Result.Add(item);
            }

            /// <summary>
            /// Inserts the item at the given position (used to place the currently open/selected
            /// item first) unless an equal item is already present.
            /// </summary>
            public void Insert(int index, FoundSolutionItem item)
            {
                if (Uniqueness.Contains(item))
                {
                    return;
                }

                Uniqueness.Add(item);
                Result.Insert(index, item);
            }
        }


        /// <summary>
        /// A solution item found while walking the Solution Explorer tree, paired with an optional
        /// text selection span within it. Used to carry both "which file" and "which part of it"
        /// through the context-gathering pipeline.
        /// </summary>
        [DebuggerDisplay("{SolutionItem}")]
        public sealed class FoundSolutionItem
        {
            /// <summary>
            /// The matched solution item (a project, folder or file) from the Solution Explorer.
            /// </summary>
            public SolutionItem SolutionItem
            {
                get;
            }
            /// <summary>
            /// The selected text span within the item's document, if any was selected when found.
            /// </summary>
            public SelectedSpan Selection
            {
                get;
            }

            /// <summary>
            /// Pairs a solution item with the selection (if any) that was active in its document.
            /// </summary>
            public FoundSolutionItem(
                Community.VisualStudio.Toolkit.SolutionItem solutionItem,
                SelectedSpan? selection
                )
            {
                if (solutionItem is null)
                {
                    throw new ArgumentNullException(nameof(solutionItem));
                }

                SolutionItem = solutionItem;
                Selection = selection;
            }

            #region equality

            public override bool Equals(object obj)
            {
                return obj is FoundSolutionItem item
                    && SolutionItem.Type == item.SolutionItem.Type
                    && SolutionItem.FullPath == item.SolutionItem.FullPath
                    && ( ReferenceEquals(Selection, item.Selection) || Selection.Equals(item.Selection) )
                    ;
            }

            public override int GetHashCode()
            {
                var hashCode = 1617180218;
                hashCode = hashCode * -1521134295 + (int)SolutionItem.Type + (SolutionItem.FullPath?.GetHashCode() ?? 0);
                hashCode = hashCode * -1521134295 + (Selection?.GetHashCode() ?? 0);
                return hashCode;
            }

            #endregion
        }
    }

}
