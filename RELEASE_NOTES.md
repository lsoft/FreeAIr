# FreeAIr Release Notes

If you like this project, please [gift a ★★★★★ rating](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr).

Please report any bugs to the [github repo](https://github.com/lsoft/FreeAIr).

If you are enjoying FreeAIr to the enough level to donate, there are many [small cancer patients](https://advitausa.org/au/index.php/donate/) that need your help. Please provide your help them!

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
