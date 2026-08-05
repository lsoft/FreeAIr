using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Loads a XAML resource dictionary embedded in the extension assembly and merges it into
    /// <see cref="Application.Resources"/>, used to register styles and templates that ship inside
    /// the VSIX rather than as loose files.
    /// </summary>
    public static class EmbeddedResourceHelper
    {
        /// <summary>
        /// Reads the named embedded XAML resource dictionary and merges every entry into the current
        /// WPF application's resources.
        /// </summary>
        public static void LoadXamlEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Ресурс {resourceName} не найден.");
                }

                var resourceDict = (ResourceDictionary)XamlReader.Load(stream);

                foreach (DictionaryEntry entry in resourceDict)
                {
                    Application.Current.Resources[entry.Key] = entry.Value;
                }
            }
        }
    }
}
