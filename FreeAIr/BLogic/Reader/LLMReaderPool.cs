using FreeAIr.UI.ViewModels;
using MarkdownParser.Antlr.Answer;
using System.Collections.Generic;

namespace FreeAIr.UI.BLogic.Reader
{
    public static class LLMReaderPool
    {
        private static readonly object _locker = new();
        private static readonly Dictionary<FreeAIr.BLogic.Chat, LLMReader> _readers = new();

        public static void AddAndStartReader(
            FreeAIr.BLogic.Chat chat,
            DialogViewModel dialog,
            AdditionalCommandContainer? additionalCommandContainer
            )
        {
            lock (_locker)
            {
                if (!_readers.TryGetValue(chat, out var reader))
                {
                    reader = new LLMReader(
                        chat,
                        dialog,
                        additionalCommandContainer
                        );
                    _readers[chat] = reader;
                }

                reader.AsyncStartRead();
            }
        }

        public static async Task StopAndDeleteReaderAsync(
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
