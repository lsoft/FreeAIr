using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeAIr.Chat.Context.Item
{
    public sealed class SimpleTextChatContextItem : IChatContextItem
    {
        private string _body;

        public string ContextUIDescription
        {
            get;
        }

        public bool IsAutoFound
        {
            get;
        }

        public SimpleTextChatContextItem(
            string contextUIDescription,
            string body,
            bool isAutoFound
            )
        {
            if (contextUIDescription is null)
            {
                throw new ArgumentNullException(nameof(contextUIDescription));
            }

            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            ContextUIDescription = contextUIDescription;
            _body = body;
            IsAutoFound = isAutoFound;
        }

        public Task<string> AsContextPromptTextAsync()
        {
            return Task.FromResult(_body);
        }

        public async Task<UserChatMessage> CreateChatMessageAsync()
        {
            return new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    _body
                    )
                );
        }

        public bool IsSame(IChatContextItem other)
        {
            if (other is not SimpleTextChatContextItem other2)
            {
                return false;
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(_body, other2._body) == 0;
        }

        public Task OpenInNewWindowAsync()
        {
            return Task.CompletedTask;
        }

        public void ReplaceWithText(string body)
        {
            _body = body;
        }

        public async Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync()
        {
            return [];
        }
    }
}
