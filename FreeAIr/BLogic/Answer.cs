using System.IO;
using System.Text;

namespace FreeAIr.BLogic
{
    public sealed class Answer
    {
        private readonly StringBuilder _result = new();

        public string ResultFilePath
        {
            get;
        }

        public Answer(string resultFilePath)
        {
            ResultFilePath = resultFilePath;
        }

        public void Append(string contentPart)
        {
            _result.Append(contentPart);
            File.AppendAllText(ResultFilePath, contentPart);

        }

        public string GetAnswer()
        {
            return _result.ToString();
        }
    }
}
