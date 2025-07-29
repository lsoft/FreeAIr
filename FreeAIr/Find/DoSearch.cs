using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ViewModels;

namespace FreeAIr.Find
{
    public static class DoSearch
    {
        public static async Task SearchAsync(
            bool useRAG,
            string fileTypesFilterText,
            string subjectToSearchText
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var chosenScope = await VisualStudioContextMenuCommandBridge.ShowAsync<NaturalSearchScope>(
                    FreeAIr.Resources.Resources.Choose_searching_scope,
                    [
                        (FreeAIr.Resources.Resources.Whole_solution, new NaturalSearchScope(NaturalSearchScopeEnum.WholeSolution)),
                        (FreeAIr.Resources.Resources.Current_project, new NaturalSearchScope(NaturalSearchScopeEnum.CurrentProject)),
                    ]
                    );
                if (chosenScope is null)
                {
                    return;
                }

                var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                    FreeAIr.Resources.Resources.Choose_support_action,
                    SupportScopeEnum.NaturalLanguageSearch
                    );
                if (chosenSupportAction is null)
                {
                    return;
                }


                var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                    FreeAIr.Resources.Resources.Choose_agent_for_natural_language,
                    chosenSupportAction.AgentName
                    );
                if (chosenAgent is null)
                {
                    return;
                }

                //закрываем окно поиска
                CloseFindWindow();

                await NaturalLanguageResultsViewModel.ShowPanelAsync(
                    new NaturalLanguageSearchParameters(
                        useRAG,
                        fileTypesFilterText,
                        subjectToSearchText,
                        chosenScope.Scope,
                        chosenSupportAction,
                        chosenAgent
                        )
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


    public sealed class NaturalLanguageSearchParameters
    {
        public bool UseRAG
        {
            get;
        }
        public string FileTypesFilterText
        {
            get;
        }
        public string SearchText
        {
            get;
        }
        public NaturalSearchScopeEnum ChosenScope
        {
            get;
        }
        public SupportActionJson ChosenAction
        {
            get;
        }
        public AgentJson ChosenAgent
        {
            get;
        }

        public NaturalLanguageSearchParameters(
            bool useRAG,
            string fileTypesFilterText,
            string searchText,
            NaturalSearchScopeEnum chosenScope,
            SupportActionJson chosenAction,
            AgentJson chosenAgent
            )
        {
            if (string.IsNullOrEmpty(fileTypesFilterText))
            {
                throw new ArgumentException($"'{nameof(fileTypesFilterText)}' cannot be null or empty.", nameof(fileTypesFilterText));
            }

            if (string.IsNullOrEmpty(searchText))
            {
                throw new ArgumentException($"'{nameof(searchText)}' cannot be null or empty.", nameof(searchText));
            }

            if (chosenAction is null)
            {
                throw new ArgumentNullException(nameof(chosenAction));
            }

            if (chosenAgent is null)
            {
                throw new ArgumentNullException(nameof(chosenAgent));
            }

            UseRAG = useRAG;
            FileTypesFilterText = fileTypesFilterText;
            SearchText = searchText;
            ChosenScope = chosenScope;
            ChosenAction = chosenAction;
            ChosenAgent = chosenAgent;
        }

    }
}
