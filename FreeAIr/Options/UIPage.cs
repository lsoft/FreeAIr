using System.ComponentModel;

namespace FreeAIr
{
    [Browsable(true)]
    public class UIPage : BaseOptionModel<UIPage>
    {
        [Category("UI")]
        [DisplayName("Opening in situ chat window")]
        [Description("Should FreeAIr switch to its in situ chat window after dev asked a prompt. If caret is out of the screen, the window will not be showed. This option have priority.")]
        [DefaultValue(true)]
        public bool SwitchToInSituChatWindow
        {
            get;
            set;
        } = true;

        [Category("UI")]
        [DisplayName("Opening tool chat window")]
        [Description("Should FreeAIr switch to its tool chat window after dev asked a prompt. Set this to true if you want to open tool chat window in any case when in situ window cannot be showed.")]
        [DefaultValue(true)]
        public bool SwitchToToolChatWindow
        {
            get;
            set;
        } = true;



        [Category("UI")]
        [DisplayName("Closing in situ window")]
        [Description("Should FreeAIr close in situ chat window is the user switched away from the window.")]
        [DefaultValue(false)]
        public bool CloseIfUserSwitchedAwayFromInSituWindow
        {
            get;
            set;
        } = false;


        [Category("UI")]
        [DisplayName("InSitu Width")]
        [DefaultValue(700)]
        public double InSituWidth
        {
            get;
            set;
        } = 700;

        [Category("UI")]
        [DisplayName("InSitu Height")]
        [DefaultValue(350)]
        public double InSituHeight
        {
            get;
            set;
        } = 350;


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
