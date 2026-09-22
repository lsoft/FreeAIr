using FreeAIr.MCP.McpServerProxy;
using Dto;
using FreeAIr.Helper;
using FreeAIr.Llm;
using FreeAIr.Llm.Models;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.SetupWizard.Catalog;
using FreeAIr.SetupWizard.Helper;
using FreeAIr.SetupWizard.Navigation;
using FreeAIr.SetupWizard.Validation;
using FreeAIr.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>Where the wizard's working <see cref="FreeAIrOptions"/> starts from - offered on <see cref="WizardStep.StartingPoint"/>.</summary>
    public enum WizardStartingPointChoice
    {
        BuiltInDefaults,
        Blank,
        ExistingConfig,
    }

    /// <summary>
    /// Backs the setup wizard window: walks the user through <see cref="WizardNavigator"/>'s step
    /// sequence, building up a <see cref="FreeAIrOptions"/> that is written out when they finish.
    ///
    /// MCP servers and actions are edited by opening the existing
    /// <see cref="McpServerConfigureWindow"/>/<see cref="ActionConfigureWindow"/> against a clone of
    /// the relevant node (so a cancelled sub-dialog leaves the wizard's data untouched) - this view
    /// model does not reimplement either editor. The agents step is the one place with genuinely new
    /// UI (a known-endpoint picker and an environment-variable token toggle), so it is edited here
    /// directly against <see cref="Options"/>' own <see cref="AgentJson"/> list.
    ///
    /// Every string the window shows comes from <see cref="Resources.Resources"/>, like the rest of
    /// the product - the pure <c>FreeAIr.SetupWizard</c> project carries no resources of its own and
    /// therefore reports its findings as enum codes which <see cref="Describe"/> renders here.
    /// </summary>
    public sealed class SetupWizardViewModel : BaseViewModel
    {
        /// <summary>
        /// The scope of the one action which has a switch of its own, <see
        /// cref="IsWholeLineCompletionEnabled"/>. The shipped defaults leave it bound to a
        /// deliberately invalid agent name, and the wizard keeps it that way until the switch is
        /// on: whole line completion fires on every keystroke, so enabling it has to be a decision
        /// the user makes rather than a side effect of pressing the defaults button.
        /// </summary>
        private const SupportScopeEnum SwitchedScope = SupportScopeEnum.WholeLineCompletion;

        private readonly WizardNavigator _navigator;
        private AgentJson? _selectedAgent;
        private string? _destinationDescription;

        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        public bool IsFirstRun
        {
            get;
        }

        public FreeAIrOptions Options
        {
            get;
            private set;
        }

        public WizardStartingPointChoice SelectedStartingPoint
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged();
            }
        } = WizardStartingPointChoice.BuiltInDefaults;

        public bool IsBuiltInDefaultsSelected
        {
            get => SelectedStartingPoint == WizardStartingPointChoice.BuiltInDefaults;
            set { if (value) SelectedStartingPoint = WizardStartingPointChoice.BuiltInDefaults; }
        }

        public bool IsBlankSelected
        {
            get => SelectedStartingPoint == WizardStartingPointChoice.Blank;
            set { if (value) SelectedStartingPoint = WizardStartingPointChoice.Blank; }
        }

        public bool IsExistingConfigSelected
        {
            get => SelectedStartingPoint == WizardStartingPointChoice.ExistingConfig;
            set { if (value) SelectedStartingPoint = WizardStartingPointChoice.ExistingConfig; }
        }

        public ObservableCollection2<AgentJson> AvailableAgents
        {
            get;
            private set;
        }

        public AgentJson? SelectedAgent
        {
            get => _selectedAgent;
            set
            {
                _selectedAgent = value;
                OnPropertyChanged();
            }
        }

        public Visibility ShowAgentPanel => SelectedAgent is null ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// On a first run the agents step wipes the built-in placeholder agents (see the
        /// constructor) and walks the user through creating this single agent field by field,
        /// instead of reusing the multi-agent list editor. Always present so the guided page's
        /// bindings resolve even before <see cref="WizardStep.Agents"/> is reached.
        /// </summary>
        public AgentJson GuidedAgent
        {
            get;
        }

        public Visibility GuidedAgentVisibility => IsFirstRun ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AgentListVisibility => IsFirstRun ? Visibility.Collapsed : Visibility.Visible;

        public ObservableCollection2<KnownEndpoint> KnownEndpoints { get; } = new(KnownEndpointCatalog.All);

        /// <summary>
        /// The wire protocols an agent may be set to. Picking a known endpoint sets this too, so the
        /// box is here for the user who types an endpoint of their own.
        /// </summary>
        public IReadOnlyList<LlmProtocol> AvailableProtocols { get; } =
            (LlmProtocol[])Enum.GetValues(typeof(LlmProtocol));

        /// <summary>
        /// Not a persisted selection - picking an entry here copies its URL into
        /// <see cref="SelectedAgent"/>'s endpoint, which stays freely editable in its own textbox.
        /// </summary>
        public KnownEndpoint? SelectedKnownEndpoint
        {
            get => null;
            set => ApplyKnownEndpoint(SelectedAgent, value);
        }

        /// <summary>Same as <see cref="SelectedKnownEndpoint"/>, but for <see cref="GuidedAgent"/> on the guided first-run page.</summary>
        public KnownEndpoint? GuidedKnownEndpoint
        {
            get => null;
            set => ApplyKnownEndpoint(GuidedAgent, value);
        }

        private void ApplyKnownEndpoint(AgentJson? agent, KnownEndpoint? value)
        {
            if (value is not null && agent is not null)
            {
                agent.Technical.Endpoint = value.Endpoint;
                //the protocol follows the endpoint that was just picked: leaving an agent pointed at
                //Anthropic while it still speaks the other protocol answers with a 404 that reads
                //like a broken installation. The box next to it stays editable, so this is an offer
                //rather than a decision.
                agent.Technical.ApiProtocol = KnownEndpointCatalog.IsAnthropicEndpoint(value.Endpoint)
                    ? LlmProtocol.Anthropic
                    : LlmProtocol.OpenAi;
                OnPropertyChanged();
            }
        }

        /// <summary>Whether <see cref="SelectedAgent"/>'s token is stored as an environment-variable reference rather than a literal value.</summary>
        public bool SelectedAgentUsesEnvToken
        {
            get => GetUsesEnvToken(SelectedAgent);
            set { SetUsesEnvToken(SelectedAgent, value); OnPropertyChanged(); }
        }

        /// <summary>Same as <see cref="SelectedAgentUsesEnvToken"/>, but for <see cref="GuidedAgent"/> on the guided first-run page.</summary>
        public bool GuidedAgentUsesEnvToken
        {
            get => GetUsesEnvToken(GuidedAgent);
            set { SetUsesEnvToken(GuidedAgent, value); OnPropertyChanged(); }
        }

        private static bool GetUsesEnvToken(AgentJson? agent)
        {
            return agent is not null && DirectOrEnvStringHelper.IsEnvReference(agent.Technical.Token ?? string.Empty);
        }

        private static void SetUsesEnvToken(AgentJson? agent, bool value)
        {
            if (agent is null)
            {
                return;
            }

            if (value)
            {
                if (!DirectOrEnvStringHelper.IsEnvReference(agent.Technical.Token ?? string.Empty))
                {
                    agent.Technical.Token = DirectOrEnvStringHelper.MakeEnvReference("MY_TOKEN_VAR");
                }
            }
            else
            {
                if (DirectOrEnvStringHelper.IsEnvReference(agent.Technical.Token ?? string.Empty))
                {
                    agent.Technical.Token = string.Empty;
                }
            }
        }

        /// <summary>
        /// Either the literal token or, while <see cref="SelectedAgentUsesEnvToken"/> is set, the
        /// bare environment variable name - so the textbox never shows the raw <c>{$VAR}</c> syntax.
        /// </summary>
        public string SelectedAgentTokenDisplay
        {
            get => GetTokenDisplay(SelectedAgent);
            set { SetTokenDisplay(SelectedAgent, SelectedAgentUsesEnvToken, value); OnPropertyChanged(); }
        }

        /// <summary>Same as <see cref="SelectedAgentTokenDisplay"/>, but for <see cref="GuidedAgent"/> on the guided first-run page.</summary>
        public string GuidedAgentTokenDisplay
        {
            get => GetTokenDisplay(GuidedAgent);
            set { SetTokenDisplay(GuidedAgent, GuidedAgentUsesEnvToken, value); OnPropertyChanged(); }
        }

        private static string GetTokenDisplay(AgentJson? agent)
        {
            if (agent is null)
            {
                return string.Empty;
            }

            var token = agent.Technical.Token ?? string.Empty;
            if (DirectOrEnvStringHelper.TryGetVarName(token, out var varName))
            {
                return varName;
            }

            return token;
        }

        private static void SetTokenDisplay(AgentJson? agent, bool usesEnvToken, string value)
        {
            if (agent is null)
            {
                return;
            }

            agent.Technical.Token = usesEnvToken
                ? DirectOrEnvStringHelper.MakeEnvReference(value ?? string.Empty)
                : value ?? string.Empty;
        }

        /// <summary>Result text of the last <see cref="TestEndpointCommand"/> run, shown next to its button.</summary>
        public string EndpointTestStatus
        {
            get;
            private set
            {
                field = value;
                OnPropertyChanged();
            }
        } = string.Empty;

        public string? DestinationDescription
        {
            get => _destinationDescription;
            private set
            {
                _destinationDescription = value;
                OnPropertyChanged();
            }
        }

        public WizardStep CurrentStep => _navigator.CurrentStep;

        public Visibility WelcomeVisibility => StepVisibility(WizardStep.Welcome);
        public Visibility StartingPointVisibility => StepVisibility(WizardStep.StartingPoint);
        public Visibility AgentsVisibility => StepVisibility(WizardStep.Agents);
        public Visibility McpServersVisibility => StepVisibility(WizardStep.McpServers);
        public Visibility ActionsVisibility => StepVisibility(WizardStep.Actions);
        public Visibility MiscSettingsVisibility => StepVisibility(WizardStep.MiscSettings);
        public Visibility SummaryVisibility => StepVisibility(WizardStep.Summary);

        public Visibility BackButtonVisibility => _navigator.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NextButtonVisibility => _navigator.IsLastStep ? Visibility.Collapsed : Visibility.Visible;
        public Visibility FinishButtonVisibility => _navigator.IsLastStep ? Visibility.Visible : Visibility.Collapsed;

        public string StepTitle => GetStepTitle(CurrentStep);

        public string StepDescription => GetStepDescription(CurrentStep);

        public int McpServerCount => Options.AvailableMcpServers.Servers.Count;

        public int ActionCount => Options.Supports.Actions.Count;

        //the counts are formatted here rather than through a XAML StringFormat so their wording
        //comes out of the resx like every other sentence in the window
        public string McpServerCountText => string.Format(Resources.Resources.Wizard_mcp_count, McpServerCount);

        public string ActionCountText => string.Format(Resources.Resources.Wizard_actions_count, ActionCount);

        public string SummaryAgentsText => string.Format(Resources.Resources.Wizard_summary_agents, AvailableAgents.Count);

        public string SummaryMcpServersText => string.Format(Resources.Resources.Wizard_summary_mcpservers, McpServerCount);

        public string SummaryActionsText => string.Format(Resources.Resources.Wizard_summary_actions, ActionCount);

        /// <summary>
        /// Whether whole line completion runs by itself as the user types. It lives on the actions
        /// step rather than among the other settings because it is what makes the whole line
        /// completion action a live action or a dormant one: it decides whether
        /// <see cref="UseDefaultActionsCommand"/> binds that action to an agent, and whether
        /// <see cref="ActionProblems"/> complains when it has none.
        /// </summary>
        public bool IsWholeLineCompletionEnabled
        {
            get => Options.Unsorted.IsImplicitWholeLineCompletionEnabled;
            set
            {
                Options.Unsorted.IsImplicitWholeLineCompletionEnabled = value;

                //the switch changes what counts as a problem, so the warning is refreshed with it
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The localized red warning listing every action which cannot run with the agents that now
        /// exist. Recomputed on every property refresh, so creating the first-run agent, editing the
        /// agent list, restoring the defaults, flipping <see cref="IsWholeLineCompletionEnabled"/>
        /// or coming back from the action editor all update it.
        /// </summary>
        public string ActionProblems
        {
            get
            {
                try
                {
                    var problems = WizardValidator.ValidateActionAgentBindings(
                        Options.Supports.Actions.Select(a => new ActionBinding(a.Name, a.AgentName, IsSwitchedScope(a))),
                        Options.AgentCollection.Agents.Select(a => a.Name),
                        IsWholeLineCompletionEnabled
                        );

                    if (problems.Count == 0)
                    {
                        return string.Empty;
                    }

                    return Resources.Resources.Wizard_actions_problems_header
                        + Environment.NewLine
                        + string.Join(Environment.NewLine, problems.Select(Describe));
                }
                catch (Exception excp)
                {
                    //a binding getter that throws is swallowed by WPF and the warning silently
                    //disappears; showing the failure itself is more useful than showing nothing
                    excp.ActivityLogException();
                    return excp.Message;
                }
            }
        }

        public Visibility ActionProblemsVisibility => string.IsNullOrEmpty(ActionProblems) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>Outcome of the last <see cref="UseDefaultActionsCommand"/> run, shown under its button.</summary>
        public string ActionDefaultsStatus
        {
            get;
            private set
            {
                field = value;
                OnPropertyChanged();
            }
        } = string.Empty;

        /// <summary>
        /// Opt-in flag driving <see cref="GithubTokenVisibility"/>: the GitHub token field (and, on
        /// Finish, the token itself) only appears once the user has actually asked for the built-in
        /// GitHub MCP server. Unchecked by default, so nothing GitHub-related is requested unasked.
        /// </summary>
        public bool WantsGithubMcpServer
        {
            get;
            set
            {
                field = value;
                if (!value)
                {
                    Options.Unsorted.GitHubToken = string.Empty;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(GithubTokenVisibility));
            }
        }

        public Visibility GithubTokenVisibility => WantsGithubMcpServer ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Whether the Microsoft Learn documentation MCP server is part of the configuration being
        /// built. Ticking the box registers it under the name and endpoint of
        /// <see cref="KnownMcpServerCatalog"/> - the same entry the control center's install button
        /// writes - and unticking it takes it back out.
        ///
        /// The state is read out of <see cref="Options"/> rather than kept in a field of its own, so
        /// the box also reflects a server added or removed in the MCP server editor next to it, and
        /// so reopening the wizard against an existing configuration shows it already ticked.
        /// </summary>
        public bool WantsMsdnMcpServer
        {
            get => FindMsdnMcpServerNames().Count > 0;
            set => Guarded(() =>
            {
                if (value)
                {
                    //the indexer rather than Add: the name may already be taken by the same server
                    //pointing at an older endpoint, which is exactly what this is meant to fix
                    Options.AvailableMcpServers.Servers[KnownMcpServerCatalog.MicrosoftDocsServerName] =
                        new McpServer(
                            McpServerType.Http,
                            KnownMcpServerCatalog.BuildHttpConfiguration(KnownMcpServerCatalog.MicrosoftDocsEndpoint)
                            );
                }
                else
                {
                    //by endpoint, not by name: the user may have registered it under a name of
                    //their own, and leaving that one behind would keep the box ticked
                    foreach (var name in FindMsdnMcpServerNames())
                    {
                        Options.AvailableMcpServers.Servers.Remove(name);
                    }
                }

                OnPropertyChanged();
            });
        }

        public ICommand BackCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(_ => Guarded(() =>
                    {
                        _navigator.Back();
                        RefreshEverything();
                    }), _ => _navigator.CanGoBack);
                }

                return field;
            }
        }

        public ICommand NextCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(async _ => await GuardedAsync(async () =>
                    {
                        if (CurrentStep == WizardStep.StartingPoint)
                        {
                            await ApplyStartingPointAsync();
                        }

                        if (!CanAdvanceFromCurrentStep())
                        {
                            var problems = GetCurrentStepProblems();
                            await VS.MessageBox.ShowErrorAsync(
                                Resources.Resources.Error,
                                string.Join(Environment.NewLine, problems)
                                );
                            return;
                        }

                        if (CurrentStep == WizardStep.Agents && IsFirstRun)
                        {
                            CommitGuidedAgent();
                        }

                        _navigator.Next();
                        RefreshEverything();
                    }));
                }

                return field;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(async _ =>
                    {
                        //deliberately outside GuardedAsync: if even the confirmation fails, the
                        //window must still be closable rather than trapping the user in the wizard
                        try
                        {
                            if (!await VS.MessageBox.ShowConfirmAsync(
                                Resources.Resources.Wizard_cancel_confirm
                                ))
                            {
                                return;
                            }
                        }
                        catch (Exception excp)
                        {
                            excp.ActivityLogException();
                        }

                        CloseWindow?.Invoke(false);
                    });
                }

                return field;
            }
        }

        public ICommand FinishCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(async _ => await GuardedAsync(async () =>
                    {
                        if (!await McpServerProxyApplication.ApplyServerNodeAsync(Options.AvailableMcpServers))
                        {
                            return;
                        }

                        await Options.SerializeAsync(null);

                        InternalPage.Instance.SetupWizardIntroduced = true;
                        await InternalPage.Instance.SaveAsync();

                        CloseWindow?.Invoke(true);
                    }));
                }

                return field;
            }
        }

        public ICommand AddAgentCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(_ => Guarded(() =>
                    {
                        var agent = new AgentJson { Name = string.Empty };
                        Options.AgentCollection.Agents.Add(agent);
                        AvailableAgents.Add(agent);
                        SelectedAgent = agent;
                        OnPropertyChanged();
                    }));
                }

                return field;
            }
        }

        public ICommand DeleteAgentCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(_ => Guarded(() =>
                    {
                        if (SelectedAgent is null)
                        {
                            return;
                        }

                        Options.AgentCollection.Agents.Remove(SelectedAgent);
                        AvailableAgents.Remove(SelectedAgent);
                        SelectedAgent = null;
                        OnPropertyChanged();
                    }), _ => SelectedAgent is not null);
                }

                return field;
            }
        }

        /// <summary>
        /// Probes <see cref="GuidedAgent"/>'s endpoint by asking it for its model list, the same
        /// call the model picker elsewhere in FreeAIr makes - so a typo or an unreachable server is
        /// caught here instead of on the first real chat request.
        /// </summary>
        public ICommand TestEndpointCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(async _ => await GuardedAsync(async () =>
                    {
                        EndpointTestStatus = Resources.Resources.Wizard_agent_test_checking;

                        var uri = UriHelper.TryBuildEndpointUri(GuidedAgent.Technical.Endpoint);
                        if (uri is null)
                        {
                            EndpointTestStatus = Resources.Resources.Wizard_agent_test_invalid_uri;
                            return;
                        }

                        //an unreachable server is the expected outcome here, not a failure of the
                        //wizard: it is reported in the status line rather than as an error dialog
                        try
                        {
                            var catalog = LlmModelCatalogFactory.Create(
                                GuidedAgent.Technical.ApiProtocol,
                                uri,
                                GuidedAgent.Technical.GetToken(),
                                TimeSpan.FromSeconds(15)
                                );
                            var models = await catalog.GetModelsAsync();
                            EndpointTestStatus = string.Format(Resources.Resources.Wizard_agent_test_reachable, models.Count);
                        }
                        catch (Exception excp)
                        {
                            excp.ActivityLogException();
                            EndpointTestStatus = string.Format(Resources.Resources.Wizard_agent_test_failed, excp.Message);
                        }
                    }));
                }

                return field;
            }
        }

        public ICommand ConfigureMcpServersCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(async _ => await GuardedAsync(async () =>
                    {
                        var serversClone = new Dictionary<string, McpServer>(Options.AvailableMcpServers.Servers);

                        var w = new McpServerConfigureWindow();
                        var vm = new McpServerConfigureViewModel(serversClone);
                        w.DataContext = vm;

                        if ((await w.ShowDialogAsync()).GetValueOrDefault())
                        {
                            Options.AvailableMcpServers.Servers = vm.BuildServerDictionary();
                            OnPropertyChanged();
                        }
                    }));
                }

                return field;
            }
        }

        /// <summary>
        /// Throws away whatever the action list currently holds and rebuilds it from the actions
        /// FreeAIr ships with, binding each one to the first configured agent so the restored
        /// prompts are usable immediately rather than asking which agent to use on every invocation.
        ///
        /// The whole line completion action follows <see cref="IsWholeLineCompletionEnabled"/>: it
        /// is bound like the rest when the user has asked for that feature, and left on the shipped
        /// placeholder name when they have not.
        /// </summary>
        public ICommand UseDefaultActionsCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(_ => Guarded(() =>
                    {
                        var firstAgent = Options.AgentCollection.Agents.FirstOrDefault();
                        if (firstAgent is null || string.IsNullOrWhiteSpace(firstAgent.Name))
                        {
                            ActionDefaultsStatus = Resources.Resources.Wizard_actions_defaults_need_an_agent;
                            return;
                        }

                        var defaults = new SupportCollectionJson();
                        foreach (var action in defaults.Actions)
                        {
                            if (IsSwitchedScope(action) && !IsWholeLineCompletionEnabled)
                            {
                                continue;
                            }

                            action.AgentName = firstAgent.Name;
                        }

                        Options.Supports = defaults;

                        ActionDefaultsStatus = string.Format(
                            Resources.Resources.Wizard_actions_defaults_applied,
                            defaults.Actions.Count,
                            firstAgent.Name
                            );

                        OnPropertyChanged();
                    }));
                }

                return field;
            }
        }

        public ICommand ConfigureActionsCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(async _ => await GuardedAsync(async () =>
                    {
                        var agentsClone = (AgentCollectionJson)Options.AgentCollection.Clone();
                        var actionsClone = (SupportCollectionJson)Options.Supports.Clone();

                        var w = new ActionConfigureWindow();
                        w.DataContext = new ActionConfigureViewModel(agentsClone, actionsClone);

                        if ((await w.ShowDialogAsync()).GetValueOrDefault())
                        {
                            Options.Supports = actionsClone;
                            OnPropertyChanged();
                        }
                    }));
                }

                return field;
            }
        }

        public SetupWizardViewModel(bool isFirstRun)
        {
            IsFirstRun = isFirstRun;
            _navigator = new WizardNavigator(isFirstRun);

            Options = new FreeAIrOptions();
            GuidedAgent = new AgentJson { Name = string.Empty };

            if (isFirstRun)
            {
                //a first run walks the user through creating their own agent from scratch, so the
                //built-in sample agents (placeholder tokens, the author's own endpoints) never end
                //up in a new install's configuration
                Options.AgentCollection.Agents.Clear();

                //likewise, nothing GitHub-related should be pre-filled until the user opts in on
                //the "other settings" step (see WantsGithubMcpServer)
                Options.Unsorted.GitHubToken = string.Empty;
            }

            AvailableAgents = new ObservableCollection2<AgentJson>(Options.AgentCollection.Agents);
        }

        /// <summary>
        /// Computes <see cref="DestinationDescription"/>; called once before the window is shown.
        /// A failure here must not stop the window from opening - it only means the welcome page
        /// cannot name the destination file, which is worth reporting but not worth blocking on.
        /// </summary>
        public async Task InitializeAsync()
        {
            await GuardedAsync(async () =>
            {
                var filePath = await FreeAIrOptions.ComposeOptionsFilePathAsync();
                DestinationDescription = string.IsNullOrEmpty(filePath)
                    ? Resources.Resources.Wizard_welcome_destination_vs_store
                    : filePath;
            });
        }

        private async Task ApplyStartingPointAsync()
        {
            switch (SelectedStartingPoint)
            {
                case WizardStartingPointChoice.BuiltInDefaults:
                    Options = new FreeAIrOptions();
                    break;

                case WizardStartingPointChoice.Blank:
                    Options = new FreeAIrOptions();
                    Options.AgentCollection.Agents.Clear();
                    Options.Supports.Actions.Clear();
                    Options.AvailableMcpServers.Servers.Clear();
                    break;

                case WizardStartingPointChoice.ExistingConfig:
                    Options = await FreeAIrOptions.DeserializeAsync(null);
                    break;
            }

            AvailableAgents = new ObservableCollection2<AgentJson>(Options.AgentCollection.Agents);
            SelectedAgent = null;
            OnPropertyChanged();
        }

        /// <summary>
        /// Runs one command body so that a failure inside it becomes a logged entry plus an error
        /// dialog naming what went wrong, never an exception escaping into WPF's dispatcher - which
        /// for a modal dialog inside devenv means taking Visual Studio down rather than the wizard.
        /// </summary>
        private static void Guarded(Action body)
        {
            try
            {
                body();
            }
            catch (Exception excp)
            {
                Report(excp);
            }
        }

        /// <summary>The awaitable counterpart of <see cref="Guarded"/>, for the commands that open dialogs or touch the network.</summary>
        private static async Task GuardedAsync(Func<Task> body)
        {
            try
            {
                await body();
            }
            catch (Exception excp)
            {
                Report(excp);
            }
        }

        /// <summary>Writes a failure to the activity log and shows it to the user, itself never throwing.</summary>
        private static void Report(Exception excp)
        {
            try
            {
                excp.ActivityLogException();

                VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    excp.Message
                    ).FileAndForget(nameof(SetupWizardViewModel) + "." + nameof(Report));
            }
            catch
            {
                //reporting a failure must not produce one of its own
            }
        }

        private bool CanAdvanceFromCurrentStep()
        {
            return GetCurrentStepProblems().Count == 0;
        }

        private IReadOnlyList<string> GetCurrentStepProblems()
        {
            if (CurrentStep != WizardStep.Agents)
            {
                return Array.Empty<string>();
            }

            if (IsFirstRun)
            {
                return WizardValidator.ValidateAgentFields(
                    GuidedAgent.Name,
                    GuidedAgent.Technical.Endpoint,
                    GuidedAgent.Technical.Token,
                    GuidedAgent.Technical.ContextSize
                    )
                    .Select(Describe)
                    .ToList();
            }

            return Options.AgentCollection.Agents
                .SelectMany(a =>
                    WizardValidator.ValidateAgentFields(a.Name, a.Technical.Endpoint, a.Technical.Token, a.Technical.ContextSize)
                        .Select(p => $"{(string.IsNullOrWhiteSpace(a.Name) ? Resources.Resources.Wizard_agent_unnamed : a.Name)}: {Describe(p)}")
                    )
                .ToList();
        }

        /// <summary>Renders one agent-field finding of the resource-free wizard project as the localized sentence the user reads.</summary>
        private static string Describe(AgentFieldProblem problem)
        {
            switch (problem)
            {
                case AgentFieldProblem.MissingName: return Resources.Resources.Wizard_agent_problem_missing_name;
                case AgentFieldProblem.InvalidEndpoint: return Resources.Resources.Wizard_agent_problem_invalid_endpoint;
                case AgentFieldProblem.InvalidTokenForm: return Resources.Resources.Wizard_agent_problem_invalid_token;
                case AgentFieldProblem.NonPositiveContextSize: return Resources.Resources.Wizard_agent_problem_context_size;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// The names every MCP server in the configuration is registered under that points at the
        /// Microsoft Learn endpoint - normally none or one. Backs <see cref="WantsMsdnMcpServer"/>
        /// in both directions.
        ///
        /// Deciding this means parsing each server's raw configuration JSON, which a hand-edited
        /// settings file can make fail; a throw here would come out of a WPF binding, where it
        /// silently turns the checkbox into a dead control instead of saying anything.
        /// </summary>
        private IReadOnlyList<string> FindMsdnMcpServerNames()
        {
            var found = new List<string>();

            foreach (var server in Options.AvailableMcpServers.Servers)
            {
                try
                {
                    if (server.Value.IsHttpAndHasEndpoint(KnownMcpServerCatalog.MicrosoftDocsEndpoint))
                    {
                        found.Add(server.Key);
                    }
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();
                }
            }

            return found;
        }

        /// <summary>
        /// Whether this action is the one governed by <see cref="IsWholeLineCompletionEnabled"/>.
        /// Told apart by its scope rather than by its name, which the user is free to change.
        /// </summary>
        private static bool IsSwitchedScope(SupportActionJson action)
        {
            return action.Scopes.Contains(SwitchedScope);
        }

        /// <summary>Renders one broken action binding as the localized sentence shown in <see cref="ActionProblems"/>.</summary>
        private static string Describe(ActionBindingProblem problem)
        {
            switch (problem.Kind)
            {
                case ActionBindingProblemKind.NoAgentAssigned:
                    return string.Format(Resources.Resources.Wizard_actions_problem_no_agent, problem.ActionName);
                case ActionBindingProblemKind.UnknownAgent:
                    return string.Format(Resources.Resources.Wizard_actions_problem_unknown_agent, problem.ActionName, problem.AgentName);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Places the completed <see cref="GuidedAgent"/> into <see cref="Options"/> once its
        /// fields pass validation, so Finish writes it out - a no-op if it is already there (e.g.
        /// the user went Back into the step and Next again without changing anything).
        /// </summary>
        private void CommitGuidedAgent()
        {
            if (Options.AgentCollection.Agents.Contains(GuidedAgent))
            {
                return;
            }

            Options.AgentCollection.Agents.Add(GuidedAgent);
            AvailableAgents.Add(GuidedAgent);
        }

        private Visibility StepVisibility(WizardStep step)
        {
            return CurrentStep == step ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshEverything()
        {
            OnPropertyChanged();
        }

        private static string GetStepTitle(WizardStep step)
        {
            switch (step)
            {
                case WizardStep.Welcome: return Resources.Resources.Wizard_step_welcome_title;
                case WizardStep.StartingPoint: return Resources.Resources.Wizard_step_startingpoint_title;
                case WizardStep.Agents: return Resources.Resources.Wizard_step_agents_title;
                case WizardStep.McpServers: return Resources.Resources.Wizard_step_mcpservers_title;
                case WizardStep.Actions: return Resources.Resources.Wizard_step_actions_title;
                case WizardStep.MiscSettings: return Resources.Resources.Wizard_step_miscsettings_title;
                case WizardStep.Summary: return Resources.Resources.Wizard_step_summary_title;
                default: return string.Empty;
            }
        }

        private static string GetStepDescription(WizardStep step)
        {
            switch (step)
            {
                case WizardStep.Welcome: return Resources.Resources.Wizard_step_welcome_description;
                case WizardStep.StartingPoint: return Resources.Resources.Wizard_step_startingpoint_description;
                case WizardStep.Agents: return Resources.Resources.Wizard_step_agents_description;
                case WizardStep.McpServers: return Resources.Resources.Wizard_step_mcpservers_description;
                case WizardStep.Actions: return Resources.Resources.Wizard_step_actions_description;
                case WizardStep.MiscSettings: return Resources.Resources.Wizard_step_miscsettings_description;
                case WizardStep.Summary: return Resources.Resources.Wizard_step_summary_description;
                default: return string.Empty;
            }
        }
    }
}
