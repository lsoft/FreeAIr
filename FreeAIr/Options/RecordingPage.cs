using System.ComponentModel;

namespace FreeAIr
{
    [Browsable(true)]
    public class RecordingPage : BaseOptionModel<RecordingPage>
    {
        [Category("Recording")]
        [DisplayName("Enabled")]
        [Description("Recording enabled")]
        [DefaultValue(true)]
        public bool Enabled
        {
            get;
            set;
        } = true;

        [Category("Recording")]
        [DisplayName("Chosen recorder name")]
        [Description("Chosen recorder name")]
        [DefaultValue("")]
        public string ChosenRecorderName
        {
            get;
            set;
        } = "";

        [Category("Recording")]
        [DisplayName("Chosen post-process action")]
        [Description("Chosen post-process action for transcribed text")]
        [DefaultValue("")]
        public string ChosenPostProcessActionName
        {
            get;
            set;
        } = "";


        #region Whisper.Net

        [Category("Whisper.Net")]
        [DisplayName("Full path to whisper.net model file")]
        [Description("You can download whisper.net model from https://huggingface.co/sandrohanea/whisper.net/tree/main")]
        [DefaultValue("")]
        public string WhisperNet_ModelFilePath
        {
            get;
            set;
        } = "";

        [Category("Whisper.Net")]
        [DisplayName("Prompt for whisper.net LLM")]
        [Description("Prompt for whisper.net LLM")]
        [DefaultValue("This is the programmer's speech.")]
        public string WhisperNet_Prompt
        {
            get;
            set;
        } = "This is the programmer's speech.";

        #endregion

        #region Whisper OpenAI

        [Category("Whisper OpenAI API")]
        [DisplayName("Name of Whisper model file")]
        [Description("If you are using local LLM you can download Whisper model from https://huggingface.co/collections/openai/whisper-release-6501bba2cf999715fd953013")]
        [DefaultValue("")]
        public string WhisperOAI_ModelName
        {
            get;
            set;
        } = "";

        [Category("Whisper OpenAI API")]
        [DisplayName("Token")]
        [Description("Token for your OpenAI API. You may use ${MY_ENV_NAME} notation to hide the secret inside your MY_ENV_NAME environment variable.")]
        [DefaultValue("")]
        public string WhisperOAI_Token
        {
            get;
            set;
        } = "";

        [Category("Whisper OpenAI API")]
        [DisplayName("Endpoint")]
        [Description("Endpoint for your OpenAI API.")]
        [DefaultValue("")]
        public string WhisperOAI_Endpoint
        {
            get;
            set;
        } = "";

        [Category("Whisper OpenAI API")]
        [DisplayName("Prompt for Whisper LLM")]
        [Description("Prompt for Whisper LLM")]
        [DefaultValue("This is the programmer's speech.")]
        public string WhisperOAI_Prompt
        {
            get;
            set;
        } = "This is the programmer's speech.";

        #endregion

    }
}
