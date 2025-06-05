# FreeAIr

[Эта страница на русском языке](https://translate.google.com/translate?sl=en&tl=ru&hl=en&u=https://github.com/lsoft/FreeAIr&client=webapp)

Access to AI for free for anyone.

![logo](https://raw.githubusercontent.com/lsoft/FreeAIr/main/logo.png)

FreeAIr is a Visual Studio extension which allows you to interact with any LLM which have OpenAI-compatible API. Even with local LLM! No artificial | political barrier injected in FreeAIr code.

[Download VSIX](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr)

# QA

Q: Wait, another one?!

A: Yes, yet another LLM VSIX :)


Q: Why? There is a fantastic Copilot!

A: Because Copilot is a subject of politics. Today you are enjoying Copilot, tomorrow your country has been banned from it. FreeAIr is not a political subject and provides no artificial barriers.


Q1: I have NO access to any LLM in Internet via public API.

Q2: I have NO rights to use any remote LLM because of license of the code I'm writing.

A: No problem! You can use local LLM, for example via KoboldCpp. Local LLM sends nothing to remote servers. See details below.


# Features

Main functions:

- Searching with natural language
- Explain the code
  - selected piece of the code
  - whole file
- Comment the code
  - selected piece of the code
  - whole file
- Optimize the code
- Continue writing code according to the comments
- Chat
- Composing commit message
- Whole line completion
- Generate unit tests
- Fix build errors
- Support for MCP servers and its tools.
- A Visual Studio MCP server is built into FreeAIr.

Searching with natural language:

![Natural0](https://raw.githubusercontent.com/lsoft/FreeAIr/main/natural0.png)

![Natural1](https://raw.githubusercontent.com/lsoft/FreeAIr/main/natural1.png)

Context menu:

![Context Menu](https://raw.githubusercontent.com/lsoft/FreeAIr/main/contextmenu.png)

Chat window:

![Chat window](https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png)

Composing commit message:

![Composing commit message](https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png)

Whole file commands:

![Whole file commands](https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png)

Also, FreeAIr actions available via codelens:

![Code lens](https://raw.githubusercontent.com/lsoft/FreeAIr/main/codelens.png)

Whole line completion: 

Put cursor in the place of your source code where you need an autocompletion and press Alt+A:

![Whole line completion](https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholelinecompletion.png)

Fix build error:

![Fix build error](https://raw.githubusercontent.com/lsoft/FreeAIr/main/fixbuilderror.png)

Main menu:

![Main Menu](https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png)

Options:

![Options Page 1](https://raw.githubusercontent.com/lsoft/FreeAIr/main/apipage.png)

![Options Page 2](https://raw.githubusercontent.com/lsoft/FreeAIr/main/reponsepage.png)

OpenRouter.ai choose model window:

![OpenRouter Model](https://raw.githubusercontent.com/lsoft/FreeAIr/main/openroutermodelpng.png)

MCP servers:

![MCP servers](https://raw.githubusercontent.com/lsoft/FreeAIr/main/controlpanel.png)


# How I can access to AI if my country is banned from Copilot and from any other LLM provider?

This is possible. If you are banned only from Copilot:

0. Install this VSIX into your Visual Studio. You will need to have Visual Studio 2022 v.17.14 at least.
1. Register on [openrouter.ai](openrouter.ai). This is easily can be done via github.com account. Also, you can obtain access to any LLM with OpenAI compatible API. FreeAIr itself does not include any LLM.
2. Choose any `free` model at [openrouter.ai](openrouter.ai) and create an access token.
3. Put your token into options page (see screenshot above).
4. That's all, enjoy! But remember: for a free LLM [openrouter.ai](openrouter.ai) has daily limits (50 prompts per day, if I remember correctly).

FreeAIr itself has no restrictions, you are able to switch another OpenAI compatible API.

If you are banned from any remote LLM then run LLM locally, which is very easy, for example with KoboldCpp: run KoboldCpp, choose the model, wait for KoboldCpp starts (it opens browser), and then use correct OpenAI compatible endpoint like `http://localhost:5001/v1`.

# Thanks

- [openrouter.ai](openrouter.ai) for free access.
- [CCodeAI](https://github.com/TimChen44/CCodeAI) for inspiration.
- [L.AI](https://github.com/cntseesharp/L.AI) for inspiration.
- [KoboldCpp](https://github.com/LostRuins/koboldcpp/) for testing without daily limits.
- to you, visitor. Thanks for reading this. If you are enjoying it please consider give it a ★ in the github repo and ★★★★★ rating on the Visual Studio Marketplace.
