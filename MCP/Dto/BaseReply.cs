namespace Dto
{
    public abstract class BaseReply
    {
        public string? ErrorMessage
        {
            get;
            set;
        }

        public static T FromError<T>(string error)
            where T : BaseReply, new()
        {
            return new T
            {
                ErrorMessage = error
            };
        }
    }

}
