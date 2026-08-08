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
    /// <summary>
    /// Backs the Action Configure window, where a support action (a custom command shown in the
    /// Actions menu, bound to an agent, a prompt and a set of scopes) is created, edited, reordered
    /// or cloned.
    /// </summary>
    public sealed class ActionConfigureViewModel : BaseViewModel
    {
        private SupportActionJson _selectedAction;

        /// <summary>
        /// Callback invoked to close the Action Configure window, passing whether the changes
        /// should be treated as applied.
        /// </summary>
        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        /// <summary>
        /// The full set of scope checkboxes (one per <see cref="SupportScopeEnum"/> value) shown
        /// for the currently selected action.
        /// </summary>
        public ObservableCollection2<ScopeViewModel> ScopeList
        {
            get;
        }

        private readonly AgentCollectionJson _agentCollection;

        /// <summary>
        /// The persisted collection of support actions being edited by this window.
        /// </summary>
        public SupportCollectionJson ActionCollection
        {
            get;
            private set;
        }

        /// <summary>
        /// The actions listed in the window's list box, kept in sync with <see cref="ActionCollection"/>.
        /// </summary>
        public ObservableCollection2<SupportActionJson> AvailableActions
        {
            get;
        }

        /// <summary>
        /// Stored in <see cref="_selectedAction"/> rather than in the backing field of the property:
        /// the rest of the class reads that one, and a `field = value` here left it null for ever,
        /// which hid the whole editing panel and disabled every command of the window.
        /// </summary>
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


        /// <summary>
        /// Whether the editing panel should be shown; hidden until an action is selected.
        /// </summary>
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


        /// <summary>
        /// Creates a new support action, timestamped as its default name, and adds it to both the
        /// persisted collection and the list box.
        /// </summary>
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

        /// <summary>
        /// Removes the currently selected action from the collection and the list box.
        /// </summary>
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

        /// <summary>
        /// Moves the currently selected action one position earlier in the ordered action list.
        /// </summary>
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

        /// <summary>
        /// Moves the currently selected action one position later in the ordered action list.
        /// </summary>
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

        /// <summary>
        /// Duplicates the currently selected action, appending "(cloned)" to its name, and adds
        /// the copy to the collection and the list box.
        /// </summary>
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

        /// <summary>
        /// Closes the Action Configure window, keeping the edits made to the action collection.
        /// </summary>
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

        /// <summary>
        /// The name of the agent that the currently selected action invokes.
        /// </summary>
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

        /// <summary>
        /// Highlights the agent name field in red when it does not match any agent in
        /// <see cref="_agentCollection"/>, flagging a broken or stale reference.
        /// </summary>
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

        /// <summary>
        /// The names of every known Visual Studio image moniker, offered as icon choices for an action.
        /// </summary>
        public ObservableCollection2<string> MonikerList
        {
            get;
        }

        /// <summary>
        /// The name of the icon (Visual Studio <c>KnownMonikers</c> member) assigned to the currently
        /// selected action.
        /// </summary>
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

        /// <summary>
        /// The resolved <see cref="ImageMoniker"/> for <see cref="SelectedMoniker"/>, used to render
        /// the action's icon in the UI.
        /// </summary>
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

        /// <summary>
        /// The context-variable anchors (e.g. current selection, file path) that can be inserted
        /// into an action's prompt text.
        /// </summary>
        public ObservableCollection2<AnchorViewModel> AnchorList
        {
            get;
        }

        /// <summary>
        /// Appends the chosen anchor's placeholder text to the selected action's prompt.
        /// </summary>
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


        /// <summary>
        /// Builds the view model for the Action Configure window from the agent collection (used
        /// to validate agent references) and the action collection being edited.
        /// </summary>
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

        /// <summary>
        /// Refreshes <see cref="SelectedMoniker"/> from the currently selected action, defaulting
        /// to a question-mark icon when nothing is selected.
        /// </summary>
        private void UpdateSelectedMoniker()
        {
            if (_selectedAction is null)
            {
                SelectedMoniker = nameof(KnownMonikers.QuestionMark);
                return;
            }

            SelectedMoniker = _selectedAction.KnownMoniker;
        }

        /// <summary>
        /// Rebuilds <see cref="ScopeList"/> with one checkbox per <see cref="SupportScopeEnum"/>
        /// value, reflecting which scopes the currently selected action applies to.
        /// </summary>
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

        /// <summary>
        /// Rebuilds <see cref="AnchorList"/> with one entry per <see cref="SupportContextVariableEnum"/>
        /// value, offering every prompt anchor available to the currently selected action.
        /// </summary>
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

    /// <summary>
    /// Represents one context-variable placeholder (e.g. current file, selection) that can be
    /// inserted into a support action's prompt text via <see cref="ActionConfigureViewModel.AppendAnchorCommand"/>.
    /// </summary>
    public sealed class AnchorViewModel : BaseViewModel
    {
        /// <summary>
        /// The action whose prompt this anchor can be appended to.
        /// </summary>
        public SupportActionJson SelectedAction
        {
            get;
        }

        /// <summary>
        /// The context variable this anchor represents.
        /// </summary>
        public SupportContextVariableEnum Variable
        {
            get;
        }

        /// <summary>
        /// The placeholder text inserted into the prompt when this anchor is chosen.
        /// </summary>
        public string AnchorName => SupportContextVariableHelper.GetAnchor(Variable);

        /// <summary>
        /// Wraps a context variable together with the action whose prompt it can be appended to.
        /// </summary>
        public AnchorViewModel(
            SupportActionJson? selectedAction,
            SupportContextVariableEnum variable
            )
        {
            SelectedAction = selectedAction;
            Variable = variable;
        }

    }

    /// <summary>
    /// Represents one scope checkbox (e.g. applies to a file, a selection, a project) shown for a
    /// support action, and keeps the action's <c>Scopes</c> set in sync with the checkbox state.
    /// </summary>
    public sealed class ScopeViewModel : BaseViewModel
    {
        private bool _scopeChecked;

        /// <summary>
        /// The action whose scope set this checkbox edits.
        /// </summary>
        public SupportActionJson SelectedAction
        {
            get;
        }

        /// <summary>
        /// The scope value this checkbox represents.
        /// </summary>
        public SupportScopeEnum Scope
        {
            get;
        }

        /// <summary>
        /// The display name of <see cref="Scope"/>.
        /// </summary>
        public string ScopeName => Enum.GetName(typeof(SupportScopeEnum), Scope);

        /// <summary>
        /// Whether this scope is enabled for the selected action; toggling it adds or removes the
        /// scope from the action's scope set.
        /// </summary>
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

        /// <summary>
        /// Wraps a scope value together with the action it belongs to, initializing the checkbox
        /// state from whether the action already includes this scope.
        /// </summary>
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
