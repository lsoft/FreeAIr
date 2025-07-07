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
    public class CodeLenseUserControlViewModel : BaseViewModel
    {
        public ObservableCollection2<ICommandViewModel> CommandList
        {
            get;
        }

        public CodeLenseUserControlViewModel(
            CodeLensUnitInfo unitInfo
            )
        {
            CommandList = new();

            ProcessAsync(unitInfo)
                .FileAndForget(nameof(ProcessAsync));
        }

        private async Task ProcessAsync(
            CodeLensUnitInfo unitInfo
            )
        {
            CommandList.Add(
                new EmbeddedCommandViewModel(
                    unitInfo,
                    "Start discussion",
                    new CommandID(
                        PackageGuids.FreeAIr,
                        PackageIds.StartDiscussionCommandId
                        )
                    )
                );

            var supportCollection = await FreeAIrOptions.DeserializeSupportCollectionAsync();
            foreach (var action in supportCollection.Actions)
            {
                if (!action.Scopes.Contains(SupportScopeEnum.CodelensInDocument))
                {
                    continue;
                }

                var cvm = new CommandViewModel(
                    unitInfo,
                    action
                    );
                CommandList.Add(cvm);
            }
        }
    }

    #region viewmodels

    public interface ICommandViewModel
    {
        string CommandName
        {
            get;
        }

        ICommand ApplyCommand
        {
            get;
        }
    }

    public sealed class EmbeddedCommandViewModel : BaseViewModel, ICommandViewModel
    {
        private readonly CodeLensUnitInfo _unitInfo;
        private readonly string _commandName;
        private readonly CommandID _commandId;
        private ICommand _applyCommand;

        public string CommandName => "✓   " + _commandName + "...";

        public ICommand ApplyCommand
        {
            get
            {
                if (_applyCommand is null)
                {
                    _applyCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                );
                        }
                        );
                }

                return _applyCommand;
            }
        }

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

    public sealed class CommandViewModel : BaseViewModel, ICommandViewModel
    {
        private readonly CodeLensUnitInfo _unitInfo;
        private readonly SupportActionJson _support;

        private ICommand _applyCommand;

        public string CommandName => "✓   " + _support.Name + "...";

        public ICommand ApplyCommand
        {
            get
            {
                if (_applyCommand is null)
                {
                    _applyCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                );
                        }
                        );
                }

                return _applyCommand;
            }
        }


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

        public sealed class ApplySupportAction : BaseApplySupportAction
        {
            private readonly SupportActionJson _support;

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

            protected override System.Threading.Tasks.Task<SupportActionJson> ChooseSupportAsync(
                )
            {
                return System.Threading.Tasks.Task.FromResult(_support);
            }
        }
    }

    #endregion
}
