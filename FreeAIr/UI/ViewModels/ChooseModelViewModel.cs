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
    [Export(typeof(ChooseModelViewModel))]
    public sealed class ChooseModelViewModel : BaseViewModel
    {
        private readonly HttpClient _httpClient = new HttpClient();

        private bool _loadFreeModels;

        public ObservableCollection2<AgentJson> AgentList
        {
            get;
        }

        public AgentJson? ChosenAgent { get; set; }

        public ObservableCollection2<ModelWrapper> ModelList
        {
            get;
        }

        public string Message
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged();
            }
        }

        public ModelWrapper SelectedModel
        {
            get;
            set
            {
                field = value;

                OnPropertyChanged();
            }
        }

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

        public sealed class ModelWrapper : BaseViewModel
        {
            private bool _isSelected;

            public string ModelId
            {
                get;
            }

            public string ModelName
            {
                get;
            }

            public string SelectedMark => _isSelected ? FreeAIr.Resources.Resources.chosen : string.Empty;

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;

                    OnPropertyChanged();
                }
            }

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
