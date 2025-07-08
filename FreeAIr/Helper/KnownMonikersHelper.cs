using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class KnownMonikersHelper
    {
        private static readonly Dictionary<string, PropertyInfo> _propertyDictionary;

        static KnownMonikersHelper()
        {
            _propertyDictionary = (
                from property in typeof(KnownMonikers).GetProperties(BindingFlags.Static | BindingFlags.Public)
                where property.PropertyType == typeof(ImageMoniker)
                where property.GetMethod is not null
                select property
                ).ToDictionary(p => p.Name, p => p);
        }

        public static ImageMoniker GetMoniker(
            string monikerName
            )
        {
            if (string.IsNullOrEmpty(monikerName))
            {
                return KnownMonikers.QuestionMark;
            }

            if (!_propertyDictionary.TryGetValue(monikerName, out var property))
            {
                return KnownMonikers.BlockError;
            }

            var result = (ImageMoniker)property.GetValue(null);
            return result;
        }
    }
}
