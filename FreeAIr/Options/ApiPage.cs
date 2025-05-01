using FreeAIr.Dto.OpenRouter;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static FreeAIr.UI.ViewModels.ChooseModelViewModel;

namespace FreeAIr
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class ApiPageOptions : BaseOptionPage<ApiPage>
        {
        }

        [ComVisible(true)]
        public class ResponsePageOptions : BaseOptionPage<ResponsePage>
        {
        }
    }

    public class ResponsePage : BaseOptionModel<ResponsePage>
    {
        private XshdProvider[] _xshdProviders =
            [
                new XshdProvider
                {
                    LanguageAlias = "cs,csharp",
                    XshdFilePath = @"Resources/xshd/csharp.xshd"
                }
            ];

        [Category("Response")]
        [DisplayName("Type")]
        [Description("A type of response of LLM API provider.")]
        [TypeConverter(typeof(EnumConverter))]
        [DefaultValue(LLMResultEnum.MD)]
        public LLMResultEnum ResponseFormat  { get; set; } = LLMResultEnum.MD;

        [Category("Response")]
        [DisplayName("Switch to task window")]
        [Description("Should FreeAIr switch to its window after dev asked a prompt.")]
        [DefaultValue(true)]
        public bool SwitchToTaskWindow { get; set; } = true;

        [Category("Response")]
        [DisplayName("Code colorization schemas")]
        [Description("Edit this list to provide custom colorization schemas for your programming languages. Xshd file path can be local relative of FreeAIr.dll, or absolute. RESTART OF VISUAL STUDIO IS REQUIRED TO APPLY THIS OPTION.")]
        [DefaultValue(true)]
        public XshdProvider[] XshdProviders
        {
            get => _xshdProviders;
            set
            {
                _xshdProviders = value;

                LoadOrUpdateMarkdownStyles();
            }
        }

        public static void LoadOrUpdateMarkdownStyles()
        {
            System.Windows.Application.Current.Resources.Remove("MdXamlPlugins");

            var plugins = new MdXaml.Plugins.MdXamlPlugins();
            foreach (var xshdProvider in ResponsePage.Instance.XshdProviders)
            {
                var fullPath = System.IO.Path.IsPathRooted(xshdProvider.XshdFilePath)
                    ? xshdProvider.XshdFilePath
                    : System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(
                            FreeAIrPackage.WorkingFolder,
                            xshdProvider.XshdFilePath
                            )
                        )
                    ;

                plugins.Highlights.Add(
                    new MdXaml.Plugins.Definition
                    {
                        Alias = xshdProvider.LanguageAlias,
                        Resource = new Uri("file:///" + fullPath)
                    }
                );
            }

            System.Windows.Application.Current.Resources.Add("MdXamlPlugins", plugins);
        }

    }

    public class ApiPage : BaseOptionModel<ApiPage>
    {
        [Category("OpenAI compatible API requisites")]
        [DisplayName("Endpoint")]
        [Description("An endpoint of LLM API provider.")]
        [DefaultValue("https://openrouter.ai/api/v1")]
        public string Endpoint { get; set; } = "https://openrouter.ai/api/v1";

        [Category("OpenAI compatible API requisites")]
        [DisplayName("Token")]
        [Description("A token of LLM API provider.")]
        [DefaultValue("place your token here")]
        public string Token { get; set; } = "place your token here";

        [Category("OpenAI compatible API requisites")]
        [DisplayName("Chosen model")]
        [Description("A token of LLM API provider.")]
        [DefaultValue("qwen/qwen3-14b:free")]
        public string ChosenModel { get; set; } = "qwen/qwen3-14b:free";


        //[Category("OpenAI compatible API requisites")]
        //[DisplayName("ChosenModel1")]
        //[Description("A token of LLM API provider.")]
        //[TypeConverter(typeof(ModelConverter))]
        ////[DefaultValue(new ChosenModelType("qwen/qwen3-14b:free"))]
        //public AvailableModel ChosenModel1 { get; set; } = new AvailableModel("qwen/qwen3-14b:free");


        //protected override string SerializeValue(object value, Type type, string propertyName)
        //{
        //    if (propertyName == nameof(ChosenModel1))
        //    {
        //        return (value as AvailableModel)?.ModelId ?? "";
        //    }

        //    return base.SerializeValue(value, type, propertyName);
        //}

        //protected override object DeserializeValue(string serializedData, Type type, string propertyName)
        //{
        //    if (propertyName == nameof(ChosenModel1))
        //    {
        //        return new AvailableModel(serializedData);
        //    }

        //    return base.DeserializeValue(serializedData, type, propertyName);
        //}

        //public sealed class ModelConverter : TypeConverter
        //{
        //    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        //    {
        //        return sourceType == typeof(string);
        //    }

        //    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        //    {
        //        return new AvailableModel(value as string ?? "");
        //    }

        //    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        //    {
        //        return destinationType == typeof(string);
        //    }

        //    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        //    {
        //        if (destinationType == typeof(string))
        //        {
        //            if (value is AvailableModel proxy)
        //            {
        //                return proxy.ModelId;
        //            }
        //            else if (value is string s)
        //            {
        //                // This is just in case the value is still a
        //                // string and wasn't converted from a string.
        //                return s;
        //            }
        //        }

        //        throw new InvalidOperationException("Unknown error.");
        //    }

        //    public override bool IsValid(ITypeDescriptorContext context, object value)
        //    {
        //        AvailableModel proxy = value as AvailableModel;
        //        if (proxy is not null)
        //        {
        //            return GetStandardValues().Cast<string>().Contains(proxy.ModelId);
        //        }
        //        return false;
        //    }

        //    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        //    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        //    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        //    {
        //        var _loadFreeModels = false;
        //        var _httpClient = new HttpClient();
        //        var modelContainer =  _httpClient.GetFromJsonAsync<ModelResponse>(
        //            "https://openrouter.ai/api/v1/models"
        //            ).Result;
        //        var models = modelContainer.Models
        //            .Where(m => !_loadFreeModels || (m.name.Contains("(free)")))
        //            .Where(m => !_loadFreeModels || (m.pricing is null || m.pricing.IsFree))
        //            .ToList()
        //            ;
        //        return new StandardValuesCollection(
        //            models.ConvertAll(m => new AvailableModel(m.id))
        //            );

        //        //// Load the values from somewhere (like a database). You will want to perform some
        //        //// sort of caching, and ideally pre-fetch the values, because any async activity
        //        //// here (run via `ThreadHelper`, of course) will block the UI.
        //        //return new StandardValuesCollection(
        //        //    new[] {
        //        //    new ChosenModelType("Alpha"),
        //        //    new ChosenModelType("Beta"),
        //        //    new ChosenModelType("Gamma"),
        //        //    new ChosenModelType("Delta")
        //        //    }
        //        //);
        //    }

        //    public override bool GetPropertiesSupported(ITypeDescriptorContext context) => false;

        //    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) => throw new NotSupportedException();

        //    public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) => false;

        //    public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues) => throw new NotSupportedException();
        //}

        //public class AvailableModel
        //{
        //    public string ModelId
        //    {
        //        get;
        //    }

        //    public AvailableModel(string modelId)
        //    {
        //        ModelId = modelId;
        //    }

        //    public override string ToString()
        //    {
        //        return ModelId;
        //    }
        //}
    }

    public sealed class XshdProvider
    {
        public string LanguageAlias
        {
            get;
            set;
        }

        public string XshdFilePath
        {
            get;
            set;
        }
    }

    public enum LLMResultEnum
    {
        MD,
        PlainText
    }
}
