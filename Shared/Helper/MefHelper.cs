using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Reaches the MEF container of the running Visual Studio, which is where every exported
    /// service of the extension is resolved from.
    ///
    /// Everything used to ask <c>FreeAIrPackage.Instance</c> for it, which tied the natural
    /// language outlines, the git commands and the MCP tools to the package class for no reason
    /// beyond that being where somebody first wrote the line. The global async service provider
    /// hands out the same SComponentModel and belongs to no assembly in particular.
    /// </summary>
    public static class MefHelper
    {
        /// <summary>
        /// The container itself, for a caller that resolves more than one service at a time.
        /// </summary>
        public static async Task<IComponentModel> GetComponentModelAsync(
            )
        {
            //there is no running Visual Studio without a MEF container, so a null here is not a
            //case to handle but the shell being gone from under us
            return (IComponentModel)(await AsyncServiceProvider.GlobalProvider.GetServiceAsync(
                typeof(SComponentModel)
                ))!;
        }

        /// <summary>
        /// Resolves a single exported service.
        /// </summary>
        public static async Task<T> GetServiceAsync<T>(
            )
            where T : class
        {
            var componentModel = await GetComponentModelAsync();

            return componentModel.GetService<T>();
        }
    }
}
