using WpfHelpers;

namespace FreeAIr.UI.Dialog
{
    public abstract class DialogContent : BaseViewModel
    {
        public DialogContentTypeEnum Type
        {
            get;
        }

        public object Tag
        {
            get;
        }

        protected DialogContent(
            DialogContentTypeEnum type,
            object tag
            )
        {
            Type = type;
            Tag = tag;
        }

        public bool IsSameTag(object tag)
        {
            if (Tag is null && tag is null)
            {
                return true;
            }
            if (tag is null && Tag is not null)
            {
                return false;
            }
            if (Tag is null && tag is not null)
            {
                return false;
            }

            return
                ReferenceEquals(Tag, tag)
                && Tag.GetType() == tag.GetType()
                ;
        }

    }
}