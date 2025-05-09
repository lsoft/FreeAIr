namespace FreeAIr.UI.Embedillo
{
    public sealed class ControlPositionInfo
    {
        public readonly int Offset;
        public readonly int Length;

        public ControlPositionInfo(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }
    }
}