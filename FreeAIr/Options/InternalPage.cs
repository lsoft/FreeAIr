using System.ComponentModel;

namespace FreeAIr
{
    public class InternalPage : BaseOptionModel<InternalPage>
    {
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
