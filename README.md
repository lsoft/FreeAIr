# FreeAIr

[Эта страница на русском языке](https://translate.google.com/translate?sl=en&tl=ru&hl=en&u=https://github.com/lsoft/FreeAIr&client=webapp)

Access to AI for free for anyone.

![logo](https://raw.githubusercontent.com/lsoft/FreeAIr/main/logo.png)

FreeAIr is a Visual Studio extension which allows you to interact with any LLM which have OpenAI-compatible API. No artificial|political barrier included in FreeAIr code.

[Download VSIX](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr)

# QA

Q: Wait, another one?!

A: Yes, yet another LLM VSIX :)


Q: Why? There is a fantastic Copilot!

A: Because Copilot is a subject of politics. Today you are enjoying Copilot, tomorrow your country has been banned from it. FreeAIr is not a political subject and provides no artificial barriers.

# Features

Source's context menu commands are available:

- Explain the code
- Comment the code
- Optimize the code
- Continue writing code according to the comments
- Chat
- Composing commit message
- Whole line completion

Context menu:

![Context Menu](https://raw.githubusercontent.com/lsoft/FreeAIr/main/contextmenu.png)

Chat window:

![Chat window](https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png)

Composing commit message:

![Composing commit message](https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png)

Main menu:

![Main Menu](https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png)

Options:

![Options Page 1](https://raw.githubusercontent.com/lsoft/FreeAIr/main/apipage.png)

![Options Page 2](https://raw.githubusercontent.com/lsoft/FreeAIr/main/reponsepage.png)

OpenRouter.ai choose model window:

![OpenRouter Model](https://raw.githubusercontent.com/lsoft/FreeAIr/main/openroutermodelpng.png)

Whole line completion: 

Put cursor in the place of your source code where you need an autocompletion and press Alt+A:

![Whole line completion](https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholelinecompletion.png)


# How I can access to AI if my country is banned from Copilot?

0. Install this VSIX into your Visual Studio. You will need to have Visual Studio 2022 v.17.10 at least.
1. Register on [openrouter.ai](openrouter.ai). This is easily can be done via github.com account. Also, you can obtain access to any LLM with OpenAI compatible API. FreeAIr itself does not include any LLM.
2. Choose any `free` model at [openrouter.ai](openrouter.ai) and create an access token.
3. Put your token into options page (see screenshot above).
4. That's all, enjoy! But remember: for a free LLM [openrouter.ai](openrouter.ai) has daily limits (50 prompts per day, if I remember correctly).

FreeAIr itself has no restrictions, you are able to switch another OpenAI compatible API.

# Thanks

- [openrouter.ai](openrouter.ai) for free access.
- [CCodeAI](https://github.com/TimChen44/CCodeAI) for inspiration.
- [L.AI](https://github.com/cntseesharp/L.AI) for inspiration.
