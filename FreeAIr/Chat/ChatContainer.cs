using EnvDTE;
using EnvDTE80;
using FreeAIr.Options2.Agent;
using FreeAIr.Shared.Helper;
using FreeAIr.Interaction;
using FreeAIr.Chat.Persistence;
using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FreeAIr.Chat
{
    /// <summary>
    /// The owner of every live <see cref="Chat"/>.
    ///
    /// This is a MEF singleton: obtain it via `IComponentModel.GetService&lt;ChatContainer&gt;()`,
    /// never construct it. Besides keeping the collection, it aggregates the status of all chats
    /// into a single indicator shown by <see cref="IChatStatusIndicator"/>, and disposes everything when
    /// Visual Studio shuts down.
    /// </summary>
    [Export(typeof(ChatContainer))]
    public sealed class ChatContainer
    {
        /// <summary>
        /// Id of the most recently created chat. Commands invoked with Ctrl held down reuse this
        /// chat instead of starting a new one.
        /// </summary>
        public Guid? LastCreatedChatId
        {
            get;
            private set;
        }

        /// <summary>Guards every read and write of <see cref="_chats"/>.</summary>
        private readonly object _locker = new();

        /// <summary>The status bar indicator this container reports the aggregate chat status into.</summary>
        private readonly IChatStatusIndicator _statusIndicator;
        /// <summary>The live chats owned by this container.</summary>
        private readonly List<Chat> _chats = new();

        /// <summary>DTE shutdown events, subscribed to so chats are stopped before the shell tears down.</summary>
        private readonly DTEEvents _dteEvents;

        /// <summary>
        /// Solution open/close, so persisted chats are loaded when a solution appears and dropped
        /// from memory (not from disk) when it goes away.
        /// </summary>
        private readonly Community.VisualStudio.Toolkit.SolutionEvents _solutionEvents;

        /// <summary>
        /// The `.freeair\chats` folder currently loaded into <see cref="_chats"/>, or null when
        /// nothing has been loaded. Stops a second open of the same solution from duplicating rows.
        /// </summary>
        private string? _loadedChatsFolder;

        /// <summary>
        /// A snapshot of the live chats, not a view over the collection: chats are added and
        /// removed from the UI thread while their statuses change on the background threads which
        /// stream the answers, so handing out the list itself would break every enumeration of it.
        /// </summary>
        public IReadOnlyList<Chat> Chats
        {
            get
            {
                lock (_locker)
                {
                    return _chats.ToList();
                }
            }
        }

        /// <summary>Raised when a chat is added or removed, which rebuilds the chat list in the tool window.</summary>
        public event ChatCollectionChangedDelegate ChatCollectionChangedEvent;

        /// <summary>
        /// Relays the status change of any chat, so a subscriber does not have to subscribe to every
        /// chat and follow the collection as it changes.
        /// </summary>
        public event ChatStatusChangedDelegate ChatStatusChangedEvent;

        /// <summary>
        /// Composed by MEF with the informer it reports into, and hooks the DTE shutdown event here
        /// so that chats are stopped while the shell is still alive enough to close their documents.
        /// </summary>
        [ImportingConstructor]
        public ChatContainer(
            IChatStatusIndicator statusIndicator
            )
        {
            if (statusIndicator is null)
            {
                throw new ArgumentNullException(nameof(statusIndicator));
            }

            //DTE is not free-threaded; the imported status indicator asserts the same thing in its own
            //constructor, so this only makes the requirement of this class explicit as well
            ThreadHelper.ThrowIfNotOnUIThread();

            _statusIndicator = statusIndicator;

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
            {
                throw new InvalidOperationException("Cannot obtain DTE service.");
            }

            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;

            _solutionEvents = VS.Events.SolutionEvents;
            _solutionEvents.OnAfterOpenSolution += SolutionEvents_OnAfterOpenSolution;
            _solutionEvents.OnAfterCloseSolution += SolutionEvents_OnAfterCloseSolution;

            if (VS.Solutions.GetCurrentSolution() is not null)
            {
                LoadPersistedChatsAsync()
                    .FileAndForget(nameof(LoadPersistedChatsAsync));
            }
        }

        /// <summary>
        /// The chat a Ctrl-clicked command should continue, or null when there is none — it was
        /// never created, or the user has since closed it.
        /// </summary>
        public Chat? GetLastCreatedChat()
        {
            if (!LastCreatedChatId.HasValue)
            {
                return null;
            }

            lock (_locker)
            {
                return _chats.FirstOrDefault(c => c.Id == LastCreatedChatId.Value);
            }
        }

        /// <summary>
        /// Creates a chat and, if a prompt is given, immediately starts the first turn.
        /// Returns null if the chosen agent is not usable (no token, no endpoint, etc.);
        /// the error is reported to the user by the agent verification itself.
        /// </summary>
        public async Task<Chat?> StartChatAsync(
            ChatDescription kind,
            UserPrompt? prompt,
            FreeAIr.Chat.ChatOptions options
            )
        {
            if (kind is null)
            {
                throw new ArgumentNullException(nameof(kind));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!await options.ChosenAgent.VerifyAgentAndShowErrorIfNotAsync())
            {
                return null;
            }

            var chat = await Chat.CreateChatAsync(
                kind,
                options
                );
            chat.ChatStatusChangedEvent += ChatStatusChanged;
            //chat.PromptStateChangedEvent.Event += PromptStateChanged;

            lock (_locker)
            {
                _chats.Add(chat);
            }

            //подписчики - это viewmodel'и, они лезут обратно в контейнер за списком чатов;
            //звать их из-под лока значит однажды получить дедлок на ровном месте
            FireChatCollectionChanged();

            LastCreatedChatId = chat.Id;

            if (!options.AutomaticallyProcessed)
            {
                await TryEnablePersistenceAsync(chat);
                chat.PersistNow();
            }

            //последним, чтобы к моменту старта чтения чат уже был и в коллекции, и в LastCreatedChatId
            if (prompt is not null)
            {
                chat.AddPrompt(prompt);
            }

            return chat;
        }

        /// <summary>
        /// Closes a chat for good: stops the reader, unsubscribes, drops it from the collection and
        /// disposes it. Doing nothing for a chat which is not in the collection makes this safe to
        /// call twice, which the shutdown path and the close button both rely on.
        ///
        /// <paramref name="deletePersistentFile"/> is true when the user closed the chat and false
        /// when Visual Studio is shutting down or the solution is closing — in those cases the
        /// json stays so the transcript comes back next time.
        /// </summary>
        public async Task RemoveChatAsync(
            Chat chat,
            bool deletePersistentFile = true
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (!CheckIfChatIsInCollection(chat))
            {
                return;
            }

            await chat.StopAsync();

            chat.ChatStatusChangedEvent -= ChatStatusChanged;

            lock (_locker)
            {
                _chats.Remove(chat);
            }

            FireChatCollectionChanged();

            if (deletePersistentFile)
            {
                chat.DeletePersistentFile();
            }

            await chat.DisposeAsync();
        }


        /// <summary>
        /// Empties the collection at shutdown. Always takes the first chat and lets
        /// <see cref="RemoveChatAsync"/> remove it rather than iterating, because awaiting inside
        /// the loop lets the collection change underneath.
        ///
        /// Fire and forget: `OnBeginShutdown` is a synchronous COM callback which cannot be awaited,
        /// and blocking it would deadlock against the UI thread the chats are being closed on.
        /// </summary>
        private void RemoveAllChats()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    while (true)
                    {
                        Chat chat;
                        lock (_locker)
                        {
                            if (_chats.Count == 0)
                            {
                                break;
                            }

                            chat = _chats[0];
                        }

                        await RemoveChatAsync(chat, deletePersistentFile: false);
                    }
                }).FileAndForget(nameof(RemoveAllChats));
        }

        /// <summary>
        /// Cancels what a chat is doing but keeps it open, which is the stop button in the chat
        /// window as opposed to the close one.
        /// </summary>
        public async Task StopChatAsync(
            Chat chat
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (!CheckIfChatIsInCollection(chat))
            {
                return;
            }

            await chat.StopAsync();
        }

        /// <summary>
        /// Whether this very chat is still held. Compares by reference rather than by id, because
        /// the question being asked is whether this object is the live one, not whether some chat
        /// with the same id exists.
        /// </summary>
        private bool CheckIfChatIsInCollection(Chat chat)
        {
            lock (_locker)
            {
                if (_chats.Any(c => ReferenceEquals(c, chat)))
                {
                    return true;
                }
            }

            return false;
        }

        //private void PromptStateChanged(object sender, PromptAddedEventArgs pea)
        //{
        //    var e = PromptStateChangedEvent;
        //    if (e is not null)
        //    {
        //        e(this, pea);
        //    }
        //}


        /// <summary>
        /// Collapses the states of all chats into the one thing the status bar can show: working if
        /// any chat is waiting for or reading an answer, idle otherwise. Runs on whichever thread
        /// streamed the change, so the informer is the one that marshals to the UI.
        /// </summary>
        private void ChatStatusChanged(object sender, ChatEventArgs ea)
        {
            bool anyIsInProgress;
            lock (_locker)
            {
                anyIsInProgress = _chats.Any(c => c.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer));
            }

            if (anyIsInProgress)
            {
                _statusIndicator.UpdateStatus(ChatsStatusEnum.Working);
            }
            else
            {
                _statusIndicator.UpdateStatus(ChatsStatusEnum.Idle);
            }

            FireChatStatusChanged(ea);
        }

        /// <summary>Raises <see cref="ChatCollectionChangedEvent"/> if anyone is listening.</summary>
        private void FireChatCollectionChanged()
        {
            var e = ChatCollectionChangedEvent;
            if (e is not null)
            {
                e(this, new EventArgs());
            }
        }
        
        /// <summary>Raises <see cref="ChatStatusChangedEvent"/> if anyone is listening.</summary>
        private void FireChatStatusChanged(ChatEventArgs ea)
        {
            var e = ChatStatusChangedEvent;
            if (e is not null)
            {
                e(this, ea);
            }
        }

        /// <summary>
        /// Visual Studio is closing: cancel every request in flight. Without this the readers keep
        /// streaming into objects the shell is tearing down, which surfaces as an exception in the
        /// activity log on every exit. Persisted chats keep their files.
        /// </summary>
        private void DTEEvents_OnBeginShutdown()
        {
            RemoveAllChats();
        }

        /// <summary>A solution has appeared: load the user chats that were saved next to it.</summary>
        private void SolutionEvents_OnAfterOpenSolution(Community.VisualStudio.Toolkit.Solution solution)
        {
            LoadPersistedChatsAsync()
                .FileAndForget(nameof(LoadPersistedChatsAsync));
        }

        /// <summary>
        /// The solution is gone: drop its persisted chats from memory so they are not mixed with
        /// the next solution's, and so opening the same solution again does not duplicate them.
        /// The json files stay.
        /// </summary>
        private void SolutionEvents_OnAfterCloseSolution()
        {
            UnloadPersistedChatsAsync()
                .FileAndForget(nameof(UnloadPersistedChatsAsync));
        }

        /// <summary>
        /// If `.freeair\chats` can be resolved, marks <paramref name="chat"/> persistent. A missing
        /// solution (or any other failure to see the folder) leaves the chat in memory only.
        /// </summary>
        private static async Task TryEnablePersistenceAsync(Chat chat)
        {
            var folder = await ChatPersistence.TryGetChatsFolderPathAsync();
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            chat.EnablePersistence(
                ChatPersistence.GetChatFilePath(folder, chat.Id)
                );
        }

        /// <summary>
        /// Reads every chat json next to the current solution and puts them in the collection.
        /// Skips a folder that is already loaded, so the constructor and the solution-opened
        /// event do not both add the same files.
        /// </summary>
        private async Task LoadPersistedChatsAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var folder = await ChatPersistence.TryGetChatsFolderPathAsync();
                if (string.Equals(folder, _loadedChatsFolder, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                await UnloadPersistedChatsCoreAsync();

                _loadedChatsFolder = folder;
                if (string.IsNullOrEmpty(folder))
                {
                    return;
                }

                var restored = new List<Chat>();
                foreach (var filePath in ChatPersistence.ListChatFiles(folder))
                {
                    var payload = ChatPersistence.TryLoad(filePath);
                    if (payload is null)
                    {
                        continue;
                    }

                    try
                    {
                        var chat = await Chat.CreateFromPersistedAsync(payload, filePath);
                        if (chat is not null)
                        {
                            restored.Add(chat);
                        }
                    }
                    catch (Exception excp)
                    {
                        excp.ActivityLogException();
                    }
                }

                foreach (var chat in restored.OrderBy(c => c.Started))
                {
                    chat.ChatStatusChangedEvent += ChatStatusChanged;
                    lock (_locker)
                    {
                        _chats.Add(chat);
                    }
                }

                var last = restored
                    .OrderByDescending(c => c.Started)
                    .FirstOrDefault();
                if (last is not null)
                {
                    LastCreatedChatId = last.Id;
                }

                if (restored.Count > 0)
                {
                    FireChatCollectionChanged();
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>Drops persisted chats from memory because the solution closed.</summary>
        private async Task UnloadPersistedChatsAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await UnloadPersistedChatsCoreAsync();
                _loadedChatsFolder = null;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Removes every persistent chat from the collection without deleting their files.
        /// Automatic and non-persistent user chats are left alone.
        /// </summary>
        private async Task UnloadPersistedChatsCoreAsync()
        {
            while (true)
            {
                Chat? chat;
                lock (_locker)
                {
                    chat = _chats.FirstOrDefault(c => c.IsPersistent);
                    if (chat is null)
                    {
                        break;
                    }
                }

                await RemoveChatAsync(chat, deletePersistentFile: false);
            }
        }

    }

    /// <summary>Handler shape of <see cref="ChatContainer.ChatCollectionChangedEvent"/>.</summary>
    public delegate void ChatCollectionChangedDelegate(object sender, EventArgs e);
}
