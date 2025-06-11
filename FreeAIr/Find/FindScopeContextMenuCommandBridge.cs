using FreeAIr.UI.Bridge;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using static FreeAIr.Find.FindScopeContextMenuCommandBridge;

namespace FreeAIr.Find
{
    public sealed class FindScopeContextMenuCommandBridge : MenuCommandBridge<FindScopeContextMenuItem>
    {
        public string FileTypesFilterText
        {
            get;
            private set;
        }

        public string SubjectToSearchText
        {
            get;
            private set;
        }

        protected override int GetMenuID() => PackageIds.FindScopeContextMenu;

        public async Task ShowAsync(
            string fileTypesFilterText,
            string subjectToSearchText,
            List<FindScopeContextMenuItem> menuItems,
            int x,
            int y
            )
        {
            FileTypesFilterText = fileTypesFilterText;
            SubjectToSearchText = subjectToSearchText;

            try
            {
                await base.ShowAsync(menuItems, x, y);
            }
            finally
            {
                FileTypesFilterText = null;
                SubjectToSearchText = null;
            }
        }

        public static void Show(
            string fileTypesFilterText,
            string subjectToSearchText
            )
        {
            List<FindScopeContextMenuItem> menuItems =
                [
                    new FindScopeContextMenuItem(
                        "Whole solution",
                        NaturalSearchScopeEnum.WholeSolution
                        ),
                    new FindScopeContextMenuItem(
                        "Current project",
                        NaturalSearchScopeEnum.CurrentProject
                        )
                ];

            ThreadHelper.JoinableTaskFactory.Run(
                async () =>
                {
                    var bridge = await VS.GetRequiredServiceAsync<FindScopeContextMenuCommandBridge, FindScopeContextMenuCommandBridge>();
                    var point = new System.Windows.Point(
                        System.Windows.Forms.Cursor.Position.X,
                        System.Windows.Forms.Cursor.Position.Y
                        );
                    await bridge.ShowAsync(
                        fileTypesFilterText,
                        subjectToSearchText,
                        menuItems,
                        (int)point.X,
                        (int)point.Y
                        );
                });
        }

        public sealed class FindScopeContextMenuItem
        {
            public string Header
            {
                get;
            }

            public NaturalSearchScopeEnum Scope
            {
                get;
            }

            public FindScopeContextMenuItem(
                string header,
                NaturalSearchScopeEnum scope
                )
            {
                Header = header;
                Scope = scope;
            }

            public override string ToString()
            {
                return Header;
            }
        }
    }


}
