using System.ComponentModel;
using System.Threading.Tasks;

namespace FreeAIr
{
    public class ApiPage : BaseOptionModel<ApiPage>
    {
        [Category("OpenAI compatible API requisites")]
        [DisplayName("Endpoint")]
        [Description("An endpoint of LLM API provider.")]
        [DefaultValue("https://openrouter.ai/api/v1")]
        public string Endpoint { get; set; } = "https://openrouter.ai/api/v1";

        public async Task<bool> VerifyUriAndShowErrorIfNotAsync()
        {
            var endpointUri = ApiPage.Instance.TryBuildEndpointUri();
            if (endpointUri is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "Invalid endpoint Uri. Please make sure it is correct uri. For example, the uri must contain a protocol prefix, like http, https."
                    );
                return false;
            }

            return true;
        }

        public Uri? TryBuildEndpointUri()
        {
            try
            {
                return new Uri(ApiPage.Instance.Endpoint);
            }
            catch (Exception excp)
            {
                //todo log
            }

            return null;
        }


        [Category("OpenAI compatible API requisites")]
        [DisplayName("Token")]
        [Description("A token of LLM API provider.")]
        [DefaultValue("")]
        public string Token { get; set; } = "";

        [Category("OpenAI compatible API requisites")]
        [DisplayName("Chosen model")]
        [Description("Chosen model, if API provider suggests many.")]
        [DefaultValue("qwen/qwen3-32b:free")]
        public string ChosenModel { get; set; } = "qwen/qwen3-32b:free";


        [Category("LLM context size")]
        [DisplayName("Context size")]
        [Description("A LLM context size. It depends on model.")]
        [DefaultValue(16384)]
        public int ContextSize { get; set; } = 16384;

    }
}
