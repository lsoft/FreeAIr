using FreeAIr.Embedding;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ViewModels;
//`Task<T>` needs this: FreeAIrPackage.cs declares a global `Task` alias which shadows the generic one
using System.Threading.Tasks;

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

                SearchTrace.Begin(
                    $"Natural language search: RAG {useRAG}, file mask '{fileTypesFilterText}', query of {subjectToSearchText?.Length ?? 0} char(s)."
                    );

                //the file mask of `Find in Files` may be left empty, and means every file then.
                //`NaturalLanguageSearchParameters` refuses an empty one, and an exception thrown
                //here reaches nothing but the activity log - the button would simply do nothing
                if (string.IsNullOrWhiteSpace(fileTypesFilterText))
                {
                    fileTypesFilterText = "*.*";
                    SearchTrace.Step("The file mask is empty, every file of the scope is taken.");
                }

                //the button is disabled while the query box is empty, but that box is found by its
                //position in the dialog and the dialog is not ours: an empty query is a defect to
                //report, not an exception to throw into the log
                if (string.IsNullOrWhiteSpace(subjectToSearchText))
                {
                    SearchTrace.Stop("The query is empty.");
                    return;
                }

                var chosenScope = await VisualStudioContextMenuCommandBridge.ShowAsync<NaturalSearchScope>(
                    FreeAIr.Resources.Resources.Choose_searching_scope,
                    [
                        (FreeAIr.Resources.Resources.Whole_solution, new NaturalSearchScope(NaturalSearchScopeEnum.WholeSolution)),
                        (FreeAIr.Resources.Resources.Current_project, new NaturalSearchScope(NaturalSearchScopeEnum.CurrentProject)),
                    ]
                    );
                if (chosenScope is null)
                {
                    SearchTrace.Stop("No scope has been chosen.");
                    return;
                }

                SearchTrace.Step($"Scope: {chosenScope.Scope}.");

                var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                    FreeAIr.Resources.Resources.Choose_support_action,
                    SupportScopeEnum.NaturalLanguageSearch
                    );
                if (chosenSupportAction is null)
                {
                    //there is no way to tell a dismissed picker from an empty one here, and an
                    //action for this scope is a part of the default options: if it is gone, it is
                    //gone from the settings and saying so is more useful than saying nothing
                    await StopAndShowAsync(FreeAIr.Resources.Resources.Search__no_action_is_configured);
                    return;
                }

                SearchTrace.Step($"Action: {chosenSupportAction.Name}.");

                var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                    FreeAIr.Resources.Resources.Choose_agent_for_natural_language,
                    chosenSupportAction.AgentName
                    );
                if (chosenAgent is null)
                {
                    await ReportMissingChatAgentAsync();
                    return;
                }

                SearchTrace.Step($"Agent: {chosenAgent.Name}, endpoint {chosenAgent.Technical.Endpoint}, model '{chosenAgent.Technical.ChosenModel}'.");

                AgentJson? embeddingAgent = null;
                if (useRAG)
                {
                    embeddingAgent = await DetermineEmbeddingAgentAsync();
                    if (embeddingAgent is null)
                    {
                        //the user has been asked which agent to vectorize the query with and has
                        //refused. Searching the whole solution instead would be the opposite of
                        //what the checkbox was for, so nothing happens at all.
                        SearchTrace.Stop("No embedding agent, the RAG search cannot run.");
                        return;
                    }

                    SearchTrace.Step($"Embedding agent: {embeddingAgent.Name}, endpoint {embeddingAgent.Technical.Endpoint}.");
                }

                //закрываем окно поиска
                CloseFindWindow();

                SearchTrace.Step("Showing the results window.");

                await NaturalLanguageResultsViewModel.ShowPanelAsync(
                    new NaturalLanguageSearchParameters(
                        useRAG,
                        fileTypesFilterText,
                        subjectToSearchText,
                        chosenScope.Scope,
                        chosenSupportAction,
                        chosenAgent,
                        embeddingAgent
                        )
                    );
            }
            catch (Exception excp)
            {
                SearchTrace.Fail("The search has failed before it could start", excp);
            }
        }

        /// <summary>
        /// The agent picker returns null both when the user has dismissed it and when it had
        /// nothing to show at all. Only the second one is a problem the user has to be told about,
        /// and it is the likelier one: a chat agent is required to carry a token, which the agents
        /// of a locally running server normally do not.
        /// </summary>
        private static async Task ReportMissingChatAgentAsync(
            )
        {
            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();
            if (agentCollection.FilterAgents().Count > 0)
            {
                SearchTrace.Stop("No agent has been chosen.");
                return;
            }

            await StopAndShowAsync(FreeAIr.Resources.Resources.Search__no_agent_with_a_token);
        }

        private static async Task StopAndShowAsync(
            string reason
            )
        {
            SearchTrace.Stop(reason);

            await VS.MessageBox.ShowErrorAsync(
                FreeAIr.Resources.Resources.Error,
                reason
                );
        }


        /// <summary>
        /// Which agent will vectorize the search query. The query vector is only comparable with
        /// the stored ones when it comes from the same model, so the index is asked first and the
        /// user is only bothered when the index does not name an agent it can find.
        ///
        /// Deliberately resolved here, among the other modal pickers, and not later from the search
        /// itself: a dialog popping up in the middle of a running search is both a surprise and a
        /// threading hazard.
        ///
        /// Shared with the calibration window, which has exactly the same problem: whatever it
        /// measures is only meaningful when measured with the model which built the index.
        /// Requires the UI thread — it may show a dialog.
        /// </summary>
        public static async Task<AgentJson?> DetermineEmbeddingAgentAsync(
            )
        {
            var metadata = await EmbeddingIndexContainer.TryReadMetadataAsync();
            if (metadata is null)
            {
                SearchTrace.Stop(FreeAIr.Resources.Resources.RAG__there_is_no_index);

                await VS.MessageBox.ShowErrorAsync(
                    FreeAIr.Resources.Resources.Error,
                    FreeAIr.Resources.Resources.RAG__there_is_no_index
                    );
                return null;
            }

            if (!string.IsNullOrEmpty(metadata.EmbeddingAgentName))
            {
                var namedAgent = await FreeAIrOptions.DeserializeAgentByNameAsync(
                    metadata.EmbeddingAgentName!
                    );
                if (namedAgent is not null)
                {
                    return namedAgent;
                }

                SearchTrace.Step(
                    $"The index names the agent '{metadata.EmbeddingAgentName}', which the settings do not have any more."
                    );
            }

            //either the index has been built by FreeAIr 4.2 or earlier, which stored no agent at
            //all, or the agent has been renamed or removed since. Every agent is offered, not only
            //the ones with a token: an embedding model normally runs on a local server which wants
            //none, and it is the agent to pick here far more often than a cloud one
            return await AgentContextMenu.ChooseAnyAgentAsync(
                FreeAIr.Resources.Resources.RAG__choose_the_embedding_agent,
                metadata.EmbeddingAgentName
                );
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

        /// <summary>
        /// The agent which vectorizes the search query, resolved from the index metadata or picked
        /// by the user. Null when <see cref="UseRAG"/> is false.
        /// </summary>
        public AgentJson? EmbeddingAgent
        {
            get;
        }

        public NaturalLanguageSearchParameters(
            bool useRAG,
            string fileTypesFilterText,
            string searchText,
            NaturalSearchScopeEnum chosenScope,
            SupportActionJson chosenAction,
            AgentJson chosenAgent,
            AgentJson? embeddingAgent = null
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

            //a RAG search without an agent to vectorize the query with is not a RAG search
            UseRAG = useRAG && embeddingAgent is not null;
            EmbeddingAgent = embeddingAgent;
            FileTypesFilterText = fileTypesFilterText;
            SearchText = searchText;
            ChosenScope = chosenScope;
            ChosenAction = chosenAction;
            ChosenAgent = chosenAgent;
        }

    }
}
