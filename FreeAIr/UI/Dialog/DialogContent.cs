using FreeAIr.BLogic.Content;
using WpfHelpers;

namespace FreeAIr.UI.Dialog
{
    public abstract class DialogContent<T> : DialogContent
        where T : IChatContent
    {
        public T TypedContent
        {
            get;
        }

        protected DialogContent(
            T content,
            object tag
            ) : base(content, tag)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            TypedContent = content;
        }
    }

    public abstract class DialogContent : BaseViewModel
    {
        public IChatContent Content
        {
            get;
        }

        public object Tag
        {
            get;
        }

        protected DialogContent(
            IChatContent content,
            object tag
            )
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Content = content;
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