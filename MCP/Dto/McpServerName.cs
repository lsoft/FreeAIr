using System.Globalization;
using System.Linq;

namespace Dto
{
    /// <summary>
    /// What an MCP server may be called, and how to invent a name which qualifies.
    ///
    /// The name is not a label. It becomes the prefix of every tool the server publishes -
    /// `Server.Tool` is the function name offered to the model - and providers validate that name,
    /// with a space the one character certain to be refused. So a server called after a local date
    /// and time has every one of its tools rejected, and the complaint which comes back names a
    /// function rather than the server which produced it. That is issue #74, and it starts with the
    /// name the `add server` button fills in, which is what <see cref="CreateDefault"/> is for.
    ///
    /// The rule is deliberately the narrow one - not empty, no whitespace - rather than the
    /// `[A-Za-z0-9_-]` the providers document, because the servers which ship already use a dot and
    /// a user's configuration which works today may not be refused by an upgrade.
    /// </summary>
    public static class McpServerName
    {
        /// <summary>What the `add server` button names a server, before the date and the counter.</summary>
        public const string DefaultPrefix = "McpServer";

        /// <summary>Whether this name can be used as the prefix of a tool name.</summary>
        public static bool IsValid(
            string? name
            )
        {
            return DescribeProblem(name) is null;
        }

        /// <summary>
        /// The sentence to show the user about this name, or null when there is nothing wrong with
        /// it. One place rather than a condition repeated in the dialog, in the tool list and in
        /// the exception, so that all three say the same thing.
        /// </summary>
        public static string? DescribeProblem(
            string? name
            )
        {
            if (string.IsNullOrEmpty(name))
            {
                return "An MCP server name cannot be empty: it is the prefix of every tool the server publishes.";
            }

            if (name!.Any(char.IsWhiteSpace))
            {
                return $"The MCP server name '{name}' contains whitespace, which some LLM providers refuse in a function name. Use '_' or '-' instead.";
            }

            return null;
        }

        /// <summary>
        /// A valid name for a newly added server: the prefix, the moment it was added, and a
        /// counter if that is taken already.
        ///
        /// The date is written in the invariant format on purpose. Taking the local one is how the
        /// name came to hold spaces in the first place, and depending on the machine it also holds
        /// slashes, dots or an am/pm suffix.
        /// </summary>
        public static string CreateDefault(
            DateTime moment,
            IEnumerable<string>? existingNames = null
            )
        {
            var stem = DefaultPrefix + "_" + moment.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            var taken = existingNames is null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

            if (!taken.Contains(stem))
            {
                return stem;
            }

            //two servers added inside the same second, or one added, renamed and added again
            for (var attempt = 2; ; attempt++)
            {
                var candidate = stem + "_" + attempt.ToString(CultureInfo.InvariantCulture);
                if (!taken.Contains(candidate))
                {
                    return candidate;
                }
            }
        }
    }
}
