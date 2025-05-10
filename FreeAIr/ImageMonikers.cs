using Microsoft.VisualStudio.Imaging.Interop;

namespace FreeAIr
{
    public static class ImageMonikers
    {
        public static ImageMoniker Icon16
        {
            get;
        } = new ImageMoniker
        {
            Guid = new Guid("4edfe9dd-fe57-4a79-9c41-10d9eb76f4ae"),
            Id = 1
        };
        public static ImageMoniker Icon32
        {
            get;
        } = new ImageMoniker
        {
            Guid = new Guid("4edfe9dd-fe57-4a79-9c41-10d9eb76f4ae"),
            Id = 2
        };


    }
}
