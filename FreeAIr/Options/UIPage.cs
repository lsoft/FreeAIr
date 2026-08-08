using System.ComponentModel;

namespace FreeAIr
{
    /// <summary>
    /// Options page controlling the chat windows' layout and behavior: whether the in situ or tool
    /// chat window is focused after a prompt, whether the in situ window auto-closes, its saved size,
    /// and whether the chat list panel is shown.
    /// </summary>
    [Browsable(true)]
    public class UIPage : BaseOptionModel<UIPage>
    {
        /// <summary>
        /// Whether FreeAIr should switch focus to the in situ chat window after the developer submits
        /// a prompt. Takes priority over <see cref="SwitchToToolChatWindow"/>; has no effect if the
        /// caret is off-screen.
        /// </summary>
        [Category("UI")]
        [DisplayName("Opening in situ chat window")]
        [Description("Should FreeAIr switch to its in situ chat window after dev asked a prompt. If caret is out of the screen, the window will not be showed. This option have priority.")]
        [DefaultValue(true)]
        public bool SwitchToInSituChatWindow
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Whether FreeAIr should switch focus to the tool chat window after a prompt. Set this to
        /// true to open the tool window whenever the in situ window cannot be shown.
        /// </summary>
        [Category("UI")]
        [DisplayName("Opening tool chat window")]
        [Description("Should FreeAIr switch to its tool chat window after dev asked a prompt. Set this to true if you want to open tool chat window in any case when in situ window cannot be showed.")]
        [DefaultValue(true)]
        public bool SwitchToToolChatWindow
        {
            get;
            set;
        } = true;



        /// <summary>
        /// Whether FreeAIr should close the in situ chat window automatically when the user switches
        /// focus away from it.
        /// </summary>
        [Category("UI")]
        [DisplayName("Closing in situ window")]
        [Description("Should FreeAIr close in situ chat window is the user switched away from the window.")]
        [DefaultValue(false)]
        public bool CloseIfUserSwitchedAwayFromInSituWindow
        {
            get;
            set;
        } = false;


        /// <summary>
        /// Saved width, in device-independent pixels, of the in situ chat window.
        /// </summary>
        [Category("UI")]
        [DisplayName("InSitu Width")]
        [DefaultValue(700)]
        public double InSituWidth
        {
            get;
            set;
        } = 700;

        /// <summary>
        /// Saved height, in device-independent pixels, of the in situ chat window.
        /// </summary>
        [Category("UI")]
        [DisplayName("InSitu Height")]
        [DefaultValue(350)]
        public double InSituHeight
        {
            get;
            set;
        } = 350;


        /// <summary>
        /// Whether the chat list panel, which lists existing chats for switching between them, is shown.
        /// </summary>
        [Category("UI")]
        [DisplayName("Show chat list panel")]
        [DefaultValue(true)]
        public bool ShowChatListPanel
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Persists a new in situ chat window size, called whenever the user resizes the window,
        /// so it is restored next time the window opens.
        /// </summary>
        public void SetInSituSize(
            double width,
            double height
            )
        {
            InSituWidth = width;
            InSituHeight = height;
            this.Save();
        }
    }
}
