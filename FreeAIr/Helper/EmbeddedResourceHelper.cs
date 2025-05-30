using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace FreeAIr.Helper
{
    public static class EmbeddedResourceHelper
    {
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
