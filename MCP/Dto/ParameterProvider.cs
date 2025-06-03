namespace Dto
{
    public interface IParameterProvider
    {
        string this[string key]
        {
            get;
        }

        bool TryGetValue(string key, out string? value);
    }

    public sealed class FakeParameterProvider : IParameterProvider
    {
        public static readonly FakeParameterProvider Instance = new();

        public string this[string key] => throw new InvalidOperationException("Not applicable");

        public bool TryGetValue(string key, out string? value)
        {
            throw new InvalidOperationException("Not applicable");
        }
    }
}
