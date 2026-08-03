using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FreeAIr.Options2.Rag
{
    /// <summary>
    /// Settings of the `Use RAG` natural language search.
    ///
    /// There is deliberately no cosine threshold here. A cosine means something different on every
    /// embedding model — the same query on the same solution scores 0.94 on one model and 0.70 on
    /// another, while the noise of both sits just below — so a number written into a settings file
    /// is right for exactly one model and silently wrong for the next one. Instead the index
    /// measures its own scale when it is built (see the `Calibration` node below) and
    /// <see cref="Sensitivity"/> says how far above that measured noise a file has to stand.
    /// </summary>
    [JsonConverter(typeof(JsonDescriptionCommentConverter<RagJson>))]
    public sealed class RagJson : ICloneable
    {
        /// <summary>How many natural language outlines are taken from the embedding index before they are aggregated into files.</summary>
        [Description("'Use RAG' search: how many natural language outlines are taken from the embedding index before they are aggregated into files. Raise it if the search misses files which contain many outlines each.")]
        public int TopOutlineCount
        {
            get;
            set;
        } = 50;

        /// <summary>The maximum count of files the shortlist is allowed to pass to the LLM; kept low to keep the search cheap.</summary>
        [Description("'Use RAG' search: the maximum count of files the shortlist is allowed to pass to the LLM. This is what makes the search cheap, so keep it low.")]
        public int MaxFileCount
        {
            get;
            set;
        } = 15;

        /// <summary>How far above the measured noise of the index a file has to stand to be taken — see the class summary for why this is not a cosine threshold.</summary>
        [Description("'Use RAG' search: how far above the noise of your index a file has to stand to be taken. 0.1 is generous, 0.2 is the default, 0.35 is strict. This is NOT a cosine similarity: it is a share of the room between the noise measured at index build time and a perfect match, which is what makes one value work on different embedding models. Has no effect until the index has been calibrated.")]
        public double Sensitivity
        {
            get;
            set;
        } = 0.2d;

        /// <summary>The probe queries the index is calibrated with at the end of every build — see <see cref="RagCalibrationJson"/>.</summary>
        [Description("'Use RAG' search: the questions the index is calibrated with, at the end of every index build. Write queries your solution CANNOT answer into 'Irrelevant' - the closer they are to being plausible for this codebase, the more honest the threshold. Put queries whose answer you know into 'Relevant' together with the file which is supposed to win; the threshold is then never allowed to climb up to them, and a query which does not find its file is reported as a miss, meaning this model does not understand this codebase.")]
        public RagCalibrationJson Calibration
        {
            get;
            set;
        } = new();

        public RagJson()
        {
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new RagJson
            {
                TopOutlineCount = TopOutlineCount,
                MaxFileCount = MaxFileCount,
                Sensitivity = Sensitivity,
                Calibration = (RagCalibrationJson)Calibration.Clone(),
            };
        }
    }

    /// <summary>
    /// The probe queries a calibration run scores against the index, so the noise floor
    /// <see cref="RagJson.Sensitivity"/> is measured against reflects this solution rather than a
    /// number picked in the abstract.
    /// </summary>
    public sealed class RagCalibrationJson : ICloneable
    {
        /// <summary>
        /// Queries with a known answer. Empty by default: nobody but the author of the solution can
        /// write one, and a wrong one would teach the search to accept a wrong file.
        /// </summary>
        public List<RagRelevantProbeJson> Relevant
        {
            get;
            set;
        } = new();

        /// <summary>
        /// Queries this solution cannot answer. Empty means the built-in set, which is deliberately
        /// far-fetched — good enough to work out of the box, weaker than anything the user writes.
        /// </summary>
        public List<string> Irrelevant
        {
            get;
            set;
        } = new();

        /// <inheritdoc/>
        public object Clone()
        {
            return new RagCalibrationJson
            {
                Relevant = Relevant.ConvertAll(p => (RagRelevantProbeJson)p.Clone()),
                Irrelevant = new List<string>(Irrelevant),
            };
        }
    }

    /// <summary>One calibration probe: a query paired with the file it must retrieve.</summary>
    public sealed class RagRelevantProbeJson : ICloneable
    {
        /// <summary>The natural language query, as if typed into the 'Use RAG' search box.</summary>
        public string Query
        {
            get;
            set;
        } = string.Empty;

        /// <summary>The file which has to come out on top, relative to the solution folder.</summary>
        public string ExpectedPath
        {
            get;
            set;
        } = string.Empty;

        /// <inheritdoc/>
        public object Clone()
        {
            return new RagRelevantProbeJson
            {
                Query = Query,
                ExpectedPath = ExpectedPath,
            };
        }
    }
}
