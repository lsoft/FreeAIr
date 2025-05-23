using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class DialogContextHelper
    {
        public static void FillMessageList(
            UserChatMessage contextMessage,
            List<OpenAI.Chat.ChatMessage> messageList
            )
        {
            messageList.Add(contextMessage);
        }
    }
}
