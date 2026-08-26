using FreeAIr.Helper;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.Chat.Persistence
{
    /// <summary>
    /// Reads and writes user chats as json under `.freeair\chats`.
    ///
    /// The folder sits next to the solution, same as the options and the embedding index. If no
    /// solution is open there is nowhere to put a file, and the chat stays in memory only.
    /// Non-ASCII is written as itself — a Russian prompt stays readable in the file instead of
    /// turning into `\uXXXX` escapes.
    /// </summary>
    public static class ChatPersistence
    {
        /// <summary>Directory name under `.freeair` that holds the chat json files.</summary>
        public const string ChatsFolderName = "chats";

        /// <summary>Same encoder the settings file uses, so Cyrillic (and anything else) stays as letters.</summary>
        private static readonly JsonSerializerOptions _jsonOptions;

        static ChatPersistence()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReadCommentHandling = JsonCommentHandling.Skip,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// `{solution folder}\.freeair\chats`, or null when no solution is open (or its path cannot
        /// be read). The directory is not created here — that happens on the first save.
        /// </summary>
        public static async Task<string?> TryGetChatsFolderPathAsync()
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                return null;
            }

            var solutionPath = solution.FullPath;
            if (string.IsNullOrEmpty(solutionPath))
            {
                solutionPath = solution.Name;
            }

            if (string.IsNullOrEmpty(solutionPath))
            {
                return null;
            }

            string? directory;
            try
            {
                directory = Path.GetDirectoryName(Path.GetFullPath(solutionPath));
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
                return null;
            }

            if (string.IsNullOrEmpty(directory))
            {
                return null;
            }

            return Path.Combine(directory, ".freeair", ChatsFolderName);
        }

        /// <summary>Absolute path of the json file for a chat id inside <paramref name="folderPath"/>.</summary>
        public static string GetChatFilePath(string folderPath, Guid chatId)
        {
            if (folderPath is null)
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            return Path.Combine(folderPath, chatId.ToString("D") + ".json");
        }

        /// <summary>
        /// Writes <paramref name="payload"/> over <paramref name="filePath"/> atomically: a temp
        /// file next to it first, then a replace, so a crash mid-write does not leave a half json.
        /// </summary>
        public static void Save(string filePath, PersistedChatJson payload)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var folder = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(folder))
            {
                throw new InvalidOperationException("Chat persistence file has no directory.");
            }

            Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }

        /// <summary>Reads one chat file, or null when the json cannot be parsed.</summary>
        public static PersistedChatJson? TryLoad(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            try
            {
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return JsonSerializer.Deserialize<PersistedChatJson>(json, _jsonOptions);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
                return null;
            }
        }

        /// <summary>Every `*.json` in the chats folder, or an empty list when the folder is missing.</summary>
        public static IReadOnlyList<string> ListChatFiles(string folderPath)
        {
            if (folderPath is null)
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            if (!Directory.Exists(folderPath))
            {
                return [];
            }

            return Directory.GetFiles(folderPath, "*.json");
        }

        /// <summary>Deletes the chat file if it is there. Missing files are not an error.</summary>
        public static void Delete(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }
    }
}
