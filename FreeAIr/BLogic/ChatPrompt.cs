using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public sealed class ChatPrompt
    {
        private readonly ChatClient _chatClient;
        public readonly UserPrompt _prompt;
        public readonly string _resultFilePath;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private Task? _task;
        private ChatPromptStatusEnum _status;

        public event ChatPromptStatusChangedDelegate ChatPromptStatusChangedEvent;

        public ChatPromptStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                StatusChanged();
            }
        }

        public ChatPrompt(
            ChatClient chatClient,
            UserPrompt prompt,
            string resultFilePath
            )
        {
            if (chatClient is null)
            {
                throw new ArgumentNullException(nameof(chatClient));
            }

            if (prompt is null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            if (string.IsNullOrEmpty(resultFilePath))
            {
                throw new ArgumentException($"'{nameof(resultFilePath)}' cannot be null or empty.", nameof(resultFilePath));
            }

            _chatClient = chatClient;
            _prompt = prompt;
            _resultFilePath = resultFilePath;
        }

        public void Ask()
        {
            File.AppendAllText(
                _resultFilePath,
                Environment.NewLine + Environment.NewLine
                );
            File.AppendAllText(
                _resultFilePath,
                "> Prompt:" + Environment.NewLine
                );
            File.AppendAllText(
                _resultFilePath,
                _prompt.PromptBody + Environment.NewLine + Environment.NewLine
                );
            File.AppendAllText(
                _resultFilePath,
                "> Answer:" + Environment.NewLine
                );

            _task = Task.Run(AskPromptAndReceiveAnswer);
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

        private void AskPromptAndReceiveAnswer()
        {
            try
            {
                Status = ChatPromptStatusEnum.WaitForAnswer;

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


                var completionUpdates = _chatClient.CompleteChatStreaming(
                    messages: [_prompt.GetPromptFullQuery()],
                    cancellationToken: cancellationToken
                    );
                foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.CompletionId is null)
                    {
                        File.AppendAllText(_resultFilePath, "Error reading answer (out of limit for free account?).");
                        Status = ChatPromptStatusEnum.Failed;
                        return;
                    }

                    foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                    {
                        if (string.IsNullOrEmpty(contentPart.Text))
                        {
                            continue;
                        }

                        File.AppendAllText(_resultFilePath, contentPart.Text);

                        Status = ChatPromptStatusEnum.ReadAnswer;

                        if (cancellationToken.IsCancellationRequested)
                        {
                            Status = ChatPromptStatusEnum.Cancelled;
                            return;
                        }
                    }
                }

                Status = ChatPromptStatusEnum.Completed;
            }
            catch (OperationCanceledException)
            {
                //this is OK
                Status = ChatPromptStatusEnum.Cancelled;
            }
            catch (Exception excp)
            {
                Status = ChatPromptStatusEnum.Failed;

                File.AppendAllText(_resultFilePath, excp.Message + Environment.NewLine + "```" + Environment.NewLine + excp.StackTrace + Environment.NewLine + "```");

                //todo
            }
        }


        private void StatusChanged()
        {
            var e = ChatPromptStatusChangedEvent;
            if (e is not null)
            {
                e(this, new ChatPromptEventArgs(this));
            }
        }

    }

    public delegate void ChatPromptStatusChangedDelegate(object sender, ChatPromptEventArgs e);

    public sealed class ChatPromptEventArgs : EventArgs
    {
        public ChatPrompt ChatPrompt
        {
            get;
        }

        public ChatPromptEventArgs(ChatPrompt chatPrompt)
        {
            ChatPrompt = chatPrompt;
        }
    }

    public enum ChatPromptStatusEnum
    {
        NotStarted,
        WaitForAnswer,
        ReadAnswer,
        Completed,
        Cancelled,
        Failed
    }

}
