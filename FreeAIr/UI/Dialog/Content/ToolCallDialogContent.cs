using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Shared.Helper;
using MarkdownParser.Antlr.Answer;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using WpfHelpers;
using FreeAIr.Chat.Content;

namespace FreeAIr.UI.Dialog.Content
{
    /// <summary>
    /// Dialog content that renders one MCP tool-call bubble in the chat window: shows the tool's
    /// arguments as a table, tracks its approval/execution status, and exposes the commands the user
    /// clicks to run, block, or always-allow the call.
    /// </summary>
    public sealed class ToolCallDialogContent : DialogContent<ToolCallChatContent>
    {
        /// <summary>Current lifecycle state of the tool call (asking, executing, succeeded, failed or blocked).</summary>
        public ToolCallStatusEnum Status => TypedContent.Status;

        /// <summary>Name of the MCP tool being called.</summary>
        public string Name => TypedContent.Name;

        /// <summary>Human-readable status line shown in the bubble, worded according to the current <see cref="Status"/>.</summary>
        public string UIDescription
        {
            get
            {
                switch (Status)
                {
                    case ToolCallStatusEnum.Asking:
                        return $"Run {Name} tool once";
                    case ToolCallStatusEnum.Executing:
                        return $"Tool {Name} is executing...";
                    case ToolCallStatusEnum.Succeeded:
                        return $"Tool {Name} call succeeded";
                    case ToolCallStatusEnum.Failed:
                        return $"Tool {Name} call failed";
                    case ToolCallStatusEnum.Blocked:
                        return $"Tool {Name} call is blocked";
                }

                return string.Empty;
            }
        }

        /// <summary>Command that runs the tool once, enabled only while the call is awaiting user approval.</summary>
        public ICommand ClickCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ExecuteToolAsync();

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (Status == ToolCallStatusEnum.Asking)
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return field;
            }
        }

        /// <summary>Command that blocks this single invocation of the tool without changing its future approval status.</summary>
        public ICommand BlockAtThisTimeCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            SetBlocked();
                        },
                        a =>
                        {
                            if (Status == ToolCallStatusEnum.Asking)
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return field;
            }
        }

        /// <summary>Command that whitelists this specific tool for all future calls, then executes the current one.</summary>
        public ICommand AllowThisToolAllTimeCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            SetStatus(ToolCallStatusEnum.Executing);

                            await AllowThisToolAllTimeAsync();

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (Status == ToolCallStatusEnum.Asking)
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return field;
            }
        }

        /// <summary>Command that whitelists every MCP tool for all future calls, then executes the current one.</summary>
        public ICommand AllowAnyToolAllTimeCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            SetStatus(ToolCallStatusEnum.Executing);

                            await AllowAnyToolAllTimeAsync();

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (Status == ToolCallStatusEnum.Asking)
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return field;
            }
        }

        /// <summary>Command that pops up a message box with the tool call's result, enabled once the call has finished (succeeded, failed or blocked) and produced output.</summary>
        public ICommand ShowResultCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await VS.MessageBox.ShowAsync(
                                $"{TypedContent.ToolCall.Name} call result:",
                                TypedContent.Result
                                );
                        },
                        a =>
                        {
                            if (Status.In(ToolCallStatusEnum.Succeeded, ToolCallStatusEnum.Failed, ToolCallStatusEnum.Blocked)
                                && !string.IsNullOrEmpty(TypedContent.Result)
                                )
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return field;
            }
        }

        /// <summary>Flow document rendering the tool call's arguments as a markdown table.</summary>
        public FlowDocument ToolArgumentsFlowDocument
        {
            get;
        }


        /// <summary>Whether <see cref="ToolArgumentsFlowDocument"/> should be shown; hidden when the tool call has no arguments.</summary>
        public Visibility DocumentVisibility
        {
            get;
        }

        /// <summary>Builds the arguments table and, if this tool is already whitelisted for auto-execution, kicks off the call immediately.</summary>
        public ToolCallDialogContent(
            ToolCallChatContent content
            ) : base(content, content)
        {
            ToolArgumentsFlowDocument = new();
            DocumentVisibility = UpdateFlowDocument(content);

            var ts = InternalPage.Instance.ReadMCPToolsExecutionStatus();
            if (ts.IsToolEnabled(content.ToolCall.Name))
            {
                ExecuteToolAsync()
                    .FileAndForget(nameof(ExecuteToolAsync));
            }
        }

        /// <summary>Renders the tool call's argument name/value pairs as a markdown table into <see cref="ToolArgumentsFlowDocument"/>, returning whether there was anything to show.</summary>
        private Visibility UpdateFlowDocument(
            ToolCallChatContent content
            )
        {
            try
            {
                var json = content.ToolCall.ArgumentsJson;
                var nameValueDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (nameValueDict.Count <= 0)
                {
                    return Visibility.Collapsed;
                }

                var md = new ParsedMarkdown(
                    FontSizePage.Instance
                    );

                md.AddTableRow($"|Argument name|Argument value|");
                md.AddTableRow($"|---|---|");
                foreach (var pair in nameValueDict)
                {
                    md.AddTableRow($"|{pair.Key}|{pair.Value}|");
                }
                md.UpdateFlowDocument(ToolArgumentsFlowDocument, null, false);

                return Visibility.Visible;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return Visibility.Collapsed;
        }

        /// <summary>Persists this tool's name to the MCP execution-status whitelist, then runs the call.</summary>
        private async Task AllowThisToolAllTimeAsync()
        {
            var ts = InternalPage.Instance.ReadMCPToolsExecutionStatus();
            ts.EnableTool(TypedContent.ToolCall.Name);

            await ExecuteToolAsync();
        }

        /// <summary>Marks every MCP tool as auto-approved in the execution-status settings, then runs the call.</summary>
        private async Task AllowAnyToolAllTimeAsync()
        {
            var ts = InternalPage.Instance.ReadMCPToolsExecutionStatus();
            ts.EnableAllTools();

            await ExecuteToolAsync();
        }


        /// <summary>
        /// Invokes the tool through <see cref="McpServerProxyCollection"/> with its parsed arguments and
        /// updates the bubble's status to reflect success, failure, or that the call is still pending approval.
        /// </summary>
        private async Task ExecuteToolAsync()
        {
            try
            {
                SetStatus(ToolCallStatusEnum.Executing);

                var toolCall = TypedContent.ToolCall;

                var toolArguments = toolCall.ParseToolInvocationArguments();

                var toolResult = await McpServerProxyCollection.CallToolAsync(
                    toolCall.Name,
                    toolArguments,
                    cancellationToken: CancellationToken.None
                    );

                if (toolResult is null || toolResult.Result == McpServerProxyToolCallResultEnum.Fail)
                {
                    SetFailed($"Failed to execute the tools named {toolCall.Name}");
                }
                else if (toolResult.Result == McpServerProxyToolCallResultEnum.Success)
                {
                    SetSuccess(
                        string.Join("", toolResult.Content)
                        );
                }
                else
                {
                    //postponed
                    SetStatus(ToolCallStatusEnum.Asking);
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                SetFailed(excp);
            }
        }

        #region set execution status

        /// <summary>Updates the underlying content's status and notifies the UI that bound properties changed.</summary>
        private void SetStatus(ToolCallStatusEnum status)
        {
            TypedContent.SetStatus(status);

            OnPropertyChanged();
        }

        /// <summary>Records the tool call as succeeded with the given result text and refreshes the UI.</summary>
        private void SetSuccess(string successMessage)
        {
            TypedContent.SetResult(
                ToolCallStatusEnum.Succeeded,
                successMessage
                );

            OnPropertyChanged();
        }

        /// <summary>Records that the user blocked this single invocation of the tool and refreshes the UI.</summary>
        private void SetBlocked()
        {
            TypedContent.SetResult(
                ToolCallStatusEnum.Blocked,
                $"Invocation of the tool named {TypedContent.ToolCall.Name} has been blocked by the user."
                );

            OnPropertyChanged();
        }


        /// <summary>Formats an exception's message and stack trace and records it as the tool call's failure result.</summary>
        private void SetFailed(Exception excp)
        {
            SetStatus(ToolCallStatusEnum.Blocked);

            var result = excp.Message + Environment.NewLine + excp.StackTrace;

            SetFailed(result);
        }

        /// <summary>Records the tool call as failed with the given message and refreshes the UI.</summary>
        private void SetFailed(string failMessage)
        {
            TypedContent.SetResult(ToolCallStatusEnum.Failed, failMessage);

            OnPropertyChanged();
        }

        #endregion

    }
}