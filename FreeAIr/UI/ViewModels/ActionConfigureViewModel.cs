using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class ActionConfigureViewModel : BaseViewModel
    {
        private SupportActionJson _selectedAction;
        private ICommand _addNewActionCommand;
        private ICommand _deleteActionCommand;
        private ICommand _applyAndCloseCommand;
        private ICommand _addAnchorCommand;
        private ICommand _downActionCommand;
        private ICommand _upActionCommand;
        private ICommand _cloneActionCommand;

        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        public ObservableCollection2<ScopeViewModel> ScopeList
        {
            get;
        }

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
            get => _selectedAction;
            set
            {
                _selectedAction = value;

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
                if (_addNewActionCommand is null)
                {
                    _addNewActionCommand = new RelayCommand(
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

                return _addNewActionCommand;
            }
        }

        public ICommand DeleteActionCommand
        {
            get
            {
                if (_deleteActionCommand is null)
                {
                    _deleteActionCommand = new RelayCommand(
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

                return _deleteActionCommand;
            }
        }

        public ICommand UpActionCommand
        {
            get
            {
                if (_upActionCommand is null)
                {
                    _upActionCommand = new RelayCommand(
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

                return _upActionCommand;
            }
        }

        public ICommand DownActionCommand
        {
            get
            {
                if (_downActionCommand is null)
                {
                    _downActionCommand = new RelayCommand(
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

                return _downActionCommand;
            }
        }

        public ICommand CloneActionCommand
        {
            get
            {
                if (_cloneActionCommand is null)
                {
                    _cloneActionCommand = new RelayCommand(
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

                return _cloneActionCommand;
            }
        }

        public ICommand ApplyAndCloseCommand
        {
            get
            {
                if (_applyAndCloseCommand is null)
                {
                    _applyAndCloseCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (CloseWindow is not null)
                            {
                                CloseWindow(true);
                            }
                        });
                }

                return _applyAndCloseCommand;
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
                if (_addAnchorCommand is null)
                {
                    _addAnchorCommand = new RelayCommand(
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

                return _addAnchorCommand;
            }
        }


        public ActionConfigureViewModel(
            SupportCollectionJson actionCollection
            )
        {
            if (actionCollection is null)
            {
                throw new ArgumentNullException(nameof(actionCollection));
            }

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
