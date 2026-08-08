using System.Collections.Generic;

namespace FreeAIr.BLogic.Reader
{
    /// <summary>
    /// Keeps at most one <see cref="LLMReader"/> per chat.
    ///
    /// A reader is created lazily on the first request for its chat and lives until the chat is
    /// stopped or removed. The reader itself ignores a start request while it is already reading,
    /// so calling <see cref="StartReaderFor"/> more than once per turn is harmless.
    /// </summary>
    public static class LLMReaderPool
    {
        /// <summary>Guards access to <see cref="_readers"/> across concurrent chat operations.</summary>
        private static readonly object _locker = new();
        /// <summary>The one live reader per chat, keyed by chat instance.</summary>
        private static readonly Dictionary<FreeAIr.Chat.Chat, LLMReader> _readers = new();

        /// <summary>
        /// Starts (or restarts) reading the model's answer for the given chat.
        /// </summary>
        public static void StartReaderFor(
            FreeAIr.Chat.Chat chat
            )
        {
            lock (_locker)
            {
                if (!_readers.TryGetValue(chat, out var reader))
                {
                    reader = new LLMReader(
                        chat
                        );
                    _readers[chat] = reader;
                }

                reader.AsyncStartRead();
            }
        }

        /// <summary>
        /// Cancels the reading (if any) and forgets the reader. Called when a chat is stopped by
        /// the user or removed from the container.
        /// </summary>
        public static async Task StopAndDeleteReaderForAsync(
            FreeAIr.Chat.Chat chat
            )
        {
            LLMReader? reader = null;
            lock (_locker)
            {
                if (!_readers.TryGetValue(chat, out reader))
                {
                    return;
                }

                _readers.Remove(chat);
            }

            if (reader is null)
            {
                return;
            }

            await reader.StopSafelyAsync();
            reader.Dispose();
        }


        /// <summary>
        /// Awaits the current reading of the chat, if it is reading right now.
        /// Returns immediately when there is nothing in flight.
        /// </summary>
        public static async Task WaitForTaskAsync(
            FreeAIr.Chat.Chat chat
            )
        {
            LLMReader? reader = null;
            lock (_locker)
            {
                if (!_readers.TryGetValue(chat, out reader))
                {
                    return;
                }
            }

            if (reader is null)
            {
                return;
            }

            await reader.WaitForTaskAsync();
        }

    }
}
