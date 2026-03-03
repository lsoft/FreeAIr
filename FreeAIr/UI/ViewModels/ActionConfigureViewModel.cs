using FreeAIr.Helper;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class ActionConfigureViewModel : BaseViewModel
    {
        private SupportActionJson _selectedAction;

        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        public ObservableCollection2<ScopeViewModel> ScopeList
        {
            get;
        }

        private readonly AgentCollectionJson _agentCollection;

        public SupportCollectionJson ActionCollection
        {
            get;
            private set;
        }

        public ObservableCollection2<SupportActionJson> AvailableActions
        {
            get;
        }

        public SupportActionJson SelectedAction
        {
            get;
            set
            {
                field = value;

                RefillScopes();
                UpdateSelectedMoniker();
                RefillAnchors();

                OnPropertyChanged();
            }
        }


        public Visibility ShowActionPanel
        {
            get
            {
                if (_selectedAction is null)
                {
                    return Visibility.Hidden;
                }

                return Visibility.Visible;
            }
        }


        public ICommand AddNewActionCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var newAction = new SupportActionJson
                            {
                                Name = DateTime.Now.ToString("ddMMyyyy HH:mm:ss"),
                                KnownMoniker = nameof(KnownMonikers.Add),
                                Scopes = new()
                            };
                            ActionCollection.Actions.Add(newAction);
                            AvailableActions.Add(newAction);
                        });
                }

                return field;
            }
        }

        public ICommand DeleteActionCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            ActionCollection.Actions.Remove(_selectedAction);
                            AvailableActions.Remove(_selectedAction);
                        },
                        a =>
                        {
                            if (_selectedAction is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return field;
            }
        }

        public ICommand UpActionCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var index = ActionCollection.Actions.IndexOf(_selectedAction);
                            ActionCollection.Actions.RemoveAt(index);
                            ActionCollection.Actions.Insert(index - 1, _selectedAction);
                            _selectedAction = null;

                            var aa = AvailableActions[index];
                            AvailableActions.RemoveAt(index);
                            AvailableActions.Insert(index - 1, aa);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedAction is null)
                            {
                                return false;
                            }

                            var index = ActionCollection.Actions.IndexOf(_selectedAction);
                            if (index <= 0)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return field;
            }
        }

        public ICommand DownActionCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var index = ActionCollection.Actions.IndexOf(_selectedAction);
                            ActionCollection.Actions.RemoveAt(index);
                            ActionCollection.Actions.Insert(index + 1, _selectedAction);
                            _selectedAction = null;

                            var aa = AvailableActions[index];
                            AvailableActions.RemoveAt(index);
                            AvailableActions.Insert(index + 1, aa);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedAction is null)
                            {
                                return false;
                            }

                            var index = ActionCollection.Actions.IndexOf(_selectedAction);
                            if (index >= ActionCollection.Actions.Count - 1)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return field;
            }
        }

        public ICommand CloneActionCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var clone = (SupportActionJson)_selectedAction.Clone();
                            clone.Name += " (cloned)";
                            ActionCollection.Actions.Add(clone);
                            AvailableActions.Add(clone);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedAction is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return field;
            }
        }

        public ICommand ApplyAndCloseCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            if (CloseWindow is not null)
                            {
                                CloseWindow(true);
                            }
                        });
                }

                return field;
            }
        }

        public string AgentName
        {
            get
            {
                return _selectedAction?.AgentName;
            }

            set
            {
                _selectedAction?.AgentName = value;

                OnPropertyChanged();
            }
        }

        public Brush AgentNameBorder
        {
            get
            {
                if (_selectedAction is null)
                {
                    return Brushes.Transparent;
                }
                if (string.IsNullOrEmpty(_selectedAction.AgentName))
                {
                    return Brushes.Transparent;
                }

                var agent = _agentCollection.Agents.FirstOrDefault(a => a.Name == _selectedAction.AgentName);
                if (agent is not null)
                {
                    return Brushes.Transparent;
                }

                return Brushes.Red;
            }
        }

        public ObservableCollection2<string> MonikerList
        {
            get;
        }

        public string SelectedMoniker
        {
            get
            {
                return SelectedAction?.KnownMoniker;
            }

            set
            {
                if (SelectedAction is null)
                {
                    return;
                }

                SelectedAction.KnownMoniker = value;

                OnPropertyChanged();
            }
        }

        public ImageMoniker SelectedImageMoniker
        {
            get
            {
                if (_selectedAction is null)
                {
                    return KnownMonikers.QuestionMark;
                }

                return KnownMonikersHelper.GetMoniker(_selectedAction.KnownMoniker);
            }
        }

        public ObservableCollection2<AnchorViewModel> AnchorList
        {
            get;
        }

        public ICommand AppendAnchorCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var avm = a as AnchorViewModel;
                            SelectedAction.Prompt += avm.AnchorName;

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (SelectedAction is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return field;
            }
        }


        public ActionConfigureViewModel(
            AgentCollectionJson agentCollection,
            SupportCollectionJson actionCollection
            )
        {
            if (agentCollection is null)
            {
                throw new ArgumentNullException(nameof(agentCollection));
            }

            if (actionCollection is null)
            {
                throw new ArgumentNullException(nameof(actionCollection));
            }

            _agentCollection = agentCollection;
            ActionCollection = actionCollection;

            ScopeList = new ObservableCollection2<ScopeViewModel>();
            RefillScopes();

            MonikerList = new ObservableCollection2<string>(
                KnownMonikersHelper.GetAllMonikerNames()
                );
            UpdateSelectedMoniker();

            AnchorList = new ObservableCollection2<AnchorViewModel>();
            RefillAnchors();

            AvailableActions = new ObservableCollection2<SupportActionJson>(actionCollection.Actions);
        }

        private void UpdateSelectedMoniker()
        {
            if (_selectedAction is null)
            {
                SelectedMoniker = nameof(KnownMonikers.QuestionMark);
                return;
            }

            SelectedMoniker = _selectedAction.KnownMoniker;
        }

        private void RefillScopes()
        {
            ScopeList.Clear();

            var evalues = Enum.GetValues(typeof(SupportScopeEnum));
            for (var i = 0; i < evalues.Length; i++)
            {
                var scope = (SupportScopeEnum)evalues.GetValue(i);

                ScopeList.Add(
                    new ScopeViewModel(
                        _selectedAction,
                        scope
                        )
                    );
            }
        }

        private void RefillAnchors()
        {
            AnchorList.Clear();

            var evalues = Enum.GetValues(typeof(SupportContextVariableEnum));
            for (var i = 0; i < evalues.Length; i++)
            {
                var evalue = (SupportContextVariableEnum)evalues.GetValue(i);

                AnchorList.Add(
                    new AnchorViewModel(
                        _selectedAction,
                        evalue
                        )
                    );
            }
        }
    }

    public sealed class AnchorViewModel : BaseViewModel
    {
        public SupportActionJson SelectedAction
        {
            get;
        }

        public SupportContextVariableEnum Variable
        {
            get;
        }

        public string AnchorName => SupportContextVariableHelper.GetAnchor(Variable);

        public AnchorViewModel(
            SupportActionJson? selectedAction,
            SupportContextVariableEnum variable
            )
        {
            SelectedAction = selectedAction;
            Variable = variable;
        }

    }

    public sealed class ScopeViewModel : BaseViewModel
    {
        private bool _scopeChecked;

        public SupportActionJson SelectedAction
        {
            get;
        }

        public SupportScopeEnum Scope
        {
            get;
        }

        public string ScopeName => Enum.GetName(typeof(SupportScopeEnum), Scope);

        public bool ScopeChecked
        {
            get => _scopeChecked;
            set
            {
                _scopeChecked = value;

                if (SelectedAction is not null)
                {
                    if (value)
                    {
                        SelectedAction.Scopes.Add(Scope);
                    }
                    else
                    {
                        SelectedAction.Scopes.Remove(Scope);
                    }
                }
                OnPropertyChanged();
            }
        }

        public ScopeViewModel(
            SupportActionJson? selectedAction,
            SupportScopeEnum scope
            )
        {
            SelectedAction = selectedAction;
            Scope = scope;
            _scopeChecked = selectedAction?.Scopes.Contains(scope) ?? false;
        }

    }
}
