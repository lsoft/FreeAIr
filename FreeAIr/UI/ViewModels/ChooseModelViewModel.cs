using FreeAIr.BLogic.Tasks;
using FreeAIr.Dto.OpenRouter;
using FreeAIr.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
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
        private ICommand _reloadListCommand;

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
                            ApiPage.Instance.ChosenModel = _selectedModel.ModelId;
                            await ApiPage.Instance.SaveAsync();
                        },
                        a => _selectedModel is not null && !_selectedModel.IsSelected
                        );
                }

                return _chooseCommand;
            }
        }
        public ICommand ReloadListCommand
        {
            get
            {
                if (_reloadListCommand == null)
                {
                    _reloadListCommand = new RelayCommand(
                        a =>
                        {
                            Task.Run(LoadModelListAsync)
                                .FileAndForget(nameof(LoadModelListAsync));
                        }
                        );
                }

                return _reloadListCommand;
            }
        }
        


        [ImportingConstructor]
        public ChooseModelViewModel(
            )
        {
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

                ModelList.Clear();

                Message = "Model list is loading...";

                var modelContainer = await _httpClient.GetFromJsonAsync<ModelResponse>(
                    "https://openrouter.ai/api/v1/models"
                    );
                var models = modelContainer.Models
                    .Where(m => !_loadFreeModels || (m.name.Contains("(free)")))
                    .Where(m => !_loadFreeModels || (m.pricing is null || m.pricing.IsFree))
                    .ToList()
                    ;

                var chosenModel = ApiPage.Instance.ChosenModel;
                
                ModelList.AddRange(
                    models.ConvertAll(m => new ModelWrapper(m.id, m.name, m.id == chosenModel))
                    );

                Message = string.Empty;
            }
            catch (Exception excp)
            {
                Message = $"Model list cannot be loaded: {excp.Message}";

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

            public string SelectedMark => _isSelected ? "(chosen)" : string.Empty;

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
