using System.Collections.Generic;

namespace FreeAIr.UI.BLogic.Reader
{
    public static class LLMReaderPool
    {
        private static readonly object _locker = new();
        private static readonly Dictionary<FreeAIr.BLogic.Chat, LLMReader> _readers = new();

        public static void StartReaderFor(
            FreeAIr.BLogic.Chat chat
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

        public static async Task StopAndDeleteReaderForAsync(
            FreeAIr.BLogic.Chat chat
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


        public static async Task WaitForTaskAsync(
            FreeAIr.BLogic.Chat chat
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
