using FreeAIr.Dto.OpenRouter;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static FreeAIr.UI.ViewModels.ChooseModelViewModel;

namespace FreeAIr
{
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
        [DefaultValue("")]
        public string Token { get; set; } = "";

        [Category("OpenAI compatible API requisites")]
        [DisplayName("Chosen model")]
        [Description("A token of LLM API provider.")]
        [DefaultValue("qwen/qwen3-32b:free")]
        public string ChosenModel { get; set; } = "qwen/qwen3-32b:free";


        //[Category("OpenAI compatible API requisites")]
        //[DisplayName("ChosenModel1")]
        //[Description("A token of LLM API provider.")]
        //[TypeConverter(typeof(ModelConverter))]
        ////[DefaultValue(new ChosenModelType("qwen/qwen3-32b:free"))]
        //public AvailableModel ChosenModel1 { get; set; } = new AvailableModel("qwen/qwen3-32b:free");


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
