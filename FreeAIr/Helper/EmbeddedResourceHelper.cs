using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
