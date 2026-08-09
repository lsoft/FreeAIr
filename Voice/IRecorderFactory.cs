using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record
{
    /// <summary>
    /// Builds one kind of <see cref="IRecorder"/> and, if it needs setup, the control that collects
    /// it. The recorder picker menu (<see cref="ChosenRecorder"/>) lists one entry per known factory,
    /// so adding a new speech-to-text backend means adding a new factory implementation.
    /// </summary>
    public interface IRecorderFactory
    {
        /// <summary>Display name shown in the recorder picker menu and stored as the user's chosen backend.</summary>
        string Name
        {
            get;
        }

        /// <summary>The settings control shown before switching to this backend, or null when it needs no configuration.</summary>
        UserControl? CreateConfigurationControl();

        /// <summary>Builds the recorder itself, using whatever the configuration control (if any) collected.</summary>
        Task<IRecorder> CreateRecorderAsync();
    }
}
