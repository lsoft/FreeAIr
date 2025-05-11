using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.BLogic.Context
{
    public sealed class ChatContext : IChatContext
    {
        private readonly List<IChatContextItem> _items = new();

        public event ChatContextChangedDelegate ChatContextChangedEvent;

        public IReadOnlyList<IChatContextItem> Items => _items;

        public void RemoveItem(
            IChatContextItem item
            )
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var removedCount = _items.RemoveAll(i => i.IsSame(item));
            if (removedCount > 0)
            {
                RaiseChatContextChanged();
            }
        }

        public void AddItem(
            IChatContextItem item
            )
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (_items.Any(i => i.IsSame(item)))
            {
                return;
            }

            _items.Add(item);

            RaiseChatContextChanged();
        }

        public void RemoveAutomaticItems()
        {
            var removedCount = _items.RemoveAll(i => i.IsAutoFound);
            if (removedCount > 0)
            {
                RaiseChatContextChanged();
            }
        }

        private void RaiseChatContextChanged()
        {
            var e = ChatContextChangedEvent;
            if (e is not null)
            {
                e(this, new ChatContextEventArgs(this));
            }
        }

    }

    public delegate void ChatContextChangedDelegate(object sender, ChatContextEventArgs e);
}
