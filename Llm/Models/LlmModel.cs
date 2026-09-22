namespace FreeAIr.Llm.Models
{
    /// <summary>
    /// One model an endpoint offers, as the model picker and the endpoint reachability check see
    /// it. Two fields, because two is all the pickers filter and display by - the providers
    /// disagree about everything else they report.
    /// </summary>
    public sealed class LlmModel
    {
        /// <summary>The id to put in the agent's `ChosenModel`, and what the picker shows.</summary>
        public string Id
        {
            get;
        }

        /// <summary>
        /// Who publishes the model, when the provider says. OpenRouter and the gateways use it to
        /// group hundreds of models by vendor, which is what makes the picker's mask worth having;
        /// a local server usually leaves it empty.
        /// </summary>
        public string? OwnedBy
        {
            get;
        }

        public LlmModel(
            string id,
            string? ownedBy = null
            )
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException($"'{nameof(id)}' cannot be null or empty.", nameof(id));
            }

            Id = id;
            OwnedBy = ownedBy;
        }
    }
}
