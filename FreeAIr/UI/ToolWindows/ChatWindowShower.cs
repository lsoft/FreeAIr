using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Text;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// Decides where a chat should appear - the in-situ popup anchored at the editor caret, or
    /// the docked chat list tool window - based on the caller's request and the UI options page,
    /// and opens it there.
    /// </summary>
    public static class ChatWindowShower
    {
        /// <summary>
        /// Shows the given chat either as an in-situ window positioned at the current caret, or
        /// in the docked <see cref="ChatListToolWindow"/>, honoring the forced overrides and the
        /// user's "switch to" preferences from the UI options page.
        /// </summary>
        public static async Task ShowChatWindowAsync(
            FreeAIr.Chat.Chat chat,
            bool inSituForce = false,
            bool toolForce = false
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (inSituForce || UIPage.Instance.SwitchToInSituChatWindow)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var documentView = await VS.Documents.GetActiveDocumentViewAsync();
                if (documentView != null)
                {
                    var textView = documentView.TextView;
                    if (textView != null)
                    {
                        var caretPoint = textView.Caret.Position.Point.GetPoint(
                            textView.TextSnapshot,
                            PositionAffinity.Successor
                            );

                        if (caretPoint.HasValue)
                        {
                            if (textView.TextViewLines.ContainsBufferPosition(caretPoint.Value))
                            {

                                // Получаем визуальные координаты символа
                                var visualPoint = textView.TextViewLines.GetCharacterBounds(caretPoint.Value);

                                // Преобразуем в экранные координаты с учетом скроллинга
                                var screenPos = textView.VisualElement.PointToScreen(
                                    new System.Windows.Point(
                                        visualPoint.Left - textView.ViewportLeft,
                                        visualPoint.Top - textView.ViewportTop
                                        )
                                    );

                                await InSituChatWindow.ShowAsync(
                                    chat,
                                    screenPos
                                    );
                                return;
                            }
                        }
                    }
                }
            }
            if (toolForce || UIPage.Instance.SwitchToToolChatWindow)
            {
                _ = await ChatListToolWindow.ShowAsync();
                return;
            }
        }
    }
}
