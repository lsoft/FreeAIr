using FreeAIr.BLogic.Context.Item;
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
    public static class SolutionHelper
    {
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

        private sealed class FoundSolutionItems
        {
            public List<FoundSolutionItem> Result
            {
                get;
            }

            public HashSet<FoundSolutionItem> Uniqueness
            {
                get;
            }

            public FoundSolutionItems()
            {
                Result = new List<FoundSolutionItem>();
                Uniqueness = new HashSet<FoundSolutionItem>();
            }

            public void Add(FoundSolutionItem item)
            {
                if (Uniqueness.Contains(item))
                {
                    return;
                }

                Uniqueness.Add(item);
                Result.Add(item);
            }

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


        [DebuggerDisplay("{SolutionItem}")]
        public sealed class FoundSolutionItem
        {
            public SolutionItem SolutionItem
            {
                get;
            }
            public SelectedSpan Selection
            {
                get;
            }

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
