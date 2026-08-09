using System.ComponentModel;

namespace FreeAIr
{
    /// <summary>
    /// Options page for voice recording and speech-to-text transcription: which recorder and
    /// post-process action are active, and the connection settings for the whisper.net local model
    /// and the Whisper OpenAI API backends used to transcribe recordings.
    /// </summary>
    [Browsable(true)]
    public class RecordingPage : BaseOptionModel<RecordingPage>
    {
        /// <summary>
        /// Whether voice recording is enabled in the extension.
        /// </summary>
        [Category("Recording")]
        [DisplayName("Enabled")]
        [Description("Recording enabled")]
        [DefaultValue(true)]
        public bool Enabled
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Name of the currently selected audio recorder implementation.
        /// </summary>
        [Category("Recording")]
        [DisplayName("Chosen recorder name")]
        [Description("Chosen recorder name")]
        [DefaultValue("")]
        public string ChosenRecorderName
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Name of the currently selected post-process action applied to transcribed text,
        /// used by <see cref="RecorderTranscriberPostProcessor"/>.
        /// </summary>
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

        /// <summary>
        /// Full filesystem path to the local whisper.net model file used for offline transcription.
        /// </summary>
        [Category("Whisper.Net")]
        [DisplayName("Full path to whisper.net model file")]
        [Description("You can download whisper.net model from https://huggingface.co/sandrohanea/whisper.net/tree/main")]
        [DefaultValue("")]
        public string WhisperNet_ModelFilePath
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Prompt text passed to the whisper.net model to bias transcription toward the expected
        /// speech style (e.g. programmer's speech with technical terms).
        /// </summary>
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

        /// <summary>
        /// Name of the Whisper model to request from the OpenAI-compatible transcription API.
        /// </summary>
        [Category("Whisper OpenAI API")]
        [DisplayName("Name of Whisper model file")]
        [Description("If you are using local LLM you can download Whisper model from https://huggingface.co/collections/openai/whisper-release-6501bba2cf999715fd953013")]
        [DefaultValue("")]
        public string WhisperOAI_ModelName
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Authentication token for the Whisper OpenAI API. Supports the <c>${MY_ENV_NAME}</c>
        /// notation to read the secret from an environment variable instead of storing it in plain text.
        /// </summary>
        [Category("Whisper OpenAI API")]
        [DisplayName("Token")]
        [Description("Token for your OpenAI API. You may use ${MY_ENV_NAME} notation to hide the secret inside your MY_ENV_NAME environment variable.")]
        [DefaultValue("")]
        public string WhisperOAI_Token
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Endpoint URL of the OpenAI-compatible Whisper transcription API.
        /// </summary>
        [Category("Whisper OpenAI API")]
        [DisplayName("Endpoint")]
        [Description("Endpoint for your OpenAI API.")]
        [DefaultValue("")]
        public string WhisperOAI_Endpoint
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Prompt text passed to the Whisper OpenAI API to bias transcription toward the expected
        /// speech style (e.g. programmer's speech with technical terms).
        /// </summary>
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
