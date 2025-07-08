using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Find
{
    public static class DoSearch
    {
        public static async Task SearchAsync(
            string fileTypesFilterText,
            string subjectToSearchText
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var chosenScope = await VisualStudioContextMenuCommandBridge.ShowAsync<NaturalSearchScope>(
                    "Choose searching scope:",
                    [
                        ("Whole solution", new NaturalSearchScope(NaturalSearchScopeEnum.WholeSolution)),
                        ("Current project", new NaturalSearchScope(NaturalSearchScopeEnum.CurrentProject)),
                    ]
                    );
                if (chosenScope is null)
                {
                    return;
                }

                var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                    "Choose support action:",
                    SupportScopeEnum.NaturalLanguageSearch
                    );
                if (chosenSupportAction is null)
                {
                    return;
                }


                var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                    "Choose agent for natural language search:",
                    chosenSupportAction.AgentName
                    );
                if (chosenAgent is null)
                {
                    return;
                }

                //закрываем окно поиска
                CloseFindWindow();

                await NaturalLanguageResultsViewModel.ShowPanelAsync(
                    fileTypesFilterText,
                    subjectToSearchText,
                    chosenScope.Scope,
                    chosenSupportAction,
                    chosenAgent
                    );
            }
            catch (Exception excp)
            {
                //todo log
            }
        }


        private static void CloseFindWindow()
        {
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window == System.Windows.Application.Current.MainWindow)
                {
                    continue;
                }

                if (window.IsActive)
                {
                    window.Close();
                }
            }
        }
    }

    public sealed class NaturalSearchScope
    {
        public NaturalSearchScopeEnum Scope
        {
            get;
        }

        public NaturalSearchScope(
            NaturalSearchScopeEnum scope
            )
        {
            Scope = scope;
        }

    }
}
