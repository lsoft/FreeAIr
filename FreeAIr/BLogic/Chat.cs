﻿using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public sealed class Chat : IDisposable
    {
        private readonly ChatClient _chatClient;
        private readonly List<UserPrompt> _prompts = new();
        private readonly ChatContext _chatContext = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Action<Chat, Answer> _promptAnsweredCallBack;
        private Task? _task;

        public IChatContext ChatContext => _chatContext;

        private ChatStatusEnum _status;

        public IReadOnlyList<IUserPrompt> Prompts => _prompts;

        public event ChatStatusChangedDelegate ChatStatusChangedEvent;

        public ChatDescription Description
        {
            get;
        }

        public string ResultFilePath
        {
            get;
        }

        public DateTime? Started
        {
            get;
            private set;
        }

        /// <summary>
        /// Status of last prompt in the chat = whole chat status.
        /// </summary>
        public ChatStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                StatusChanged();
            }
        }

        public Chat(
            ChatDescription description,
            Action<Chat, Answer> promptAnsweredCallBack = null
            )
        {
            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            Description = description;
            _promptAnsweredCallBack = promptAnsweredCallBack;

            _status = ChatStatusEnum.NotStarted;

            Started = DateTime.Now;

            ResultFilePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString() + ".md"
                );

            _chatClient = new(
                model: ApiPage.Instance.ChosenModel,
                new ApiKeyCredential(
                    ApiPage.Instance.Token
                    ),
                new OpenAIClientOptions
                {
                    Endpoint = ApiPage.Instance.TryBuildEndpointUri(),
                }
                );

            _chatContext.ChatContextChangedEvent += ChatContextChangedRaised;
        }

        public void AddPrompt(
            UserPrompt userPrompt
            )
        {
            if (userPrompt is null)
            {
                throw new ArgumentNullException(nameof(userPrompt));
            }

            _prompts.Add(userPrompt);

            _task = Task.Run(async () => await AskPromptAndReceiveAnswerAsync(userPrompt));
        }

        public Task WaitForPromptResultAsync()
        {
            return WaitForTaskAsync();
        }

        public string ReadResponse()
        {
            if (File.Exists(ResultFilePath))
            {
                return ReadResponseFile();
            }

            return "Chat is not started yet.";
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();

            await WaitForTaskAsync();
        }

        public void Dispose()
        {
            DeleteResultFile();

            Description.Dispose();
        }

        private async Task WaitForTaskAsync()
        {
            if (_task is null)
            {
                return;
            }
            if (_task.IsCompleted)
            {
                return;
            }
            await _task;
        }

        private void ChatContextChangedRaised(object sender, ChatContextEventArgs e)
        {
            StatusChanged();
        }

        private async Task AskPromptAndReceiveAnswerAsync(UserPrompt userPrompt)
        {
            var answer = await AskAndWaitForAnswerInternalAsync(userPrompt);

            userPrompt.SetAnswer(answer.GetAnswer());

            if (_promptAnsweredCallBack is not null)
            {
                _promptAnsweredCallBack(this, answer);
            }
        }

        private async Task<Answer> AskAndWaitForAnswerInternalAsync(UserPrompt userPrompt)
        {
            var cancellationToken = _cancellationTokenSource.Token;

            var answer = new Answer(ResultFilePath);

            try
            {
                File.AppendAllText(
                    ResultFilePath,
                    Environment.NewLine + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    $"> {"UI_Prompt".GetLocalizedResourceByName()}:" + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    _prompts.Last().PromptBody + Environment.NewLine + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    $"> {"UI_Answer".GetLocalizedResourceByName()}:" + Environment.NewLine
                    );

                Status = ChatStatusEnum.WaitingForAnswer;

                var chatMessages = new List<ChatMessage>(_prompts.Count + 1);
                chatMessages.Add(
                    new SystemChatMessage(userPrompt.BuildRulesSection())
                    );
                chatMessages.AddRange(
                    _prompts.ConvertAll(p => new UserChatMessage(p.PromptBody))
                    );
                foreach (var contextItem in _chatContext.Items)
                {
                    chatMessages.Add(
                        new UserChatMessage(await contextItem.AsContextPromptTextAsync())
                        );
                }

                var completionUpdates = _chatClient.CompleteChatStreaming(
                    messages: chatMessages,
                    cancellationToken: cancellationToken
                    );
                foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.CompletionId is null)
                    {
                        answer.Append("Error reading answer (out of limit for free account?).");
                        Status = ChatStatusEnum.Failed;
                        return answer;
                    }

                    foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                    {
                        if (string.IsNullOrEmpty(contentPart.Text))
                        {
                            continue;
                        }

                        answer.Append(contentPart.Text);

                        Status = ChatStatusEnum.ReadingAnswer;

                        if (cancellationToken.IsCancellationRequested)
                        {
                            Status = ChatStatusEnum.Cancelled;
                            return answer;
                        }
                    }
                }

                Status = ChatStatusEnum.Ready;
            }
            catch (OperationCanceledException)
            {
                //this is OK
                Status = ChatStatusEnum.Cancelled;
            }
            catch (Exception excp)
            {
                answer.Append(excp.Message + Environment.NewLine + "```" + Environment.NewLine + excp.StackTrace + Environment.NewLine + "```");

                Status = ChatStatusEnum.Failed;

                //todo
            }

            return answer;
        }

        private string ReadResponseFile()
        {
            using var fs = new FileStream(
                ResultFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
                );

            using var sr = new StreamReader(fs);

            return sr.ReadToEnd();
        }

        private void DeleteResultFile()
        {
            if (File.Exists(ResultFilePath))
            {
                File.Delete(ResultFilePath);
            }
        }

        private void StatusChanged()
        {
            var e = ChatStatusChangedEvent;
            if (e is not null)
            {
                e(this, new ChatEventArgs(this));
            }
        }
    }

    public enum ChatKindEnum
    {
        ExplainCode,
        AddXmlComments,
        OptimizeCode,
        CompleteCodeAccordingComments,
        Discussion,
        GenerateCommitMessage,
        SuggestWholeLine,
        GenerateUnitTests
    }

    public sealed class Answer
    {
        private readonly StringBuilder _result = new();

        public string ResultFilePath
        {
            get;
        }

        public Answer(string resultFilePath)
        {
            ResultFilePath = resultFilePath;
        }

        public void Append(string contentPart)
        {
            _result.Append(contentPart);
            File.AppendAllText(ResultFilePath, contentPart);

        }

        public string GetAnswer()
        {
            return _result.ToString();
        }
    }

    public sealed class ChatDescription : IDisposable
    {
        public ChatKindEnum Kind
        {
            get;
        }
        public IOriginalTextDescriptor? SelectedTextDescriptor
        {
            get;
        }

        public ChatDescription(
            ChatKindEnum kind,
            IOriginalTextDescriptor? selectedTextDescriptor
            )
        {
            Kind = kind;
            SelectedTextDescriptor = selectedTextDescriptor;
        }

        public void Dispose()
        {
            SelectedTextDescriptor?.Dispose();
        }
    }

    public delegate void ChatStatusChangedDelegate(object sender, ChatEventArgs e);

    public sealed class ChatEventArgs : EventArgs
    {
        public Chat Chat
        {
            get;
        }

        public ChatEventArgs(Chat chat)
        {
            Chat = chat;
        }
    }

    public enum ChatStatusEnum
    {
        NotStarted,
        WaitingForAnswer,
        ReadingAnswer,
        Ready,
        Cancelled,
        Failed
    }
}
