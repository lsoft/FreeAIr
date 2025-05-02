using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public sealed class Chat : IDisposable
    {
        private readonly ChatClient _chatClient;
        private readonly List<UserPrompt> _prompts = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private Task? _task;


        private ChatStatusEnum _status;

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
            ChatDescription description
            )
        {
            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            Description = description;

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
                    Endpoint = new Uri(ApiPage.Instance.Endpoint),
                }
                );
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

            _task = Task.Run(AskPromptAndReceiveAnswer);
        }

        public string ReadResponse()
        {
            if (File.Exists(ResultFilePath))
            {
                return ReadResponseFile();
            }

            return "Chat is not started yet. Please wait for completion.";
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();

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

        public void Dispose()
        {
            _chatClient.CompleteChat();

            DeleteResultFile();
        }


        private void AskPromptAndReceiveAnswer()
        {
            try
            {
                File.AppendAllText(
                    ResultFilePath,
                    Environment.NewLine + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    "> Prompt:" + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    _prompts.Last().PromptBody + Environment.NewLine + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    "> Answer:" + Environment.NewLine
                    );

                Status = ChatStatusEnum.WaitForAnswer;

                var cancellationToken = _cancellationTokenSource.Token;


                //                File.AppendAllText(_resultFilePath, @"
                //> Prompt:
                //>
                //> Show me some C# code, please!


                //sds;kjnhfndsglkjn

                //as;fdfgkjhjfdjk

                //alkfsjdghlkjfgh

                //> Prompt:
                //Show me some C# code, please!


                //> Answer:
                //# Header

                //Text.

                //```csharp
                //        //comment comment
                //        //and again
                //        public static int IndexOf<T>(this T[] array, T value)
                //            where T : IComparable
                //        {
                //            for (var c = 0; c < array.Length; c++)
                //            {
                //                if (array[c].CompareTo(value) == 0)
                //                {
                //                    return c;
                //                }
                //            }

                //            return -1;
                //        }
                //```
                //");
                //                Status = ChatPromptStatusEnum.Completed;
                //                return;


                var chatMessages = new List<ChatMessage>(_prompts.Count + 1);
                chatMessages.Add(new UserChatMessage(UserPrompt.BuildRulesSection()));
                chatMessages.AddRange(
                    _prompts
                    .ConvertAll(p => new UserChatMessage(p.PromptBody))
                    );

                var completionUpdates = _chatClient.CompleteChatStreaming(
                    messages: chatMessages,
                    cancellationToken: cancellationToken
                    );
                foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.CompletionId is null)
                    {
                        File.AppendAllText(ResultFilePath, "Error reading answer (out of limit for free account?).");
                        Status = ChatStatusEnum.Failed;
                        return;
                    }

                    foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                    {
                        if (string.IsNullOrEmpty(contentPart.Text))
                        {
                            continue;
                        }

                        File.AppendAllText(ResultFilePath, contentPart.Text);

                        Status = ChatStatusEnum.ReadAnswer;

                        if (cancellationToken.IsCancellationRequested)
                        {
                            Status = ChatStatusEnum.Cancelled;
                            return;
                        }
                    }
                }

                Status = ChatStatusEnum.Completed;
            }
            catch (OperationCanceledException)
            {
                //this is OK
                Status = ChatStatusEnum.Cancelled;
            }
            catch (Exception excp)
            {
                Status = ChatStatusEnum.Failed;

                File.AppendAllText(ResultFilePath, excp.Message + Environment.NewLine + "```" + Environment.NewLine + excp.StackTrace + Environment.NewLine + "```");

                //todo
            }
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
        AddComments,
        OptimizeCode,
        CompleteCodeAccordingComments,
        Discussion
    }

    public sealed class ChatDescription
    {
        public ChatKindEnum Kind
        {
            get;
        }

        public string FileName
        {
            get;
        }

        public ChatDescription(ChatKindEnum kind, string fileName)
        {
            Kind = kind;
            FileName = fileName;
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
        WaitForAnswer,
        ReadAnswer,
        Completed,
        Cancelled,
        Failed
    }
}
