# FreeAIr architecture

This document describes how FreeAIr is put together. It is aimed at contributors; if you are
looking for the user manual, read the [README](README.md) instead.

## Solution layout

| Project | Target | Purpose |
| --- | --- | --- |
| `FreeAIr` | .NET Framework 4.8 (VSIX) | The extension itself: package, commands, tool windows, chats, options, MCP client side. |
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
- `NLOutline/` generates and stores natural-language outlines — LLM-written comments embedded in
  the source, following [arxiv 2408.04820](https://arxiv.org/html/2408.04820v4).
- `Embedding/` builds the embedding JSON files (`<solution name>_embeddings.json`) from those
  outlines.

Note that the `Use RAG` flag reaches `NaturalLanguageSearchParameters` but is not consumed by the
search yet — see the warning in the README.

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
