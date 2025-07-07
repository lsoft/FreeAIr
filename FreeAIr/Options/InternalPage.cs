using System.ComponentModel;

namespace FreeAIr
{
    [Browsable(false)]
    public class InternalPage : BaseOptionModel<InternalPage>
    {

        [Category("Internal options")]
        [DisplayName("Internal option")]
        [Description("This page is used to store an internal options of FreeAIr, so you see nothing here.")]
        [DefaultValue("")]
        public string Empty
        {
            get;
            set;
        } = string.Empty;

        [Category("Internal options")]
        [DisplayName("All")]
        [Description("Options for FreeAIr")]
        [DefaultValue("")]
        [Browsable(false)]
        public string Options
        {
            get;
            set;
        }

        [Category("Logic")]
        [DisplayName("FreeAIr Last Version")]
        [DefaultValue("2.0.0")]
        [Browsable(false)]
        public string FreeAIrLastVersion
        {
            get;
            set;
        }
    }

}
