using FreeAIr.Chat.Content;
using FreeAIr.Chat.Context;
using FreeAIr.Chat.Context.Item;
using FreeAIr.Options2.Mcp;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Chat.Persistence
{
    /// <summary>
    /// The on-disk shape of a user chat, written to `.freeair\chats\{id}.json`.
    ///
    /// Automatic chats never become this: only a dialogue the user opened, and only when the
    /// solution folder was known at creation time so there was a place to put the file.
    /// </summary>
    public sealed class PersistedChatJson
    {
        /// <summary>Format marker so a future writer can recognise an older file instead of guessing.</summary>
        public int Version
        {
            get;
            set;
        } = 1;

        /// <summary>The chat id, also used as the file name.</summary>
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>What the chat list shows as the row caption.</summary>
        public string Title
        {
            get;
            set;
        } = "Untitled";

        /// <summary>When the chat was created, local time.</summary>
        public DateTime Started
        {
            get;
            set;
        }

        /// <summary>Idle / failed / not started. In-flight states are stored as ready: a restart cannot resume a stream.</summary>
        public ChatStatusEnum Status
        {
            get;
            set;
        }

        /// <summary>The agent that was answering when the file was last written, looked up by name on load.</summary>
        public string AgentName
        {
            get;
            set;
        } = string.Empty;

        /// <summary>The file the chat was started from, if any, so the list can still show it after a restart.</summary>
        public string? SelectedFilePath
        {
            get;
            set;
        }

        /// <summary>The per-chat MCP tool switches as they were when the file was written.</summary>
        public AvailableMcpServersJson? Tools
        {
            get;
            set;
        }

        /// <summary>Context chips attached to the chat: solution files, external files and raw text.</summary>
        public List<PersistedContextItemJson> Context
        {
            get;
            set;
        } = new();

        /// <summary>The transcript, in chronological order.</summary>
        public List<PersistedContentJson> Contents
        {
            get;
            set;
        } = new();

        /// <summary>
        /// Snapshots a live chat into the json shape. Call this on a settled turn — a prompt just
        /// added, an answer just finished — not while tokens are still arriving.
        /// </summary>
        public static PersistedChatJson FromChat(Chat chat)
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            var status = chat.Status;
            if (status == ChatStatusEnum.WaitingForAnswer || status == ChatStatusEnum.ReadingAnswer)
            {
                status = ChatStatusEnum.Ready;
            }

            return new PersistedChatJson
            {
                Version = 1,
                Id = chat.Id,
                Title = chat.Description.Title,
                Started = chat.Started ?? DateTime.Now,
                Status = status,
                AgentName = chat.Options.ChosenAgent.Name,
                SelectedFilePath = chat.Description.SelectedTextDescriptor?.FilePath,
                Tools = chat.ChatTools.CloneServers(),
                Context = chat.ChatContext.Items
                    .Select(PersistedContextItemJson.FromItem)
                    .Where(i => i is not null)
                    .Cast<PersistedContextItemJson>()
                    .ToList(),
                Contents = chat.Contents
                    .Select(PersistedContentJson.FromContent)
                    .ToList(),
            };
        }
    }

    /// <summary>
    /// One context chip as stored on disk. <see cref="Kind"/> says which live item to rebuild;
    /// unused fields stay null rather than growing three sibling types.
    /// </summary>
    public sealed class PersistedContextItemJson
    {
        public const string KindSolutionItem = "SolutionItem";
        public const string KindCustomFile = "CustomFile";
        public const string KindSimpleText = "SimpleText";

        /// <summary>Which live context item this row rebuilds.</summary>
        public string Kind
        {
            get;
            set;
        } = string.Empty;

        /// <summary>Absolute path of a solution or external file; unused for raw text.</summary>
        public string? FilePath
        {
            get;
            set;
        }

        /// <summary>Selection start inside the file, or null when the whole file is attached.</summary>
        public int? SelectionStart
        {
            get;
            set;
        }

        /// <summary>Selection length inside the file, or null when the whole file is attached.</summary>
        public int? SelectionLength
        {
            get;
            set;
        }

        /// <summary>True when FreeAIr attached this item by itself (Copilot instructions, reference walk).</summary>
        public bool IsAutoFound
        {
            get;
            set;
        }

        /// <summary>How line numbers were prefixed onto a solution file, if at all.</summary>
        public AddLineNumbersModeEnum? LineNumbers
        {
            get;
            set;
        }

        /// <summary>The numbered ranges when <see cref="LineNumbers"/> is specific scopes.</summary>
        public List<PersistedLineNumberScopeJson>? LineNumberScopes
        {
            get;
            set;
        }

        /// <summary>Chip label of a raw-text item.</summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>Body of a raw-text item.</summary>
        public string? Body
        {
            get;
            set;
        }

        /// <summary>Turns a live context item into a row, or null when the item has no on-disk form.</summary>
        public static PersistedContextItemJson? FromItem(IChatContextItem item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            switch (item)
            {
                case SolutionItemChatContextItem solutionItem:
                    return new PersistedContextItemJson
                    {
                        Kind = KindSolutionItem,
                        FilePath = solutionItem.SelectedIdentifier.FilePath,
                        SelectionStart = solutionItem.SelectedIdentifier.Selection?.StartPosition,
                        SelectionLength = solutionItem.SelectedIdentifier.Selection?.Length,
                        IsAutoFound = solutionItem.IsAutoFound,
                        LineNumbers = solutionItem.LineNumberMode.Mode,
                        LineNumberScopes = solutionItem.LineNumberMode.Mode == AddLineNumbersModeEnum.SpecificScopes
                            ? solutionItem.LineNumberMode.Scopes
                                .Select(s => new PersistedLineNumberScopeJson
                                {
                                    StartLine = s.StartLine,
                                    LineCount = s.LineCount,
                                })
                                .ToList()
                            : null,
                    };

                case CustomFileChatContextItem customFile:
                    return new PersistedContextItemJson
                    {
                        Kind = KindCustomFile,
                        FilePath = customFile.FilePath,
                        IsAutoFound = customFile.IsAutoFound,
                    };

                case SimpleTextChatContextItem simpleText:
                    return new PersistedContextItemJson
                    {
                        Kind = KindSimpleText,
                        Description = simpleText.ContextUIDescription,
                        Body = simpleText.Body,
                        IsAutoFound = simpleText.IsAutoFound,
                    };

                default:
                    return null;
            }
        }

        /// <summary>
        /// Rebuilds the live context item. A missing file is still restored as a chip — the prompt
        /// builder already turns that into a note rather than throwing.
        /// </summary>
        public IChatContextItem? ToItem()
        {
            switch (Kind)
            {
                case KindSolutionItem:
                    if (string.IsNullOrEmpty(FilePath))
                    {
                        return null;
                    }

                    SelectedSpan? selection = null;
                    if (SelectionStart.HasValue && SelectionLength.HasValue)
                    {
                        selection = new SelectedSpan(SelectionStart.Value, SelectionLength.Value);
                    }

                    return new SolutionItemChatContextItem(
                        SelectedIdentifier.Create(FilePath, selection),
                        IsAutoFound,
                        RestoreLineNumberMode()
                        );

                case KindCustomFile:
                    if (string.IsNullOrEmpty(FilePath))
                    {
                        return null;
                    }

                    return new CustomFileChatContextItem(FilePath, IsAutoFound);

                case KindSimpleText:
                    return new SimpleTextChatContextItem(
                        Description ?? string.Empty,
                        Body ?? string.Empty,
                        IsAutoFound
                        );

                default:
                    return null;
            }
        }

        private AddLineNumbersMode RestoreLineNumberMode()
        {
            switch (LineNumbers)
            {
                case AddLineNumbersModeEnum.AllInScope:
                    return AddLineNumbersMode.RequiredAllInScope;
                case AddLineNumbersModeEnum.SpecificScopes:
                    var scopes = (LineNumberScopes ?? [])
                        .Select(s => (s.StartLine, s.LineCount))
                        .ToList();
                    return AddLineNumbersMode.RequiredForScopes(scopes);
                default:
                    return AddLineNumbersMode.NotRequired;
            }
        }
    }

    /// <summary>One numbered line range stored with a solution-file context item.</summary>
    public sealed class PersistedLineNumberScopeJson
    {
        public int StartLine
        {
            get;
            set;
        }

        public int LineCount
        {
            get;
            set;
        }
    }

    /// <summary>
    /// One prompt, answer or tool call as stored on disk. The <see cref="Type"/> field chooses
    /// which of the other fields are filled.
    /// </summary>
    public sealed class PersistedContentJson
    {
        /// <summary>Prompt, LLM answer or tool call.</summary>
        public ChatContentTypeEnum Type
        {
            get;
            set;
        }

        /// <summary>Prompt text, answer text, or unused for a tool call.</summary>
        public string? Body
        {
            get;
            set;
        }

        /// <summary>Whether this entry has been dropped from the next request.</summary>
        public bool IsArchived
        {
            get;
            set;
        }

        /// <summary>Tool name, for a tool call.</summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>Provider-assigned tool call id, needed to send the result back.</summary>
        public string? ToolCallId
        {
            get;
            set;
        }

        /// <summary>JSON arguments the model supplied.</summary>
        public string? Arguments
        {
            get;
            set;
        }

        /// <summary>Where the tool call was in its lifecycle when the file was written.</summary>
        public ToolCallStatusEnum? Status
        {
            get;
            set;
        }

        /// <summary>Tool output, or a note if the call was interrupted.</summary>
        public string? Result
        {
            get;
            set;
        }

        /// <summary>Snapshots one live content into the json shape.</summary>
        public static PersistedContentJson FromContent(IChatContent content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            switch (content)
            {
                case UserPrompt prompt:
                    return new PersistedContentJson
                    {
                        Type = ChatContentTypeEnum.Prompt,
                        Body = prompt.PromptBody,
                        IsArchived = prompt.IsArchived,
                    };

                case AnswerChatContent answer:
                    return new PersistedContentJson
                    {
                        Type = ChatContentTypeEnum.LLMAnswer,
                        Body = answer.AnswerBody,
                        IsArchived = answer.IsArchived,
                    };

                case ToolCallChatContent toolCall:
                    return new PersistedContentJson
                    {
                        Type = ChatContentTypeEnum.ToolCall,
                        Name = toolCall.Name,
                        ToolCallId = toolCall.ToolCall.Id,
                        Arguments = toolCall.ToolCall.ArgumentsJson,
                        Status = toolCall.Status,
                        Result = toolCall.Result,
                        IsArchived = toolCall.IsArchived,
                    };

                default:
                    throw new InvalidOperationException($"Cannot persist chat content of type {content.GetType().Name}.");
            }
        }
    }
}
