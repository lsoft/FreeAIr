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

        private string _message;
        private bool _loadFreeModels;
        private ModelWrapper _selectedModel;

        private ICommand _chooseCommand;
        private ICommand _updatePageCommand;
        private AgentJson _chosenAgent;

        public ObservableCollection2<AgentJson> AgentList
        {
            get;
        }

        public AgentJson? ChosenAgent
        {
            get => _chosenAgent;
            set => _chosenAgent = value;
        }

        public ObservableCollection2<ModelWrapper> ModelList
        {
            get;
        }

        public string Message
        {
            get => _message;
            private set
            {
                _message = value;

                OnPropertyChanged();
            }
        }

        public ModelWrapper SelectedModel
        {
            get => _selectedModel;
            set
            {
                _selectedModel = value;

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
                if (_chooseCommand == null)
                {
                    _chooseCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                            ModelList.ForEach(m => m.IsSelected = false);
                            _selectedModel.IsSelected = true;

                            var agents = await FreeAIrOptions.DeserializeAgentCollectionAsync();
                            if (!_chosenAgent.Technical.IsOpenRouterAgent())
                            {
                                return;
                            }

                            _chosenAgent.Technical.ChosenModel = _selectedModel.ModelId;
                            await FreeAIrOptions.SaveAgentsAsync(agents);
                        },
                        a =>
                            _chosenAgent is not null
                            && _chosenAgent.Technical.IsOpenRouterAgent()
                            && _selectedModel is not null
                            && !_selectedModel.IsSelected
                        );
                }

                return _chooseCommand;
            }
        }
        public ICommand UpdatePageCommand
        {
            get
            {
                if (_updatePageCommand == null)
                {
                    _updatePageCommand = new RelayCommand(
                        a =>
                        {
                            Task.Run(LoadModelListAsync)
                                .FileAndForget(nameof(LoadModelListAsync));
                        }
                        );
                }

                return _updatePageCommand;
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

                var chosenModel = _chosenAgent?.Technical.ChosenModel;

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
