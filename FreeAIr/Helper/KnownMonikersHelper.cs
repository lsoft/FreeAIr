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
        private static readonly Dictionary<string, ImageMoniker> _propertyByName;
        //private static readonly Dictionary<ImageMoniker, string> _propertyByMoniker;

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

        public static List<string> GetAllMonikerNames()
        {
            return _propertyByName
                .Keys
                .ToList()
                ;
        }

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

        private static ImageMoniker GetMoniker(PropertyInfo p)
        {
            return (ImageMoniker)p.GetValue(null);
        }

    }
}
