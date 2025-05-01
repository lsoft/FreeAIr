using EnvDTE;
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

namespace FreeAIr.BLogic.Tasks
{
    public sealed class AITask : IDisposable
    {
        private readonly ChatClient _chatClient;
        private readonly string _query;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private AITaskStatusEnum _status;
        
        private JoinableTask? _task;

        public event TaskStatusChangedDelegate TaskStatusChangedEvent;

        public TaskKind Kind
        {
            get;
        }

        public string ResultFilePath
        {
            get;
            private set;
        }

        public DateTime? Started
        {
            get;
            private set;
        }

        public AITaskStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                StatusChanged();
            }
        }

        private void StatusChanged()
        {
            var e = TaskStatusChangedEvent;
            if (e is not null)
            {
                e(this, new TaskEventArgs(this));
            }
        }

        public AITask(
            TaskKind kind,
            string query
            )
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            Kind = kind;
            _query = query;

            _status = AITaskStatusEnum.NotStarted;

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

        public void Run()
        {
            ResultFilePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString() + ".md"
                );

            _task = ThreadHelper.JoinableTaskFactory.RunAsync(RunAsync);
        }
        
        public string ReadResponse()
        {
            switch (Status)
            {
                case AITaskStatusEnum.NotStarted:
                    return "Task is not started yet. Please wait for completion.";
                case AITaskStatusEnum.WaitForAnswer:
                    return "Task is waiting for AI answer. Please wait for completion.";
                case AITaskStatusEnum.ReadAnswer:
                case AITaskStatusEnum.Completed:
                    return ReadResponseFile();
                case AITaskStatusEnum.Cancelled:
                    return "Task is cancelled. No response exists.";
                case AITaskStatusEnum.Failed:
                    return "Task is failed: " + Environment.NewLine + ReadResponseFile();
                default:
                    return "Task is in unknown status. Please fire an issue to the dev.";
            }
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

        private async Task RunAsync()
        {
            try
            {
                Status = AITaskStatusEnum.WaitForAnswer;
                Started = DateTime.Now;

                var cancellationToken = _cancellationTokenSource.Token;


                File.AppendAllText(ResultFilePath, @"
# Header

Text.

```csharp
        //comment comment
        //and again
        public static int IndexOf<T>(this T[] array, T value)
            where T : IComparable
        {
            for (var c = 0; c < array.Length; c++)
            {
                if (array[c].CompareTo(value) == 0)
                {
                    return c;
                }
            }

            return -1;
        }
```
");

                //var completionUpdates = _chatClient.CompleteChatStreamingAsync(
                //    messages: [_query],
                //    cancellationToken: cancellationToken
                //    );
                //await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                //{
                //    if (completionUpdate.CompletionId is null)
                //    {
                //        File.AppendAllText(ResultFilePath, "Error reading answer (out of limit for free account?).");
                //        Status = AITaskStatusEnum.Failed;
                //        return;
                //    }

                //    foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                //    {
                //        if (string.IsNullOrEmpty(contentPart.Text))
                //        {
                //            continue;
                //        }

                //        File.AppendAllText(ResultFilePath, contentPart.Text);

                //        Status = AITaskStatusEnum.ReadAnswer;

                //        if (cancellationToken.IsCancellationRequested)
                //        {
                //            Status = AITaskStatusEnum.Cancelled;
                //            return;
                //        }
                //    }
                //}

                Status = AITaskStatusEnum.Completed;
            }
            catch (OperationCanceledException)
            {
                //this is OK
                Status = AITaskStatusEnum.Cancelled;
            }
            catch (Exception excp)
            {
                Status = AITaskStatusEnum.Failed;

                File.AppendAllText(ResultFilePath, excp.Message + Environment.NewLine + "```" + Environment.NewLine + excp.StackTrace + Environment.NewLine + "```");

                //todo
            }
        }
    }

    public enum AITaskStatusEnum
    {
        NotStarted,
        WaitForAnswer,
        ReadAnswer,
        Completed,
        Cancelled,
        Failed
    }

    public enum AITaskKindEnum
    {
        ExplainCode,
        AddComments,
        OptimizeCode,
        CompleteCodeAccordingComments,
        GenerateCode
    }

    public sealed class TaskKind
    {
        public AITaskKindEnum Kind
        {
            get;
        }

        public string FileName
        {
            get;
        }

        public TaskKind(AITaskKindEnum kind, string fileName)
        {
            Kind = kind;
            FileName = fileName;
        }
    }

    public delegate void TaskStatusChangedDelegate(object sender, TaskEventArgs e);

    public sealed class TaskEventArgs : EventArgs
    {
        public AITask Task
        {
            get;
        }

        public TaskEventArgs(AITask task)
        {
            Task = task;
        }
    }

}
