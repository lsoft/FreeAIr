using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using MessagePack.Formatters;
using Microsoft.VisualStudio;
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

                            var snapshot = textView.TextSnapshot;

                            var startLine = snapshot.GetLineFromLineNumber(foundItem.LineIndex);
                            var lineText = startLine.GetText();
                            
                            var startColumnIndex = lineText.IndexOf(foundItem.FoundText);
                            if (startColumnIndex < 0)
                            {
                                startColumnIndex = 0;
                            }

                            var startOffset = startLine.Start.Position + startColumnIndex;
                            var endOffset = startOffset + foundItem.FoundText.Length;
                            var endLine = snapshot.GetLineFromPosition(endOffset);
                            var endLineIndex = endLine.LineNumber;
                            var endColumnIndex = endOffset - endLine.Start.Position;

                            var docViewType = default(Guid);

                            if (textView.ToIVsTextView().GetBuffer(out var buffer) != VSConstants.S_OK)
                            {
                                return;
                            }

                            var textManager = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;
                            textManager.NavigateToLineAndColumn(
                                buffer,
                                ref docViewType,
                                foundItem.LineIndex,
                                startColumnIndex,
                                foundItem.LineIndex,
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

                var cleanAnswer = await chat.WaitForPromptCleanAnswerAsync(
                    Environment.NewLine
                    );
                if (!string.IsNullOrEmpty(cleanAnswer))
                {
                    var results = JsonSerializer.Deserialize<NaturalSearchResults>(cleanAnswer);
                    foreach (var match in results.matches.OrderByDescending(m => m.confidence_level))
                    {
                        FoundItems.Add(
                            new FoundItem(
                                filePath: match.fullpath,
                                foundText: match.found_text,
                                reason: match.reason,
                                confidenceLevel: match.confidence_level,
                                lineIndex: match.line
                                )
                            );
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
        public double confidence_level
        {
            get; set;
        }
        public int line
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

        public double ConfidenceLevel
        {
            get;
        }

        public int LineIndex
        {
            get;
        }


        public FoundItem(
            string filePath,
            string foundText,
            string reason,
            double confidenceLevel,
            int lineIndex
            )
        {
            FilePath = filePath;

            FileName = new System.IO.FileInfo(filePath).Name;
            FoundText = foundText;
            Reason = reason;
            ConfidenceLevel = 
                confidenceLevel > 100
                    ? 100
                    : (confidenceLevel < 0
                        ? 0
                        : confidenceLevel)
                    ;
            LineIndex = lineIndex;
        }
    }
}
