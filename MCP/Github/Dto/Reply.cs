namespace Dto
{
    public abstract class Reply
    {
        public string? ErrorMessage
        {
            get;
            set;
        }

        public static T FromError<T>(string error)
            where T : Reply, new()
        {
            return new T
            {
                ErrorMessage = error
            };
        }
    }

}
