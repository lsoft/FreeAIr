OK - codelenses
OK - add specific file with `@file` possibility in prompt' `/command` too
OK - start discussion with presetted selected code in promt TextBox (with chat context)
OK - for prompts: c# additional context by roslyn references
OK - fix all warning/error in build log
OK - ошибка: `что такое C#?` решетка превращается в знак вопроса
OK - natural language search
OK   - add a probability of confidence and sort the list appropriate
OK   - different scopes (whole solution, current project)
- MCP protocol and its tools
  - auto search for mcp.json (as copilot does)
- feature: `implement with freeair` in context menu of editor if user clicked on throw notimplementedexception or in its codelens
- chain of answer files, and collection of Mdxaml controls (for efficience: in long dialogs rewriting a whole chat text may become slow)
- integration with static code tools (viva64?)
- custom prompt text (add to new option page)

MCP servers:
- github
- VS
  - list of files (solution items)
  - analyze file body
  - change file body or its part
  - build
  - access to warning\error list
- git (https://github.com/geropl/git-mcp-go)
  - analyze history of git commits
  - access to branches information
- websearch (https://github.com/claw256/mcp-web-search OR https://github.com/williamvd4/web-search and A LOT OF OTHERS)
- pandoc (https://github.com/vivekVells/mcp-pandoc OR https://github.com/Klavis-AI/klavis/tree/main/mcp_servers/pandoc)
- sql server (https://github.com/aekanun2020/mcp-server/ and A LOT OF OTHERS)
  - access to schema
  - access to data
- postgres (https://github.com/crystaldba/postgres-mcp)
  - access to schema
  - access to data
- gitlab
- nuget
- clickhouse (https://github.com/ClickHouse/mcp-clickhouse?tab=readme-ov-file)

(отдельное расширение) - автоперевод комментов, текстов (через adornments) на язык, выбранный как язык ответов LLM
