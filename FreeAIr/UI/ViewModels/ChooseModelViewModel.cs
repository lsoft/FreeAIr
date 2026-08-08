using FreeAIr.Dto.OpenRouter;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.Shared.Helper;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Backs the Choose Model picker used to assign an OpenRouter model to an OpenRouter agent:
    /// it fetches the live model catalog from openrouter.ai and lets the user pick one for the
    /// currently selected agent.
    /// </summary>
    [Export(typeof(ChooseModelViewModel))]
    public sealed class ChooseModelViewModel : BaseViewModel
    {
        /// <summary>HTTP client used to fetch the OpenRouter model catalog.</summary>
        private readonly HttpClient _httpClient = new HttpClient();

        /// <summary>Backing field for <see cref="LoadFreeModels"/>.</summary>
        private bool _loadFreeModels;

        /// <summary>
        /// The OpenRouter agents available to have a model assigned.
        /// </summary>
        public ObservableCollection2<AgentJson> AgentList
        {
            get;
        }

        /// <summary>
        /// The agent whose model is being chosen.
        /// </summary>
        public AgentJson? ChosenAgent { get; set; }

        /// <summary>
        /// The models fetched from the OpenRouter catalog, offered for selection.
        /// </summary>
        public ObservableCollection2<ModelWrapper> ModelList
        {
            get;
        }

        /// <summary>
        /// Status text shown while the model list is loading, or an error if the fetch failed.
        /// </summary>
        public string Message
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The model currently highlighted in the list, about to be assigned to the chosen agent.
        /// </summary>
        public ModelWrapper SelectedModel
        {
            get;
            set
            {
                field = value;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the model list should be filtered down to OpenRouter's free-tier models only;
        /// changing it reloads the catalog.
        /// </summary>
        public bool LoadFreeModels
        {
            get => _loadFreeModels;
            set
            {
                if (_loadFreeModels == value)
                {
                    return;
                }

                _loadFreeModels = value;

                Task.Run(LoadModelListAsync)
                    .FileAndForget(nameof(LoadModelListAsync));

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Assigns the selected OpenRouter model to the chosen agent and persists the agent
        /// collection to FreeAIr settings.
        /// </summary>
        public ICommand ChooseCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                            ModelList.ForEach(m => m.IsSelected = false);
                            SelectedModel.IsSelected = true;

                            var agents = await FreeAIrOptions.DeserializeAgentCollectionAsync();
                            if (!ChosenAgent.Technical.IsOpenRouterAgent())
                            {
                                return;
                            }

                            ChosenAgent.Technical.ChosenModel = SelectedModel.ModelId;
                            await FreeAIrOptions.SaveAgentsAsync(agents);
                        },
                        a =>
                            ChosenAgent is not null
                            && ChosenAgent.Technical.IsOpenRouterAgent()
                            && SelectedModel is not null
                            && !SelectedModel.IsSelected
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// Reloads the OpenRouter model catalog from scratch.
        /// </summary>
        public ICommand UpdatePageCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            Task.Run(LoadModelListAsync)
                                .FileAndForget(nameof(LoadModelListAsync));
                        }
                        );
                }

                return field;
            }
        }
        


        /// <summary>
        /// Creates the Choose Model view model and kicks off the initial fetch of the OpenRouter
        /// model catalog.
        /// </summary>
        [ImportingConstructor]
        public ChooseModelViewModel(
            )
        {
            AgentList = new ObservableCollection2<AgentJson>();
            ModelList = new ObservableCollection2<ModelWrapper>();
            _loadFreeModels = true;

            Task.Run(LoadModelListAsync)
                .FileAndForget(nameof(LoadModelListAsync));
        }

        /// <summary>Refreshes the OpenRouter agent list, fetches the current model catalog from openrouter.ai, applies the free-tier filter if enabled, and updates <see cref="Message"/> with progress/errors.</summary>
        private async Task LoadModelListAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                AgentList.Clear();
                AgentList.AddRange(
                    (await FreeAIrOptions.DeserializeAsync()).AgentCollection
                        .Agents
                        .FindAll(a => a.Technical.IsOpenRouterAgent())
                    );
                ChosenAgent = null;

                ModelList.Clear();

                Message = FreeAIr.Resources.Resources.Model_list_is_loading;

                var modelContainer = await _httpClient.GetFromJsonAsync<ModelResponse>(
                    "https://openrouter.ai/api/v1/models"
                    );
                var models = modelContainer.Models
                    .Where(m => !_loadFreeModels || (m.name.Contains("(free)")))
                    .Where(m => !_loadFreeModels || (m.pricing is null || m.pricing.IsFree))
                    .ToList()
                    ;

                var chosenModel = ChosenAgent?.Technical.ChosenModel;

                ModelList.AddRange(
                    models
                        .OrderBy(m => m.name)
                        .Select(m => new ModelWrapper(m.id, m.name, m.id == chosenModel))
                    );

                Message = string.Empty;
            }
            catch (Exception excp)
            {
                Message = FreeAIr.Resources.Resources.Model_list_cannot_be_loaded + $": {excp.Message}";

                //todo
            }
        }

        /// <summary>Display adapter for one OpenRouter model entry in <see cref="ModelList"/>, tracking whether it is the agent's currently assigned model.</summary>
        public sealed class ModelWrapper : BaseViewModel
        {
            /// <summary>Backing field for <see cref="IsSelected"/>.</summary>
            private bool _isSelected;

            /// <summary>The OpenRouter model id, e.g. as sent in API requests.</summary>
            public string ModelId
            {
                get;
            }

            /// <summary>The model's display name from the OpenRouter catalog.</summary>
            public string ModelName
            {
                get;
            }

            /// <summary>Localized "chosen" marker shown next to the model when it is the agent's current model.</summary>
            public string SelectedMark => _isSelected ? FreeAIr.Resources.Resources.chosen : string.Empty;

            /// <summary>Whether this model is the one currently assigned to the chosen agent.</summary>
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;

                    OnPropertyChanged();
                }
            }

            /// <summary>Wraps one OpenRouter model catalog entry for display.</summary>
            public ModelWrapper(
                string modelId,
                string modelName,
                bool isSelected
                )
            {
                if (modelName is null)
                {
                    throw new ArgumentNullException(nameof(modelName));
                }

                ModelId = modelId;
                ModelName = modelName;
                _isSelected = isSelected;
            }

        }
    }
}
