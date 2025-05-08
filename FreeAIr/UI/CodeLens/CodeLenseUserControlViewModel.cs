using Community.VisualStudio.Toolkit;
using FreeAIr.Commands;
using FreeAIr.Shared.Dto;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.CodeLens
{
    public class CodeLenseUserControlViewModel : BaseViewModel
    {
        private readonly CodeLensUnitInfo _unitInfo;

        private ICommand _explainCodeCommand;
        private ICommand _addXmlCommentsCommand;
        private ICommand _optimizeCodeCommand;
        private ICommand _generateUnitTestsCommand;
        private ICommand _completeCodeByCommentsCommand;
        private ICommand _startDiscussionCommand;

        public ICommand ExplainCodeCommand
        {
            get
            {
                if (_explainCodeCommand is null)
                {
                    _explainCodeCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                new CommandID(
                                    PackageGuids.FreeAIr,
                                    PackageIds.ExplainCommandId
                                    )
                                );
                        }
                        );
                }

                return _explainCodeCommand;
            }
        }

        public ICommand AddXmlCommentsCommand
        {
            get
            {
                if (_addXmlCommentsCommand is null)
                {
                    _addXmlCommentsCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                new CommandID(
                                    PackageGuids.FreeAIr,
                                    PackageIds.AddXmlCommentsCommandId
                                    )
                                );
                        }
                        );
                }

                return _addXmlCommentsCommand;
            }
        }

        public ICommand OptimizeCodeCommand
        {
            get
            {
                if (_optimizeCodeCommand is null)
                {
                    _optimizeCodeCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                new CommandID(
                                    PackageGuids.FreeAIr,
                                    PackageIds.OptimizeCommandId
                                    )
                                );
                        }
                        );
                }

                return _optimizeCodeCommand;
            }
        }

        public ICommand GenerateUnitTestsCommand
        {
            get
            {
                if (_generateUnitTestsCommand is null)
                {
                    _generateUnitTestsCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                new CommandID(
                                    PackageGuids.FreeAIr,
                                    PackageIds.GenerateUnitTestsCommandId
                                    )
                                );
                        }
                        );
                }

                return _generateUnitTestsCommand;
            }
        }

        public ICommand CompleteCodeByCommentsCommand
        {
            get
            {
                if (_completeCodeByCommentsCommand is null)
                {
                    _completeCodeByCommentsCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                new CommandID(
                                    PackageGuids.FreeAIr,
                                    PackageIds.CompleteCodeByCommentsCommandId
                                    )
                                );
                        }
                        );
                }

                return _completeCodeByCommentsCommand;
            }
        }
        
        public ICommand StartDiscussionCommand
        {
            get
            {
                if (_startDiscussionCommand is null)
                {
                    _startDiscussionCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProcessAsync(
                                new CommandID(
                                    PackageGuids.FreeAIr,
                                    PackageIds.StartDiscussionCommandId
                                    )
                                );
                        }
                        );
                }

                return _startDiscussionCommand;
            }
        }

        private async Task<bool> ProcessAsync(
            CommandID commandId
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
                commandId
                ).FileAndForget(nameof(PackageIds.ExplainCommandId));
            return true;
        }

        public CodeLenseUserControlViewModel(
            CodeLensUnitInfo sic
            )
        {
            _unitInfo = sic;
        }
    }
}
