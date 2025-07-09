# FreeAIr

[Эта страница на русском языке](https://translate.google.com/translate?sl=en&tl=ru&hl=en&u=https://github.com/lsoft/FreeAIr&client=webapp)

[این صفحه به زبان فارسی](https://translate.google.com/translate?sl=en&tl=fa&hl=en&u=https://github.com/lsoft/FreeAIr&client=webapp)

Access to AI for free for everyone who is using Visual Studio.

![logo](https://raw.githubusercontent.com/lsoft/FreeAIr/main/logo.png)

FreeAIr is a Visual Studio extension which allows you to interact with any LLM which have OpenAI-compatible API. Even with local LLM! No artificial || political barrier injected in FreeAIr code.

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

- Searching with natural language (with RAG support)
- Explain the code
  - selected piece of the code
  - whole file
- Comment the code
  - selected piece of the code
  - whole file
- Continue writing code according to the comments
- Composing commit message
- Whole line completion
- Generate unit tests
- Fix build errors
- Support for MCP servers and their tools.
- A Visual Studio MCP server is built into FreeAIr.

# FreeAIr images (click to open)

<div style="display: flex; flex-wrap: wrap; gap: 10px;">
  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Searching with natural language" />
  </a>
  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Searching with natural language" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="List of generated outlines" />
  </a>
  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Show the difference created by the outlines" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Generate NLO-embedding json files" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/contextmenu.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/contextmenu.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Context menu" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Chat window" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Commit message" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Whole file commands" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/codelens.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/codelens.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Codelenses support" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholelinecompletion.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholelinecompletion.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Whole line completion" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/fixbuilderror.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/fixbuilderror.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Fix building error" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Main menu" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/openroutermodelpng.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/openroutermodelpng.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="OpenRouter.ai choose model window" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/controlpanel.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/controlpanel.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="FreeAIr control panel" />
  </a>

</div>

# Getting started

- Установите расширение через магазин [Visual Studio](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr).
- Если Вы хотите использовать локальный инференс, запустите свою модель и получите её `endpoint`, `token` и имя модели.
- Запустите `Visual Studio` и откройте ваш solution.
- Нажмите `Extensions` -> `FreeAIr` -> `Open control center`.
- В открывшемся окне нажмите `Edit agents`.
- Выберите любого агента (например, `Yandex General`) и настройте `endpoint`, `token`, `chosen model`. Также вы можете исправить системный промпт, но для начала используйте кнопку `Replace with general system prompt`.
- Нажмите `Apply changes` и окно закроется.
- Вы вернетесь в окно control center, и внесенные Вами изменения будут видны в json тексте. Сохраните измененный документ в файле, используя `Store to options file`.
- Нажмите `Extensions` -> `FreeAIr` -> `Open chat list window`.
- В открывшемся окне выберите `Start chat`, и выберите модель, которую настраивали. Окно выбора модели может не показываться, если у вас одна модель или только у одной модели задан токен.
- Начинайте общаться с LLM.

# Основные понятия FreeAIr

- Агент - это конкретное сочетание `endpoint`, `token`, имени модели и ее системного промпта. Одна и та же LLM может выступать в разных ролях, роль определяется её системным промптом (например, `Ты - опытный программист...`, `Ты - программист баз данных...`). В таком случае у Вас будет два агента, у которых одинаковые `endpoint`, `token` и имя модели, но разные системные промпты.
- Чат - это диалог с LLM. Вы можете вести несколько диалогов и удалять устаревшие.
- Контекст чата - это дополнительная информация, которая доступна LLM, туда добавляются разные файлы, selections и т.п. Это удобнее, чем приводить тексты в самом промпте.
- Natural Language Search - это фича FreeAIr, которая позволяет искать по вашей кодовой базе на естесственном человеческом языке.
- Natural Language Outlines - это комментарии внутри документов Вашего solution. В конечном итоге, они используются в Natural Language Search для ускорения поиска.
- Support Action - это действие, которое может предпринять FreeAIr в ответ на действия пользователя в Visual Studio.
- MCP Servers - это Model Context Protocol сервера, которые предоставляют для LLM доп. возможности.
- Tools - это возможности, которые предлагают выбранные MCP Servers.

## Настройки FreeAIr

Настройки FreeAIr делятся на две категории:

- Json настройки, которые по смыслу применяются к solution
- Настройки для конкретного пользователя Visual Studio

## Настройки для конкретного пользователя Visual Studio

Это - группа настроек для конкретного пользователя. Эти настройки каждый член Вашей команды может настраивать под себя, они сохраняются только локально в инстансе Visual Studio.

К примеру, это настройки размера шрифта для диалога с LLM. Их можно открыть через `Extensions` -> `FreeAIr` -> `Open FreeAIr properties`.

## FreeAIr JSON settings

Это - группа настроек, которые имеет смысл держать общими для всех членов Вашей команды. Эти настройки могут быть сохранены в json файл, который рекомендуется закомитить в git репозиторий. Также, эти настройки можно сохранить внутрь Visual Studio, если, по каким-то причинам, нежелательно создавать файл.

Эти настройки содержат:
- настройки агентов
- настройки MCP servers and their tools
- настройки support action
- прочие настройки, смысл которых описан прямо в Json файле.

## Agents

Агент - это конкретное сочетание `endpoint`, `token`, имени модели и ее системного промпта. Одна и та же LLM может выступать в разных ролях, роль определяется её системным промптом (например, `Ты - опытный программист...`, `Ты - программист баз данных...`). В таком случае у Вас будет два агента, у которых одинаковые `endpoint`, `token` и имя модели, но разные системные промпты.

Вы можете редактировать существующие или добавлять собственных агентов. Если у агента не задан token - агент считается неактивным.

## Чат

Чат - это основной элемент FreeAIr, где происходит общение с LLM и кодогенерация. 

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Chat window" />
</a>

Чат состоит из трех элементов:
- область диалога, где содержается промпты пользователя и ответы LLM
- область для вводимого промпта
- область контекста чата

### Область диалога чата

В этой области отображаются:
- отправленные промпты
- полученные ответы от LLM
- служебная информация (например, вызовы MCP Server Tools)

Размер этих элементов регулируется в `Tools` -> `Options` -> `FreeAIr`.

Для некоторых элементов диалога (куски кода, картинки и пр). к тексту добавляются специальные кнопки, которые позволяют удобно оперировать этими элементами:

- Copy to clipboard
- Choose context item to replace its content
- Replace the selected block of the code in the VS document
- Create new file with this

Размер этих кнопок также регулируется в `Tools` -> `Options` -> `FreeAIr`.

### Область для вводимого промпта

Это - область для введения нового промпта. Чтобы отправить введенный промпт надо нажать Ctrl+Enter, после чего начнется ожидание ответа от LLM. Ожидание ответа можно прервать кнопкой `Stop`.

Также в эту область можно дополнительно вводить:
- actions for FreeAIr: для этого необходимо ввести `/` и выбрать action из списка; список доступных support actions задается в JSON настройках.
- документы солюшена: для этого необходимо ввести `#` и выбрать необходимый документ.

### Контекст чата

Контекст чата - это дополнительная информация для LLM, например, в контекст часто добавляются документы Вашего solution. Вы можете добавить в контекст:

- документ вашего солюшена, для этого наберите `#`, выберите документ из списка и нажмите Ctrl+Enter.
- внешний документ, для этого нажмите на кнопку `Add custom file`.

Если Ваш проект настроен под использование Microsoft Copilot, то файл `copilot-instructions.md` будет автоматически добавлен в контекст чата при создании чата.

Если Ваш проект написан на C#, то Вы можете добавлять в контекст чата все файлы, зависимые от уже добавленного файла, чтобы LLM получала больше контекста. Для этого есть соответствующая кнопка рядом с именем документа.

## Support Action

Действия, которые FreeAIr способны предложить пользователю не закодированы в коде FreeAIr. Список действий - это часть Json файла настроек в секции `Supports/Actions`.

Каждое действие состоит из:
- имени.
- коллекции scope: при каких действиях пользователя это support action надо применять (например, при работе с кодом документа; внутри codelens; при работе с файлами в Solution Explorer; при формировании commit message и другие).
- промпта с якорями.
- имени агента, который должен выполнить это действие. Если имя агента не задано, FreeAIr будет предлагать пользователю выбрать агента вручную.
- image moniker - используется как иконка для отображения в контроле ввода промпта в чате FreeAIr.

Якори - это placeholder, в который добавляется соответствующая контекстная информация. Они бывают:
- имя документа(ов) в контекте чата.
- текст ошибки компиляции.
- строка, где произошла ошибка компиляции.
- колонка, где произошла ошибка компиляции.
- предпочтительный unit test framework.
- git diff с Вашими изменениями.
- natural language search query.

Вы можете редактировать существующие и добавлять свои собственные support actions.

## MCP Servers and their tools

MCP Servers - это Model Context Protocol servers, которые предоставляют для LLM дополнительные возможности (например, доступ в БД, или доступ до git). FreeAIr полностью поддерживает MCP Servers.

Существует три категории MCP servers:

- Visual Studio Embedded MCP server - встроенный в FreeAIr MCP сервер, который предоставляет LLM возможность выполнять действия внутри Visual Studio (например, компилировать проект, изменять текст документа и пр.).
- GitHub.com MCP server - это стандартный github.com MCP сервер; установить его можно из FreeAIr Control Panel.
- Прочие MCP сервера - их можно "установить", отредактировав соотв-ю секцию FreeAIr Json settings. Формат - стандартный формат Claude.

Каждый MCP server предоставляет свой набор tools. Вы можете отредактировать набор MCP серверов и их tools и закомитить этот файл в репозиторий. При создании чата, выбранные tools копируются в чат и Вы можете включать\выключать tools внутри чата, это никак не влияет на статус глобальных tools.

Примеры промптов, которые LLM может выполнить, если ей предоставить соответствующие tools:

- `commit my changes with message "newcommit"`
- `install a 3.3.6 version of "Ninject" nuget package in TestSubject project`

### Embedded Visual Studio MCP server

Этот MCP сервер предоставляет для LLM возможности по работе внутри Visual Studio, например:
- сделать git commit.
- build solution and collect errors.
- install nuget package.
- get solution tree structure.
- get document body.
- replace document body.
- и другие.

Этот MCP сервер доступен сразу и не требует никаких действий по настройке.

### Github.com MCP server

Github.com MCP server - это сервер, который предоставляет LLM возможности по работе с репозиторием на github.com. Например, через этот сервер можно попросить LLM получить список issue и попросить LLM исправить один из них.

Чтобы установить этот сервер, нажмите соответствующую кнопку в FreeAIr Command Center. Скачается и установится наиболее свежая версия github.com MCP server.

## Commit message building

FreeAIr позволяет пользователю Visual Studio использовать LLM для генерации commit message. Для этого необходимо переключиться на вкладку `Git Changes` и нажать соответствующую кнопку. Запустится новый чат с LLM, когда ответ будет получен, commit message будет скопирован в поле `Enter a message`.

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Commit message" />
</a>

Пример промпта: `suggest me the best commit message for my uncommitted changes`.

## Natural Language Search

Использование естесственного языка в поиске по солюшену или проекту помогает искать код с помощью нечетких запросов, ориентированных на смысл кода, а не на его текст.

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Searching with natural language" />
</a>

Каждый текстовый файл из солюшена (проекта) передается в LLM вместе с поисковым запросов и LLM определяет, есть ли что-то подходящее в этом файле. После обработки всех файлов, результаты собираются в окне результатов поиска:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Searching with natural language" />
</a>

Так как обрабатываются все файлы проекта, поиск может занимать значительное время. Чтобы ускорить его, FreeAIr поддерживает RAG с использованием natural language outlines.

## Natural Language Outlines

Natural Language Outlines - это комментарии специального вида, встроенные прямо в исходный код. Эти комментарии создаются LLM, и использнуются для создания эмбеддингов, которые используются в RAG. Во FreeAIr NLO реализованы в соответствии со [статьей](https://arxiv.org/html/2408.04820v4).

### Generating NLO

Первым этапом необходимо сгенерировать NLO и добавить их в файлы исходного кода. При начале этой работы следует сгенерировать NLO для всего солюшена через данное меню:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Whole file commands" />
</a>

Далее, Вы можете инкрементально добавлять NLO только в те файлы, что были изменены, используя это меню:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Commit message" />
</a>

Само окно просмотра и сохранения NLO устроено тривиально:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="List of generated outlines" />
</a>

Снимая галочку, Вы можете включать или отключать добавление конкретного outline. Нажав `Apply` Вы сохраните outlines с проставленными галочками.

Щелкая левой кнопкой мыши по имени файла или самому outline Вы можете просматривать предлагаемые изменения:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Show the difference created by the outlines" />
</a>

### Building NLO Json file

После добавления (актуализации) NLO в файлах исходного кода, необходимо создать (обновить) JSON файлы специального вида: NLO-embedding Json files. Это можно сделать для всего солюшена через меню:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Main menu" />
</a>

или только для измененных файлов через меню:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Commit message" />
</a>

В любом случае, откроется окно:

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Generate NLO-embedding json files" />
</a>

Настройте всё необходимое и создайте (обновите) json файлы. В этих файлах содержится:

- информация обо всех NLO (а также об обычных комментариях в коде)
- эмбеддинги всех NLO

Если файлы созданы, то в меню

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Searching with natural language" />
</a>

чекбокс `Use RAG` станет доступным. При его выборе, FreeAIr сначала выбирает подходящие эмбеддинги, по ним восстанавливает документы, в которых содержатся NLO, связанные с выбранными эмбеддингами и передает эти документы в поисковый движок (вместо всех документов солюшена или проекта).

Эти Json файлы рекомендуется сохранять в git репозиторий, чтобы функция natural language search работала для всех членов команды.

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
- [Yandex](https://ya.ru) for supporting grant to access Yandex LLMs.
- to you, visitor. Thanks for reading this. If you are enjoying it please consider give it a ★ in the github repo and ★★★★★ rating on the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr).
