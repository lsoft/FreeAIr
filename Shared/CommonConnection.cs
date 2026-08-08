using System;

namespace FreeAIr.Shared
{
    /// <summary>
    /// Taken from  https://github.com/bert2/microscope completely.
    /// Take a look to that repo, it's amazing!
    /// </summary>
    public static class CodeLensPipeName
    {
        /// <summary>Named-pipe name scoped by PID so multiple VS instances don't compete for connecting CodeLenses.</summary>
        public static string Get(int pid) => $@"FreeAIrVisualStudioCodeLens\{pid}";
    }

    /// <summary>Remote interface exposed by the CodeLens data-point process, letting the package tell it to re-query <see cref="ICodeLensListener"/> after the indicator's underlying data changes.</summary>
    public interface IRemoteCodeLens
    {
        /// <summary>Asks the CodeLens data point to re-query its data and redraw.</summary>
        void Refresh();
    }

    /// <summary>Remote interface exposed by the Visual Studio side, letting a CodeLens data point register itself so the package can later call back into it via <see cref="IRemoteCodeLens"/>.</summary>
    public interface IRemoteVisualStudioCodeLens
    {
        /// <summary>Registers a connecting CodeLens data point under <paramref name="id"/> so it can later be targeted for a <see cref="IRemoteCodeLens.Refresh"/>.</summary>
        void RegisterCodeLensDataPoint(Guid id);
    }
}
