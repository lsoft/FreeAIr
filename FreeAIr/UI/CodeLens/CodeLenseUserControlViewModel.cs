using FreeAIr.Commands;
using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using FreeAIr.Shared.Dto;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.CodeLens
{
    /// <summary>
    /// View model backing the CodeLens details-pane user control: builds the list of support-action
    /// commands (start discussion, plus every configured <see cref="SupportActionJson"/> scoped to
    /// <see cref="SupportScopeEnum.CodelensInDocument"/>) offered for the method under the indicator.
    /// </summary>
    public class CodeLenseUserControlViewModel : BaseViewModel
    {
        /// <summary>Commands shown in the CodeLens details pane, populated asynchronously by <see cref="ProcessAsync"/>.</summary>
        public ObservableCollection2<ICommandViewModel> CommandList
        {
            get;
        }

        /// <summary>Starts building <see cref="CommandList"/> for the given method's CodeLens.</summary>
        public CodeLenseUserControlViewModel(
            CodeLensUnitInfo unitInfo
            )
        {
            CommandList = new();

            ProcessAsync(unitInfo)
                .FileAndForget(nameof(ProcessAsync));
        }

        /// <summary>Populates <see cref="CommandList"/> with the built-in "start discussion" command plus every configured support action scoped to this CodeLens.</summary>
        private async Task ProcessAsync(
            CodeLensUnitInfo unitInfo
            )
        {
            CommandList.Add(
                new EmbeddedCommandViewModel(
                    unitInfo,
                    Resources.Resources.Start_discussion,
                    new CommandID(
                        PackageGuids.FreeAIr,
                        PackageIds.StartDiscussionCommandId
                        )
                    )
                );

            var actions = await FreeAIrOptions.DeserializeSupportActionsAsync(
                a => a.Scopes.Contains(SupportScopeEnum.CodelensInDocument)
                );
            foreach (var action in actions)
            {
                var cvm = new CommandViewModel(
                    unitInfo,
                    action
                    );
                CommandList.Add(cvm);
            }
        }
    }

    #region viewmodels

    /// <summary>One clickable command row in the CodeLens details pane's command list.</summary>
    public interface ICommandViewModel
    {
        /// <summary>Display text shown for this command row.</summary>
        string CommandName
        {
            get;
        }

        /// <summary>Command invoked when the row is clicked.</summary>
        ICommand ApplyCommand
        {
            get;
        }
    }

    /// <summary>Command row that selects the method's span and dispatches an existing built-in Visual Studio command (identified by a <see cref="CommandID"/>), e.g. "start discussion".</summary>
    public sealed class EmbeddedCommandViewModel : BaseViewModel, ICommandViewModel
    {
        /// <summary>Method the CodeLens indicator is attached to.</summary>
        private readonly CodeLensUnitInfo _unitInfo;
        /// <summary>Display text for this row.</summary>
        private readonly string _commandName;
        /// <summary>Id of the built-in command to dispatch.</summary>
        private readonly CommandID _commandId;

        /// <summary>Display text for this row, built from <see cref="_commandName"/>.</summary>
        public string CommandName => "✓   " + _commandName + "...";

        /// <summary>Lazily-created command that selects the method's span and dispatches <see cref="_commandId"/>.</summary>
        public ICommand ApplyCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                );
                        }
                        );
                }

                return field;
            }
        }

        /// <summary>Creates the row for an existing built-in Visual Studio command against the given method.</summary>
        public EmbeddedCommandViewModel(
            CodeLensUnitInfo unitInfo,
            string commandName,
            CommandID commandId
            )
        {
            if (unitInfo is null)
            {
                throw new ArgumentNullException(nameof(unitInfo));
            }

            if (commandName is null)
            {
                throw new ArgumentNullException(nameof(commandName));
            }

            if (commandId is null)
            {
                throw new ArgumentNullException(nameof(commandId));
            }

            _unitInfo = unitInfo;
            _commandName = commandName;
            _commandId = commandId;
        }

        /// <summary>Selects the method's span in the active document, then dispatches <see cref="_commandId"/>; returns false if the span or active text view is unavailable.</summary>
        private async Task<bool> ProcessAsync(
            )
        {
            if (!_unitInfo.UnitInfo.SpanStart.HasValue)
            {
                return false;
            }
            if (!_unitInfo.UnitInfo.SpanLength.HasValue)
            {
                return false;
            }

            var activeDocument = await VS.Documents.GetActiveDocumentViewAsync();
            if (activeDocument is null)
            {
                return false;
            }
            if (activeDocument.TextView is null)
            {
                return false;
            }

            var textView = activeDocument.TextView;
            var textBuffer = textView.TextBuffer;
            var snapshot = textBuffer.CurrentSnapshot;

            SnapshotSpan span = new SnapshotSpan(
                snapshot,
                new Span(
                    _unitInfo.UnitInfo.SpanStart.Value,
                    _unitInfo.UnitInfo.SpanLength.Value
                    )
                );

            textView.Selection.Select(span, false);

            VS.Commands.ExecuteAsync(
                _commandId
                ).FileAndForget("VS.Commands.ExecuteAsync");
            return true;
        }
    }

    /// <summary>Command row that selects the method's span and runs a configured <see cref="SupportActionJson"/> (e.g. a custom chat prompt) against it.</summary>
    public sealed class CommandViewModel : BaseViewModel, ICommandViewModel
    {
        /// <summary>Method the CodeLens indicator is attached to.</summary>
        private readonly CodeLensUnitInfo _unitInfo;
        /// <summary>The configured support action this row runs.</summary>
        private readonly SupportActionJson _support;

        /// <summary>Display text for this row, built from the support action's name.</summary>
        public string CommandName => "✓   " + _support.Name + "...";

        /// <summary>Lazily-created command that selects the method's span and runs <see cref="_support"/>.</summary>
        public ICommand ApplyCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                );
                        }
                        );
                }

                return field;
            }
        }


        /// <summary>Creates the row for one configured <paramref name="support"/> action against the given method.</summary>
        public CommandViewModel(
            CodeLensUnitInfo unitInfo,
            SupportActionJson support
            )
        {
            if (unitInfo is null)
            {
                throw new ArgumentNullException(nameof(unitInfo));
            }

            if (support is null)
            {
                throw new ArgumentNullException(nameof(support));
            }

            _unitInfo = unitInfo;
            _support = support;
        }

        /// <summary>Selects the method's span in the active document, then runs <see cref="_support"/> via <see cref="ApplySupportAction"/>; returns false if the span or active text view is unavailable.</summary>
        private async Task<bool> ProcessAsync(
            )
        {
            if (!_unitInfo.UnitInfo.SpanStart.HasValue)
            {
                return false;
            }
            if (!_unitInfo.UnitInfo.SpanLength.HasValue)
            {
                return false;
            }

            var activeDocument = await VS.Documents.GetActiveDocumentViewAsync();
            if (activeDocument is null)
            {
                return false;
            }
            if (activeDocument.TextView is null)
            {
                return false;
            }

            var textView = activeDocument.TextView;
            var textBuffer = textView.TextBuffer;
            var snapshot = textBuffer.CurrentSnapshot;

            SnapshotSpan span = new SnapshotSpan(
                snapshot,
                new Span(
                    _unitInfo.UnitInfo.SpanStart.Value,
                    _unitInfo.UnitInfo.SpanLength.Value
                    )
                );

            textView.Selection.Select(span, false);

            var asa = new ApplySupportAction(
                _support
                );
            await asa.ExecuteAsync();

            return true;
        }

        /// <summary><see cref="BaseApplySupportAction"/> specialization that runs one fixed, already-chosen support action instead of prompting the user to pick one.</summary>
        public sealed class ApplySupportAction : BaseApplySupportAction
        {
            /// <summary>The already-chosen support action to run.</summary>
            private readonly SupportActionJson _support;

            /// <summary>Wraps the already-chosen <paramref name="support"/> action so it can be run without a picker.</summary>
            public ApplySupportAction(
                SupportActionJson support
                )
            {
                if (support is null)
                {
                    throw new ArgumentNullException(nameof(support));
                }

                _support = support;
            }

            /// <summary>Returns the fixed <see cref="_support"/> action instead of prompting.</summary>
            protected override System.Threading.Tasks.Task<SupportActionJson> ChooseSupportAsync(
                )
            {
                return System.Threading.Tasks.Task.FromResult(_support);
            }
        }
    }

    #endregion
}
