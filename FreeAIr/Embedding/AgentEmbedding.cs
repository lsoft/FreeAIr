using FreeAIr.Find;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Rag;
using System.Collections.Generic;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// Binds the index pipeline of FreeAIr.Search to the settings of the extension. Everything in
    /// here is a translation from a FreeAIr type into a plain one, and nothing else: the pipeline
    /// deliberately does not know about agents or about the settings file.
    /// </summary>
    public static class AgentEmbedding
    {
        /// <summary>
        /// The agent must point at an embedding model, not at a chat one.
        /// </summary>
        public static IEmbeddingVectorizer CreateVectorizer(
            AgentJson agent
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            return new OpenAIEmbeddingVectorizer(
                agent.Technical.ChosenModel,
                agent.Technical.Endpoint,
                agent.Technical.GetToken()
                );
        }

        /// <summary>Translates the RAG settings' shortlist knobs (top outline count, max files, sensitivity) into the pipeline's own options type.</summary>
        public static RagShortlistOptions CreateShortlistOptions(
            RagJson rag
            )
        {
            if (rag is null)
            {
                throw new ArgumentNullException(nameof(rag));
            }

            return new RagShortlistOptions
            {
                TopOutlineCount = rag.TopOutlineCount,
                MaxFileCount = rag.MaxFileCount,
                Sensitivity = rag.Sensitivity
            };
        }

        /// <summary>
        /// The questions the index is calibrated with. An empty settings node means the built-in
        /// ones, so that an index built by a user who has never opened the settings still gets a
        /// threshold.
        /// </summary>
        public static RagCalibrationProbes CreateCalibrationProbes(
            RagJson rag
            )
        {
            if (rag is null)
            {
                throw new ArgumentNullException(nameof(rag));
            }

            var relevant = new List<RagRelevantProbe>();

            foreach (var probe in rag.Calibration.Relevant)
            {
                if (string.IsNullOrWhiteSpace(probe.Query) || string.IsNullOrWhiteSpace(probe.ExpectedPath))
                {
                    continue;
                }

                relevant.Add(
                    new RagRelevantProbe(
                        probe.Query.Trim(),
                        probe.ExpectedPath.Trim()
                        )
                    );
            }

            var irrelevant = new List<string>();

            foreach (var probe in rag.Calibration.Irrelevant)
            {
                if (string.IsNullOrWhiteSpace(probe))
                {
                    continue;
                }

                irrelevant.Add(probe.Trim());
            }

            return new RagCalibrationProbes(
                relevant,
                irrelevant
                );
        }
    }
}
