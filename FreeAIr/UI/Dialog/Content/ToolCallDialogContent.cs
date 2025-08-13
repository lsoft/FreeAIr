using FreeAIr.BLogic.Content;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Shared.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.Json;
using System.Threading;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.Dialog.Content
{
    public sealed class ToolCallDialogContent : DialogContent<ToolCallChatContent>
    {
        private ICommand? _clickCommand;
        private ICommand? _allowThisToolAllTimeCommand;
        private ICommand? _allowAnyToolAllTimeCommand;
        private ICommand? _blockAtThisTimeCommand;
        private ICommand? _showResultCommand;

        public ToolCallStatusEnum Status => TypedContent.Status;

        public string Name => TypedContent.Name;

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

        public ICommand ClickCommand
        {
            get
            {
                if (_clickCommand is null)
                {
                    _clickCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            try
                            {
                                SetStatus(ToolCallStatusEnum.Executing);

                                await ExecuteToolAsync();
                            }
                            catch (Exception excp)
                            {
                                excp.ActivityLogException();

                                SetFailed(excp);
                            }

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

                return _clickCommand;
            }
        }

        public ICommand BlockAtThisTimeCommand
        {
            get
            {
                if (_blockAtThisTimeCommand is null)
                {
                    _blockAtThisTimeCommand = new RelayCommand(
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

                return _blockAtThisTimeCommand;
            }
        }

        public ICommand AllowThisToolAllTimeCommand
        {
            get
            {
                if (_allowThisToolAllTimeCommand is null)
                {
                    _allowThisToolAllTimeCommand = new AsyncRelayCommand(
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

                return _allowThisToolAllTimeCommand;
            }
        }

        public ICommand AllowAnyToolAllTimeCommand
        {
            get
            {
                if (_allowAnyToolAllTimeCommand is null)
                {
                    _allowAnyToolAllTimeCommand = new AsyncRelayCommand(
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

                return _allowAnyToolAllTimeCommand;
            }
        }

        public ICommand ShowResultCommand
        {
            get
            {
                if (_showResultCommand is null)
                {
                    _showResultCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await VS.MessageBox.ShowAsync(
                                $"{TypedContent.ToolCall.FunctionName} call result:",
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

                return _showResultCommand;
            }
        }

        public ToolCallDialogContent(
            ToolCallChatContent content
            ) : base(content, content)
        {
        }

        private async Task AllowThisToolAllTimeAsync()
        {
            await ExecuteToolAsync();
        }

        private async Task AllowAnyToolAllTimeAsync()
        {
            await ExecuteToolAsync();
        }


        private async Task ExecuteToolAsync()
        {
            var toolCall = TypedContent.ToolCall;

            var toolArguments = ParseToolInvocationArguments(toolCall);

            var toolResult = await McpServerProxyCollection.CallToolAsync(
                toolCall.FunctionName,
                toolArguments,
                cancellationToken: CancellationToken.None
                );
            if (toolResult is null)
            {
                SetFailed($"Failed to execute the tools named {toolCall.FunctionName}");
            }
            else
            {
                SetSuccess(
                    string.Join("", toolResult)
                    );
            }
        }

        #region set execution status

        private void SetStatus(ToolCallStatusEnum status)
        {
            TypedContent.SetStatus(status);

            OnPropertyChanged();
        }

        private void SetSuccess(string successMessage)
        {
            TypedContent.SetResult(
                ToolCallStatusEnum.Succeeded,
                successMessage
                );

            OnPropertyChanged();
        }

        private void SetBlocked()
        {
            TypedContent.SetResult(
                ToolCallStatusEnum.Blocked,
                $"Invocation of the tool named {TypedContent.ToolCall.FunctionName} has been blocked by the user."
                );

            OnPropertyChanged();
        }


        private void SetFailed(Exception excp)
        {
            SetStatus(ToolCallStatusEnum.Blocked);

            var result = excp.Message + Environment.NewLine + excp.StackTrace;

            SetFailed(result);
        }

        private void SetFailed(string failMessage)
        {
            TypedContent.SetResult(ToolCallStatusEnum.Failed, failMessage);

            OnPropertyChanged();
        }

        #endregion

        private static Dictionary<string, object?> ParseToolInvocationArguments(
            StreamingChatToolCallUpdate toolCall
            )
        {
            var toolArguments = new Dictionary<string, object?>();
            if (toolCall.FunctionArgumentsUpdate.ToMemory().Length > 0)
            {
                using JsonDocument toolArgumentJson = JsonDocument.Parse(
                    toolCall.FunctionArgumentsUpdate
                    );

                foreach (var pair in toolArgumentJson.RootElement.EnumerateObject())
                {
                    toolArguments.Add(pair.Name, pair.Value.DeserializeToObject());
                }
            }

            return toolArguments;
        }


    }
}