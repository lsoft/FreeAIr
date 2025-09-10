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
