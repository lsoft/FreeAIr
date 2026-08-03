# Overview

Please report any bugs to the [github repo](https://github.com/lsoft/FreeAIr).

## Feedback

Visual Studio extension authors suffers of lack of feedback. Please share your feelings and gratitude. Choose one or few available options:

1. Please [gift a ★★★★★ rating](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr) for this VSIX in the VS Marketplace.
2. Gift a ★ to the [github repo](https://github.com/lsoft/FreeAIr).
3. If you are enjoying FreeAIr to the enough level to donate, there are many [small cancer patients](https://advitausa.org/au/index.php/donate/) that need your help. Please provide your help them!

## Other my VSIXes may interest you

- [Fix incorrect namespaces](https://marketplace.visualstudio.com/items?itemName=lsoft.AdjustNamespaceVisualStudioExtension2022) for a single file, folder, project or a whole solution and rules the resulting regressions in the code (including XAML), e.g. fixes the broken references. This extension works like Resharper `Adjust namespaces` function.
- [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=lsoft.MultiLineDebugExpressionEvaluatorInternalName) for quick watch window which allows to debug and edit multilines expressions.
- If you are using plain SQL inside you code base you may want to validate these queries against your DB schema right inside Visual Studio. [ReSequel](https://marketplace.visualstudio.com/items?itemName=lsoft.ReSequel64) does exactly that.
- [This extension](https://marketplace.visualstudio.com/items?itemName=lsoft.RelationalRoslynVisualStudioExtension) puts Roslyn metadata of your project into the in-memory sqlite database and allows to you to execute queries to the database.
- [The faster way](https://marketplace.visualstudio.com/items?itemName=lsoft.StringLocalizer) to add strings to your multilanguage resx files. Just install the extension, select the text and press Alt+J.
- A [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=lsoft.SyncToAsyncExtension) which creates codelenses allows you to go to sync sibling method for async methods and vice-versa even if sibling method is in different file or code generated.

My others extensions lives [here](https://marketplace.visualstudio.com/publishers/lsoft).

# FreeAIr Release Notes

## 4.3.0

- Fixed bug, thanks to nrmncr for [reporting it](https://github.com/lsoft/FreeAIr/issues/61).
- The `Use RAG` checkbox of the natural language search is implemented. The search now takes the
  files whose outlines are the closest to the query and asks the LLM about those only, instead of
  reading through the whole solution.
- The embedding json files have a new, much smaller format: vectors are quantized and stored one per
  line, and outlines which are nothing but the name of the member they describe are not embedded at
  all. On the test solution that is 18 times less disk space. The files are also written in an order
  which does not depend on the machine which built them, so two people regenerating them in two
  branches no longer get a git conflict out of it.
- There are three index files instead of four: the tree of the outlines is not stored any more, it
  is derived from the outlines themselves. One file less to merge.
- The natural language search can now be cancelled at every step, including while the index is being
  read.
- The GetAllSolutionFiles MCP tool no longer reads the embeddings on every call, and the index is
  shared between the tool and the search instead of being loaded twice.
- Fixed an agent with no token being unusable for embeddings, which is how a local embedding server
  is normally configured.
- The `Use RAG` search no longer has a similarity threshold to guess. Every index build now measures
  what a query with no answer scores on your model and your solution, and the new `Sensitivity`
  setting says how far above that measured level a file has to stand — one value which keeps its
  meaning when you change the embedding model. You can add your own questions, including ones whose
  answer you know, in the `Calibration` node; a query which fails to find its own file is reported
  at the end of the build, because that means the model does not understand your code.
- There is a window for that measurement: `Extensions` / `FreeAIr` / `Open RAG search calibration
  window...`. Ask it the questions you would ask the search, mark the file which answers each one —
  or say that nothing here does — and it shows what the threshold lets through and what it cuts.
  Saving stores the questions in the settings and the numbers in the index without rebuilding the
  vectors, so a threshold can be fixed in a minute instead of in an hour of embedding. The window
  names the agent it is asking and lets you change it, and it describes itself in a block at the
  top. The `Создание NLO Json файла` window now carries its own description in that same block.
- Fixed an agent without a token being invisible to everything which looks an agent up by name. An
  embedding server run locally wants no token, so the agent which had built the index was never
  found and a cloud chat agent was silently used to vectorize the query instead — which answers a
  request for embeddings with HTTP 400. The natural language search was affected as well.
- The natural language search no longer gives up in silence. It used to walk through the scope, the
  action and the agent pickers, and a `return` at any of them left the button looking broken: no
  window, no message, nothing in the log. Every one of those steps now says why it stopped — the
  most likely reason being that no agent has a token, which is how the agents of a local server are
  normally configured. An empty file mask in `Find in Files` means every file now, instead of
  killing the search with an exception nobody saw.
- Every step of the natural language search writes itself into a `FreeAIr natural language search`
  pane of the Output window: which agent it asks, how many files the mask matched, what the RAG
  shortlist kept, how big the prompt and the answer were, and how many matches of the answer were
  usable. An answer which carries no `matches` is reported as such instead of being counted as
  `Found 0 items`.
- Fixed the search results window failing to open at all: its progress bar was bound two way onto a
  read only property, which throws while the window is being built.
- The results window names the agents behind the answers: the one which was asked, and — with
  `Use RAG` — the one which turned the query into a vector. The second one is normally chosen for
  you, from the index, and until now there was no way to see which one it was.
- The `Confidence` column reads `High (85)` instead of `85`. The number is a guess of the model, not
  a measurement, and the word says how much of the list is worth opening. The boundaries are 70 and
  40.
- Fixed the `Edit actions` window: clicking an action did nothing. Its `SelectedAction` was writing
  into the backing field of the property while the whole rest of the window kept reading a separate
  field, which therefore stayed empty — the editing panel was hidden and every command disabled.
- An embedding server which refuses a request is now quoted. `HTTP 400` on its own says nothing
  about which model, which endpoint or which parameter it did not like, and the server had already
  explained all of it in an answer that was being thrown away.
- The index remembers which embedding model built it, by storing the vectors of a few fixed
  sentences. A search with a different model is now refused instead of silently returning nonsense —
  the vector length alone did not catch it, since different models often share it. The model name
  the server itself reports is recorded too, which is the only name that changes when a local server
  is given a different model to load.
- The `RagTopOutlineCount`, `RagMaxFileCount` and `RagMinScore` options moved from the unsorted
  settings into a `Rag` node of their own, and `RagMinScore` is gone: it is replaced by `Sensitivity`
  above.
- The built-in Visual Studio MCP server has a new tool, `VisualStudio.SearchFileContent`: the model
  can ask where a text occurs and gets the matching lines back — each with its file and its line
  number — instead of reading whole files one by one until it finds them. Nothing is installed and
  no process is started, the matching is done by the extension itself.
- The pattern is literal by default and a .NET regular expression on request, both through the same
  code path. `case_sensitive` and `whole_word` are the other two switches, and `invert_match`
  reports the lines which do not match, the way `grep -v` does.
- `search_scope` chooses between the files of the projects — the default — and every file in the
  solution folder. The walk skips `bin`, `obj`, `.git`, `node_modules` and the like either way, so
  a search does not answer with build output.
- `file_mask` is a list like `*.cs;*.xaml`, and a mask which starts with `!` subtracts:
  `*.cs;!*.Designer.cs` is what keeps generated code from eating the context budget.
- The answer is capped — 100 matching lines by default, 500 at most — and says whether the cap was
  reached. A truncated list which does not admit to being one is read by the model as the whole
  truth.
- A document which is open and edited but not saved yet is searched as you see it on the screen
  rather than as it is on the disk. Those buffers are collected on the main thread and the scan
  itself runs off it, so a solution of thousands of files does not freeze the IDE.
- The pattern is written by a model, so a regular expression is given a five second timeout: the
  file which makes it misbehave is reported and the rest of the search survives.
- **The new format is not backward compatible: an index built by FreeAIr 4.2.11 or earlier has to be
  rebuilt.**

## 4.2.11

- Thanks to [ish-1313](https://github.com/ish-1313) for his\her first contribution to FreeAIr project:
  - Added chat name.
  - Added ability to rename chat (by clicking on its name).
  - Added ability to filter models when asking a model provider about its available models.
- Added ability to resize in situ chat window by dragging a special control.

## 4.2.10

- Fixed bug with GetAllSolutionFiles MCP tool.

## 4.2.9

- Improved usability in case of no agent with non-empty token exists.

## 4.2.8

- Fixed bug with parsing markdown with leading \n\n.

## 4.2.7

- Fixed #51 (improved agent <-> action pairing).

## 4.2.6

- Fixed few bugs.

## 4.2.5

- Fixed few bugs.

## 4.2.2

- Fixed few bugs.

## 4.2.1

- Fixed stupid bug.

## 4.2.0

- Added initial support for Visual Studio 2026.
- Added voice prompting with 4 voice providers and LLM-based post-processor.
- Simplified IPC data flow with FreeAIr child process Proxy.exe.

## 4.0.2

- Added [in situ chat](https://raw.githubusercontent.com/lsoft/FreeAIr/main/in_situ_chat.gif) mode.
- BREAKING CHANGE: changed MCP servers json format in FreeAIr options json file.
- Added easy way to enable Microsoft MDSN MCP server.
- Added asking permission from the user to call any MCP tools LLM requested.
- Fixed various bugs.

## 3.7.1

- Fixed minor bug.

## 3.7.0

- Added a command to add selected files (in Solution Explorer window) to the current chat context.
- Added ability to add items to chat context with drag-and-drop.
- Improved VS theme support.

## 3.6.0

- Improved markdown parser (added bold text, added support for markdown tables).
- Fixed bugs in markdown parser.

## 3.5.0

- Improved markdown parser.
- Added ability to collapse XML nodes in LLM answers.

## 3.4.0

- Added NLOs into solution item metadata for GetAllSolutionItemsTool MCP tool. This is helpful if you are using LLM to make decisions about your solution structure (where to add new entity, etc.).

## 3.3.0

- Added Russian and simplified Chinese localization.

## 3.2.0

- Added ability to set whole line completion prompts. The suggested prompt is:
```
- In the document {CONTEXT_ITEM_NAME} suggest whole local code completion at the place where {WHOLE_LINE_COMPLETION_ANCHOR} anchor is set.
- Follow the style, the formatting and the actual indent of the provided code.
- Do not post whole modified document. Do not post anything except the code snipped you suggest to add to that place. Skip any preamble, I need only suggested code part!
- If you see that nothing can be suggested, return answer `//Nothing to suggest`.
```

## 3.1.0

- Added ability to edit available MCP servers visually.
- Added ability to install MCP servers from Docker registry.
- Added ability to hide automatically created chats.

## 3.0.0

- Major revision has changed because of breaking changes in FreeAIr properties. Now you can control FreeAIr via its Control Center. Unfortunate, previous FreeAIr properties has been deleted, please, resetup it. For additional information please refer to [readme](https://github.com/lsoft/FreeAIr/blob/main/README.md).
- Added multiagent support.
- Added chat chosen agent.
- Added a solution-related or VS-related configuration file of FreeAIr.
- Added user-defined prompts.
- Fixed various bugs.

## 2.5.0

- Switched to different rendering of LLM answers.

## 2.4.0

- Added a FreeAIr Control Center window.
- Added a global MCP tools status, and chat-scoped MCP tools status.

## 2.3.0

- Added a natural language search (across whole solution or current project).
- Implemented UI improvements.
- Added a github MCP server and Visual Studio MCP server and its tool.
- And a lot more.
