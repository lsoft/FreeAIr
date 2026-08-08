using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Looks up Visual Studio's built-in <see cref="KnownMonikers"/> image glyphs by name, used to
    /// let the user pick an icon (e.g. for a chat or context item) by its known-moniker name rather
    /// than a hard-coded resource.
    /// </summary>
    public static class KnownMonikersHelper
    {
        /// <summary>
        /// Maps every public static <see cref="ImageMoniker"/> property name on
        /// <see cref="KnownMonikers"/> to its resolved moniker value, built once via reflection.
        /// </summary>
        private static readonly Dictionary<string, ImageMoniker> _propertyByName;
        //private static readonly Dictionary<ImageMoniker, string> _propertyByMoniker;

        /// <summary>
        /// Reflects over <see cref="KnownMonikers"/> once at type load to build the name-to-moniker lookup table.
        /// </summary>
        static KnownMonikersHelper()
        {
            var propertyList =
                from property in typeof(KnownMonikers).GetProperties(BindingFlags.Static | BindingFlags.Public)
                where property.PropertyType == typeof(ImageMoniker)
                where property.GetMethod is not null
                select property;

            _propertyByName = propertyList.ToDictionary(p => p.Name, p => GetMoniker(p));
            //_propertyByMoniker = propertyList.ToDictionary(p => GetMoniker(p), p => p.Name);
        }

        /// <summary>
        /// Returns the names of every known Visual Studio image moniker, e.g. for populating an
        /// icon-picker dropdown.
        /// </summary>
        public static List<string> GetAllMonikerNames()
        {
            return _propertyByName
                .Keys
                .ToList()
                ;
        }

        /// <summary>
        /// Returns every known Visual Studio <see cref="ImageMoniker"/> value.
        /// </summary>
        public static List<ImageMoniker> GetAllMonikers()
        {
            return _propertyByName
                .Values
                .ToList()
                ;
        }

        //public static string GetMonikerName(
        //    ImageMoniker moniker
        //    )
        //{
        //    if (!_propertyByMoniker.TryGetValue(moniker, out var monikerName))
        //    {
        //        return nameof(KnownMonikers.BlockError);
        //    }

        //    return monikerName;
        //}

        /// <summary>
        /// Resolves a known-moniker name (e.g. as stored in options or config) to its
        /// <see cref="ImageMoniker"/>, falling back to a question-mark icon for an empty name or a
        /// block-error icon for an unrecognized one.
        /// </summary>
        public static ImageMoniker GetMoniker(
            string monikerName
            )
        {
            if (string.IsNullOrEmpty(monikerName))
            {
                return KnownMonikers.QuestionMark;
            }

            if (!_propertyByName.TryGetValue(monikerName, out var moniker))
            {
                return KnownMonikers.BlockError;
            }

            return moniker;
        }

        /// <summary>
        /// Reads the <see cref="ImageMoniker"/> value from a static <see cref="KnownMonikers"/> property.
        /// </summary>
        private static ImageMoniker GetMoniker(PropertyInfo p)
        {
            return (ImageMoniker)p.GetValue(null);
        }

    }
}
