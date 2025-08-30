using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record
{
    public interface IRecorderFactory
    {
        string Name
        {
            get;
        }

        UserControl? CreateConfigurationControl();

        Task<IRecorder> CreateRecorderAsync();
    }
}
