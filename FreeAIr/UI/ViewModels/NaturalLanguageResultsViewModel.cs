using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using MessagePack.Formatters;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(NaturalLanguageResultsViewModel))]
    public sealed class NaturalLanguageResultsViewModel : BaseViewModel
    {
        private Chat? _chat;
        private ICommand _gotoCommand;
        private ICommand _cancelChatCommand;
        
        private string _status = "Idle";

        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public ObservableCollection2<FoundItem> FoundItems
        {
            get;
        }

        public ICommand GotoCommand
        {
            get
            {
                if (_gotoCommand is null)
                {
                    _gotoCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var foundItem = a as FoundItem;
                            if (foundItem is null)
                            {
                                return;
                            }

                            var documentView = await VS.Documents.OpenAsync(foundItem.FilePath);
                            if (documentView is null)
                            {
                                return;
                            }

                            var textView = documentView.TextView;
                            var text = textView.TextSnapshot.GetText();
                            var offset = text.IndexOf(foundItem.FoundText);
                            if (offset < 0)
                            {
                                offset = foundItem.OffsetIndex;
                            }

                            var snapshot = textView.TextSnapshot;

                            var startOffset = offset;
                            var startLine = snapshot.GetLineFromPosition(startOffset);
                            var startLineIndex = startLine.LineNumber;
                            var startColumnIndex = startOffset - startLine.Start.Position;

                            var endOffset = offset + foundItem.FoundText.Length;
                            var endLine = snapshot.GetLineFromPosition(endOffset);
                            var endLineIndex = endLine.LineNumber;
                            var endColumnIndex = endOffset - endLine.Start.Position;

                            var docViewType = default(Guid);

                            _ = textView.ToIVsTextView().GetBuffer(out var buffer);

                            var _textManager = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;
                            _textManager.NavigateToLineAndColumn(
                                buffer,
                                ref docViewType,
                                startLineIndex,
                                startColumnIndex,
                                endLineIndex,
                                endColumnIndex
                                );

                        }
                        );
                }

                return _gotoCommand;
            }
        }

        public ICommand CancelChatCommand
        {
            get
            {
                if (_cancelChatCommand is null)
                {
                    _cancelChatCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var chat = Interlocked.Exchange(ref _chat, null);
                            if (chat is not null)
                            {
                                await chat.StopAsync();
                            }

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            var chat = _chat;
                            return
                                chat is not null
                                && chat.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer)
                                ;
                        }
                        );
                }

                return _cancelChatCommand;
            }
        }

        public NaturalLanguageResultsViewModel()
        {
            FoundItems = new();
        }

        public async Task SetNewChatAsync(
            Chat chat
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (_chat is not null)
            {
                await _chat.StopAsync();
            }

            _chat = chat;

            UpdateFormAsync()
                .FileAndForget(nameof(UpdateFormAsync));
        }

        private async Task UpdateFormAsync()
        {
            try
            {
                FoundItems.Clear();

                var chat = _chat;

                if (chat is null)
                {
                    Status = "Idle";
                    return;
                }

                Status = "Waiting for answers...";

                await chat.WaitForPromptResultAsync();

                if (chat.Status == ChatStatusEnum.Ready)
                {
                    var lastPrompt = chat.Prompts.Last();
                    if (lastPrompt.Answer is not null)
                    {
                        var textAnswer = lastPrompt.Answer.GetTextualAnswer();
                        if (!string.IsNullOrEmpty(textAnswer))
                        {
                            var cleanAnswer = textAnswer.CleanupFromQuotesAndThinks(
                                Environment.NewLine
                                );
                            if (!string.IsNullOrEmpty(cleanAnswer))
                            {
                                var results = JsonSerializer.Deserialize<NaturalSearchResults>(cleanAnswer);
                                foreach (var match in results.matches)
                                {
                                    FoundItems.Add(
                                        new FoundItem(
                                            filePath: match.fullpath,
                                            foundText: match.found_text,
                                            reason: match.reason,
                                            lineIndex: match.line,
                                            columnIndex: match.column,
                                            offsetIndex: match.offset
                                            )
                                        );
                                }
                            }
                        }
                    }
                }

                Status = $"Found {FoundItems.Count} items:";
            }
            catch (Exception excp)
            {
                //todo log
                Status = $"Error: {excp.Message}";
            }

            OnPropertyChanged();
        }
    }


    public class NaturalSearchResults
    {
        public Match[] matches
        {
            get; set;
        }
    }

    public class Match
    {
        public string fullpath
        {
            get; set;
        }
        public string found_text
        {
            get; set;
        }
        public int line
        {
            get; set;
        }
        public int column
        {
            get; set;
        }
        public int offset
        {
            get; set;
        }
        public string reason
        {
            get; set;
        }
    }


    public sealed class FoundItem
    {
        public string FilePath
        {
            get;
        }

        public string FileName
        {
            get;
        }

        public string FoundText
        {
            get;
        }

        public string Reason
        {
            get;
        }

        public int LineIndex
        {
            get;
        }

        public int ColumnIndex
        {
            get;
        }

        public int OffsetIndex
        {
            get;
        }

        public FoundItem(
            string filePath,
            string foundText,
            string reason,
            int lineIndex,
            int columnIndex,
            int offsetIndex
            )
        {
            FilePath = filePath;

            FileName = new System.IO.FileInfo(filePath).Name;
            FoundText = foundText;
            Reason = reason;
            LineIndex = lineIndex;
            ColumnIndex = columnIndex;
            OffsetIndex = offsetIndex;
        }
    }
}
