using WpfHelpers;
using FreeAIr.Chat.Content;

namespace FreeAIr.UI.Dialog
{
    /// <summary>Strongly-typed base for a chat bubble's view model, giving derived dialog contents direct access to their specific <see cref="IChatContent"/> subtype.</summary>
    public abstract class DialogContent<T> : DialogContent
        where T : IChatContent
    {
        /// <summary>The chat content backing this bubble, exposed as its concrete type.</summary>
        public T TypedContent
        {
            get;
        }

        /// <summary>Stores the typed content alongside the base class's untyped reference.</summary>
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

    /// <summary>
    /// Base view model for one bubble in the chat window's item list, pairing the underlying
    /// <see cref="IChatContent"/> with an opaque tag used to correlate it back to the UI element that
    /// requested its display (e.g. a specific dialog template).
    /// </summary>
    public abstract class DialogContent : BaseViewModel
    {
        /// <summary>The chat content this bubble renders (a prompt, an answer, a tool call, etc).</summary>
        public IChatContent Content
        {
            get;
        }

        /// <summary>Opaque identifier used to check whether two dialog contents were created for the same logical bubble.</summary>
        public object Tag
        {
            get;
        }

        /// <summary>Validates and stores the chat content and its correlation tag.</summary>
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

        /// <summary>Checks whether the given tag identifies the same bubble as this content's <see cref="Tag"/>.</summary>
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