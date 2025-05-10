using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace FreeAIr.Helper
{
    public static class SolutionHelper
    {
        public static async Task<List<(SolutionItem, UI.Embedillo.Answer.Parser.SelectedSpan?)>> ProcessDownRecursivelyForAsync(
            this SolutionItem item,
            SolutionItemType type,
            string? fullPath
            )
        {
            var result = new List<(SolutionItem, UI.Embedillo.Answer.Parser.SelectedSpan?)>();

            //https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/issues/401
            item.GetItemInfo(out IVsHierarchy hierarchy, out uint itemID, out _);
            if (HierarchyUtilities.TryGetHierarchyProperty(hierarchy, itemID, (int)__VSHPROPID.VSHPROPID_IsNonMemberItem, out bool isNonMemberItem))
            {
                if (isNonMemberItem)
                {
                    // The item is not usually visible. Skip it.
                    return result;
                }
            }

            if (item.Type == type && (string.IsNullOrEmpty(fullPath) || fullPath == item.FullPath))
            {
                result.Add(
                    (item, null)
                    );

                //check for selection in this file
                var documentView = await VS.Documents.GetDocumentViewAsync(item.FullPath);
                var selection = documentView?.TextView?.Selection;
                if (selection is not null && selection.SelectedSpans.Count == 1)
                {
                    var sspan = selection.SelectedSpans[0];

                    result.Add(
                        (
                            item,
                            new UI.Embedillo.Answer.Parser.SelectedSpan(
                                sspan.Span.Start,
                                sspan.Span.Length
                                )
                            )
                        );
                }
            }

            foreach (var child in item.Children)
            {
                if (child == null)
                {
                    continue;
                }

                result.AddRange(await child.ProcessDownRecursivelyForAsync(type, fullPath));
            }

            return result;
        }

    }

}
