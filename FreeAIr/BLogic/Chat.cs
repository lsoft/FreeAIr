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
        private readonly SemaphoreSlim _semaphore = new(1);

        private readonly ChatClient _chatClient;
        private readonly List<ChatPrompt> _prompts = new();

        private ChatPromptStatusEnum _status;

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
        public ChatPromptStatusEnum Status
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

            _status = ChatPromptStatusEnum.NotStarted;

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
            UserPrompt prompt
            )
        {
            if (prompt is null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            var chatPrompt = new ChatPrompt(
                _chatClient,
                prompt,
                ResultFilePath
                );

            _semaphore.Wait();
            try
            {
                UnsubscribeLastPrompt();

                _prompts.Add(chatPrompt);
            }
            finally
            {
                _semaphore.Release();
            }

            chatPrompt.ChatPromptStatusChangedEvent += ChatPromptStatusChangedEvent;
            chatPrompt.Ask();
        }

        public string ReadResponse()
        {
            if (File.Exists(ResultFilePath))
            {
                return ReadResponseFile();
            }

            return "Chat is not started yet. Please wait for completion.";

            //switch (Status)
            //{
            //    case PromptStatusEnum.NotStarted:
            //        return "Chat is not started yet. Please wait for completion.";
            //    case PromptStatusEnum.WaitForAnswer:
            //        return "Chat is waiting for AI answer. Please wait for completion.";
            //    case PromptStatusEnum.ReadAnswer:
            //    case PromptStatusEnum.Completed:
            //        return ReadResponseFile();
            //    case PromptStatusEnum.Cancelled:
            //        return "Chat is cancelled. No response exists.";
            //    case PromptStatusEnum.Failed:
            //        return "Chat is failed: " + Environment.NewLine + ReadResponseFile();
            //    default:
            //        return "Chat is in unknown status. Please fire an issue to the dev.";
            //}
        }

        public async Task StopAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_prompts.Count > 0)
                {
                    var lastPrompt = _prompts.Last();
                    await lastPrompt.StopAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _chatClient.CompleteChat();

            _semaphore.Wait();
            try
            {
                UnsubscribeLastPrompt();
            }
            finally
            {
                _semaphore.Release();
            }

            DeleteResultFile();
        }

        private void UnsubscribeLastPrompt()
        {
            if (_prompts.Count > 0)
            {
                var lastPrompt = _prompts.Last();
                lastPrompt.ChatPromptStatusChangedEvent -= ChatPromptStatusChangedEvent;
            }
        }

        private void ChatPromptStatusChangedEvent(
            object sender,
            ChatPromptEventArgs e
            )
        {
            Status = e.ChatPrompt.Status;
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
        GenerateCode
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

}
