using System.Collections.Generic;

namespace FreeAIr.BLogic.Context
{
    public interface IChatContext
    {
        IReadOnlyList<IChatContextItem> Items
        {
            get;
        }

        void AddItem(IChatContextItem item);

        void AddItems(IReadOnlyList<IChatContextItem> items);

        void RemoveAutomaticItems();
        
        void RemoveItem(IChatContextItem item);
    }

}
