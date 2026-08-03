# FreeAIr architecture

This document describes how FreeAIr is put together. It is aimed at contributors; if you are
looking for the user manual, read the [README](README.md) instead.

## Solution layout

| Project | Target | Purpose |
| --- | --- | --- |
| `FreeAIr` | .NET Framework 4.8 (VSIX) | The extension itself: package, commands, tool windows, chats, options, MCP client side. |
| `FreeAIr.Rag` | netstandard2.0 | The natural-language-search machinery that does not need Visual Studio: index format, vector codec, outline tree, ranking. |
| `FreeAIr.Rag.Tests` | .NET 8 (xunit) | Unit tests of `FreeAIr.Rag`. Not shipped. |
| `MCP/Proxy` | .NET 9 (exe) | Out-of-process host for MCP servers. Shipped inside the VSIX as `.art/Proxy.zip` and unpacked on first run. |
| `MCP/Dto` | netstandard | Request/reply contracts of the JSON-RPC channel between `FreeAIr` and `Proxy.exe`. |
| `CodeLens` | .NET Framework 4.8 | CodeLens data point provider. Runs in the separate Visual Studio CodeLens process. |
| `Shared` | netstandard | Types shared between the VSIX and the CodeLens process (pipe name, `UnitInfo` DTOs). |
| `MarkdownParser` | netstandard | ANTLR-based markdown parser used to render LLM answers. |
| `MarkdownParserTester` | WPF app | Scratch harness for the markdown parser; not shipped. |
| `WpfHelpers` | netstandard | View-model / command / collection helpers used by the WPF UI. |
| `TestSubject` | — | A sample solution used to try FreeAIr out manually. Not part of the product. |

Two of these projects run **outside** the Visual Studio process, and that is the main thing to
keep in mind when reading the code:

```
                    +------------------------------------------------+
                    |             devenv.exe (VS 2022/2026)          |
                    |                                                |
    OpenAI API      |   +-------------+        +-----------------+   |
  <---------------------+  LLMReader  |        | FreeAIrPackage  |   |
      (HTTPS)       |   +------+------+        +--------+--------+   |
                    |          |                        |            |
                    |   +------v------------------------v--------+   |
                    |   |   Chat / ChatContainer / FreeAIrOptions |   |
                    |   +--------------------+--------------------+   |
                    +------------------------|-----------------------+
                                             |  JSON-RPC over stdin/stdout
                                             |  (StreamJsonRpc)
                                  +----------v----------+
                                  |      Proxy.exe      | ---> external MCP servers
                                  +---------------------+

    +----------------------+        named pipe         +----------------------+
    |  ServiceHub CodeLens |<------------------------->|    FreeAIrPackage    |
    |        process       |     (CodeLensPipeName)    |                      |
    +----------------------+                           +----------------------+
```

## Runtime pieces

### Package startup

`FreeAIr/FreeAIrPackage.cs` is the entry point. It auto-loads both with and without a solution
and, during `InitializeAsync`, it:

- manually loads a couple of assemblies that VS does not resolve on its own;
- registers commands and tool windows;
- starts listening for CodeLens connections (`CodeLensConnectionHandler`);
- starts the Find-window modifier that injects the natural-language-search UI;
- resolves MEF services (`ChatContainer`, `UIInformer`, `GitWindowModifier`);
- initializes the voice recorder (`ChosenRecorder`);
- shows the release-notes info bar after an upgrade;
- kicks off `McpServerProxyApplication.UpdateExternalServersAsync()`.

`McpServerProxyApplication`'s static constructor unpacks and starts `Proxy.exe`, so simply
touching that class launches the child process.

### Chats

- `Chat/ChatContainer.cs` — MEF-exported singleton owning every live chat. Creates and removes
  chats and aggregates their status for the status-bar informer.
- `Chat/Chat.cs` — one dialogue. Holds an ordered list of `IChatContent` (prompts, answers, tool
  calls), the `ChatContext`, the chat-scoped MCP tool switches (`AvailableToolContainer`) and the
  `ChatOptions` (chosen agent, response format, tool choice).
- `Chat/Context/ChatContext.cs` — extra material handed to the LLM: solution documents, selections,
  external files. `copilot-instructions.md` is picked up automatically when present.
- `BLogic/Reader/LLMReader.cs` — the worker that actually talks to the model. It streams the
  completion, appends text to an `AnswerChatContent` as it arrives (this is what makes the UI
  update live) and materializes `ToolCallChatContent` for every tool the model asks for.
- `BLogic/Reader/LLMReaderPool.cs` — one reader per chat, keyed by the chat instance.

The loop is deliberately re-entrant: when the last outstanding tool call of a turn finishes,
`Chat.CreateToolCall`'s callback starts the reader again so the model can consume the tool results.

### Options

`Options2/FreeAIrOptions.cs` is the JSON settings root (agents, MCP servers, tools, support
actions, plus unsorted knobs). It can live in either of two places, see `OptionsPlaceEnum`:

- `<solution folder>\.freeair\<solution name>_options.json` — team-wide, meant to be committed;
- Visual Studio's own option store (`InternalPage`) — when a file is undesirable.

Reads go through `BLogic/DataPieceCache.cs`, which re-parses only when the file timestamp (or the
stored string) changes.

`Options/*Page.cs` are the per-user Visual Studio option pages (UI, font sizes, recording,
internal). These are intentionally *not* part of the JSON settings.

### MCP

- `MCP/ServerProxy/McpServerProxyApplication.cs` — owns `Proxy.exe`: unpacks it, monitors it
  (`ProcessMonitor`), attaches `StreamJsonRpc` to its stdin/stdout and exposes `IMcpProxyInterface`.
- `MCP/ServerProxy/McpServerProxyCollection.cs` — the set of currently initialized servers. It
  reconciles the configured server list against the running one, refreshes the tool catalogue and
  dispatches `CallToolAsync` to the owning server.
- `MCP/ServerProxy/VS` — the built-in Visual Studio MCP server. Its tools (build, git commit,
  nuget install, read/replace document body, solution tree, error list, web search) run **inside**
  devenv and therefore have direct access to DTE/Roslyn.
- `MCP/ServerProxy/Github` and `MCP/ServerProxy/External` — the github.com server and any
  user-configured server, both hosted by `Proxy.exe`.
- `MCP/AvailableToolContainer.cs` — enabled/disabled state of individual tools, globally and
  per chat.

### Natural language search / outlines

- `Find/FindWindowModifier.cs` injects FreeAIr's controls into the standard Find window;
  `Find/DoSearch.cs` collects the search parameters and opens the results tool window.
- `Find/SearchTrace.cs` is the log of a search, written into an output pane of its own. The search
  passes through three pickers, a tool window and a chat, and every one of them is entitled to
  decide there is nothing to do; each such decision used to be a bare `return`, which is
  indistinguishable from a broken button. Steps report themselves, and the ones which end the search
  early state the reason and repeat it into the activity log.
- `NLOutline/` generates and stores natural-language outlines — LLM-written comments embedded in
  the source, following [arxiv 2408.04820](https://arxiv.org/html/2408.04820v4).
Everything below `Embedding/` and `Find/RagShortlist.cs` lives in the **`FreeAIr.Rag`** project, not
in the VSIX. That split is what makes the feature testable: the VSIX assembly cannot be loaded by a
test runner, while `FreeAIr.Rag` knows nothing about the IDE and is covered by `FreeAIr.Rag.Tests`.

- `Embedding/Json/Objects.cs` writes and reads the index files. `Embedding/VectorCodec.cs` is the
  storage format of a vector: int8 quantization in base64, normalized on the way back in, so a
  cosine similarity is a plain dot product.
- `Embedding/OutlineEmbedder.cs` decides which nodes are worth a vector and fills them in through
  `IEmbeddingVectorizer` — the single seam where the pipeline talks to a server, implemented by
  `OpenAIEmbeddingVectorizer`.
- `Embedding/EmbeddingIndexReader.cs` reads the files with progress and full cancellation;
  `EmbeddingIndex` is the searchable view of them.
- `Find/RagShortlist.cs` turns a query into a handful of files: vectorize, rank the outlines,
  aggregate them per file by the best score, cut by threshold and count.
- `Find/RagCalibration.cs` is where that threshold comes from. A cosine is not comparable between
  models — measured on one solution, the same query scores 0.94, 0.84 and 0.70 on three models while
  their noise sits at 0.89, 0.53 and 0.53 — so no constant can be shipped. At the end of every index
  build the fresh index is asked a few questions it cannot answer, and the level they reach is
  stored in the metadata as `EmbeddingCalibration`. The user's `Sensitivity` then says how far above
  that level a file has to stand. Questions with a known answer may be added too: they clamp the
  threshold from above and turn "this model does not understand my code" into a number.
- `UI/ViewModels/RagCalibrationViewModel.cs` is the window that produces those questions. It asks
  the index through `RagShortlist.ProbeAsync` — the ordinary ranking with the threshold reported
  instead of enforced, because the rows the threshold cuts are the ones worth looking at — and the
  user labels each query with the file which answers it, or with nothing. Saving writes the queries
  into the settings and the numbers into the index through
  `EmbeddingOutlineJsonObject.SerializeMetadataAsync`, which touches the metadata file alone: the
  calibration belongs to the queries, not to the vectors, and rebuilding megabytes of identical
  vectors to store five floats would be the wrong trade. The window names the agent it asks, and
  resolves it through `DoSearch.DetermineEmbeddingAgentAsync`, the same code the search uses — what
  it measures is only meaningful when measured with the model which built the index.
- Agents for embeddings are looked up without the `has a token` filter the chat pickers apply
  (`FreeAIrOptions.DeserializeAgentByNameAsync`, `AgentContextMenu.ChooseAnyAgentAsync`). An
  embedding model is normally served by a local process which wants no token, so that filter hid
  exactly the agents this path needs and substituted a cloud chat agent, which answers a request for
  embeddings with HTTP 400.
- `Embedding/EmbeddingSpaceFingerprint.cs` stores the vectors of three fixed sentences in the index
  and compares them with the current model before a search. Neither the model name nor the vector
  length can do this job: a local server reports whatever name it likes (koboldcpp says `inactive`
  for every model it loads), and two unrelated models of the same length produce an index which
  reads perfectly and matches nothing. The sentinels travel in the same request as the query, so the
  check costs no round trip.
- `FreeAIr/Embedding/EmbeddingIndexContainer.cs` is where that machinery meets Visual Studio: a MEF
  singleton which resolves the paths from the solution, loads off the UI thread and caches the
  parsed files, keyed by their write time. Both the search and the `GetAllSolutionFiles` MCP tool go
  through it, so the megabytes are read once and the tool never pays for the vectors it does not
  need.
- `NLOutline/Tree/OutlineTreeAssembler.cs` rebuilds the node tree from the flat outline list. The
  shape used to be a fourth file; it is not stored any more, because every node already carries its
  kind and the path of its file, and a file which only repeats what another one says is one more
  thing to merge.

The three index files are linked by `Id = MD5(Kind + ":" + Target + ":" + RelativePath)`. They are
meant to be committed, which drives two decisions: nothing that changes by itself is stored in them
(no timestamps, no absolute paths, no counters — the file system knows all of it), and everything
in them is ordered by content rather than by traversal, so that a rebuild produces the same bytes.

The `Use RAG` flag travels from `FindWindowModifier` through `NaturalLanguageSearchParameters` into
`NaturalLanguageResultsViewModel`, which replaces the scope of the search with the shortlist. The
agent that vectorizes the query is resolved in `DoSearch` — from the index metadata when it names
one, from the user otherwise — because a wrong model gives no matches at all and the question has
to be asked before the search starts, not in the middle of it.

### Voice input

`Record/` implements dictation. `ChosenRecorder` is the façade: it picks a recorder factory by the
name stored in the recording option page and swaps implementations at runtime. Four backends ship:
Microsoft Speech API, WinRT, Whisper.Net (local, Vulkan/CPU) and Whisper over the OpenAI API.
`BLogic/RecorderTranscriberPostProcessor.cs` drives record → transcribe → optional LLM
post-processing, where post-processing is an ordinary support action with the
`RecordPostProcess` scope.

### Support actions

Support actions are not hard-coded. They live in the `Supports/Actions` node of the JSON settings
and are matched to a user gesture by their `SupportScopeEnum` values. `Options2/Support/Context.cs`
substitutes the anchors (document names, build error text, git diff, search query, …) into the
prompt template before it is sent.

### UI

`UI/` is WPF, MVVM-ish, with view models under `UI/ViewModels`. Notable pieces:

- `UI/Chat` — the chat control used both by the chat tool window and by the in-situ window.
- `UI/Windows/InSituChatWindow` — the small floating chat opened at the caret. It dims when it
  loses focus and can close itself depending on the UI option page.
- `UI/Embedillo` — the prompt editor with `/` (support action) and `#` (document) completion.
- `UI/ClickableText` — renders parsed markdown blocks with the per-block action buttons.
- `ViewElementFactory.cs` — converts `CodeLensUnitInfo` coming from the CodeLens process into a
  WPF control.

## Building

Open `FreeAIr.sln` in Visual Studio 2022 (17.14+) or 2026 and build. Two post-build steps matter:

- `MCP/Proxy` zips its output into `.art/Proxy.zip`;
- the Whisper.Net runtime is zipped into `.art/WhisperNet.Runtime.zip`.

Both archives are embedded into the VSIX and unpacked into the extension folder at run time, so a
stale `.art` archive means you are debugging an old proxy. Debugging launches
`devenv.exe /rootsuffix Exp`.
