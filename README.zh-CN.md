# FreeAIr

[English version of this page](https://github.com/lsoft/FreeAIr)

让每一位 Visual Studio 用户都能免费使用 AI。

![logo](https://raw.githubusercontent.com/lsoft/FreeAIr/main/logo.png)

FreeAIr 是一款 Visual Studio 扩展，可让您与任何具有 OpenAI 兼容 API 的 LLM 进行交互。即使是本地的 LLM！FreeAIr 代码中没有人为的政治障碍。

[下载 VSIX](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr)

# 问答

问：等等，又来一个？！

答：是的，又一个 LLM VSIX :)


问：为什么？已经有很棒的 Copilot 了！

答：因为 Copilot 受政治因素影响。今天您享受 Copilot，明天您的国家可能就被禁用了。FreeAIr 不受政治影响，也没有人为的障碍。


问1：我无法通过公共 API 访问互联网上的任何 LLM。

问2：由于我正在编写的代码的许可证，我无权使用任何远程 LLM。

答：没问题！您可以使用本地 LLM，例如通过 KoboldCpp。本地 LLM 不会将任何内容发送到远程服务器。详见下文。


# 功能

主要功能：

- 使用自然语言搜索（支持 RAG）
- 解释代码
  - 选定的代码片段
  - 整个文件
- 注释代码
  - 选定的代码片段
  - 整个文件
- 根据注释继续编写代码
- 撰写提交信息
- 整行补全
- 生成单元测试
- 修复构建错误
- 支持 MCP 服务器及其工具。
- FreeAIr 内置了一个 Visual Studio MCP 服务器。

# FreeAIr 图片（点击打开）

<div style="display: flex; flex-wrap: wrap; gap: 10px;">
  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="使用自然语言搜索" />
  </a>
  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="使用自然语言搜索" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="生成的提纲列表" />
  </a>
  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="显示提纲创建的差异" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="生成 NLO 嵌入 json 文件" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/contextmenu.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/contextmenu.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="上下文菜单" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="聊天窗口" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="提交信息" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="整个文件命令" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/codelens.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/codelens.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="Codelens 支持" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholelinecompletion.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholelinecompletion.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="整行补全" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/fixbuilderror.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/fixbuilderror.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="修复构建错误" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="主菜单" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/openroutermodelpng.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/openroutermodelpng.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="OpenRouter.ai 选择模型窗口" />
  </a>

  <a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/controlpanel.png" target="_blank">
    <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/controlpanel.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="FreeAIr 控制面板" />
  </a>

</div>

# 入门

- 通过 [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr) 安装扩展。
- 如果要使用本地推理，请运行您的模型并获取其 `endpoint`、`token` 和模型名称。
- 运行 `Visual Studio` 并打开您的解决方案。
- 单击 `扩展` -> `FreeAIr` -> `打开控制中心`。
- 在打开的窗口中，单击 `编辑代理`。
- 选择任何代理（例如 `Yandex General`）并配置 `endpoint`、`token`、`chosen model`。您还可以修复系统提示，但首先使用 `替换为通用系统提示` 按钮。
- 单击 `应用更改`，窗口将关闭。
- 您将返回到控制中心窗口，您所做的更改将在 json 文本中可见。使用 `存储到选项文件` 将修改后的文档保存到文件中。
- 单击 `扩展` -> `FreeAIr` -> `打开聊天列表窗口`。
- 在打开的窗口中，选择 `开始聊天`，然后选择您配置的模型。如果您有一个模型或只有一个模型设置了令牌，则可能不会出现模型选择窗口。
- 开始与 LLM 聊天。

# FreeAIr 的基本概念

- 代理是 `endpoint`、`token`、模型名称及其系统提示的特定组合。同一个 LLM 可以扮演不同的角色，角色由其系统提示定义（例如，`你是一位经验丰富的程序员...`，`你是一位数据库程序员...`）。在这种情况下，您将有两个具有相同 `endpoint`、`token` 和模型名称但系统提示不同的代理。
- 聊天是与 LLM 的对话。您可以有多个对话并删除过时的对话。
- 聊天上下文是 LLM 可用的附加信息，其中添加了各种文件、选择等。这比在提示本身中提供文本更方便。
- 自然语言搜索是 FreeAIr 的一项功能，可让您使用自然人类语言搜索代码库。
- 自然语言大纲是解决方案文档中的注释。它们最终被自然语言搜索用来加快搜索速度。
- 支持操作是 FreeAIr 可以响应 Visual Studio 中用户操作而采取的操作。
- MCP 服务器是模型上下文协议服务器，可为 LLM 提供附加功能。
- 工具是选定的 MCP 服务器提供的功能。

## FreeAIr 设置

FreeAIr 设置分为两类：

- 应用于解决方案的 Json 设置
- 特定 Visual Studio 用户的设置

## Visual Studio 用户特定设置

这是一组用户特定的设置。您团队的每个成员都可以为自己自定义这些设置，它们仅保存在 Visual Studio 实例的本地。

例如，这些是 LLM 对话框的字体大小设置。可以通过 `扩展` -> `FreeAIr` -> `打开 FreeAIr 属性` 打开它们。

## FreeAIr JSON 设置

这是一组设置，对于团队的所有成员来说，保持通用是有意义的。这些设置可以保存在 json 文件中，建议将其提交到 git 存储库。此外，如果由于某种原因不希望创建文件，这些设置也可以保存在 Visual Studio 内部。

这些设置包含：
- 代理设置
- MCP 服务器及其工具设置
- 支持操作设置
- 其他设置，其含义直接在 Json 文件中描述。

## 代理

代理是 `endpoint`、`token`、模型名称及其系统提示的特定组合。同一个 LLM 可以扮演不同的角色，角色由其系统提示确定（例如 `你是一位经验丰富的程序员...`，`你是一位数据库程序员...`）。在这种情况下，您将有两个具有相同 `endpoint`、`token` 和模型名称但系统提示不同的代理。

您可以编辑现有代理或添加自己的代理。如果代理没有令牌，则该代理被视为非活动状态。

## 聊天

聊天是 FreeAIr 的核心元素，在这里进行与 LLM 的通信和代码生成。

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/chatwindow.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="聊天窗口" />
</a>

聊天由三个元素组成：
- 对话区，包含用户的提示和 LLM 的响应
- 输入提示区
- 聊天上下文区

### 聊天对话区

该区域显示：
- 已发送的提示
- 从 LLM 收到的响应
- 服务信息（例如 MCP 服务器工具调用）

这些元素的大小在 `工具` -> `选项` -> `FreeAIr` 中调整。

对于某些对话元素（代码块、图像等），文本中会添加特殊按钮，以便您方便地操作这些元素：

- 复制到剪贴板
- 选择要替换其内容的上下文项
- 替换 VS 文档中选定的代码块
- 使用此内容创建新文件

这些按钮的大小也在 `工具` -> `选项` -> `FreeAIr` 中调整。

### 提示输入区

这是输入新提示的区域。要发送输入的提示，请按 Ctrl+Enter，之后 LLM 将等待响应。您可以通过按 `停止` 按钮中断响应。

您还可以在此区域中输入以下内容：
- FreeAIr 的操作：为此，请输入 `/` 并从列表中选择一个操作；可用支持操作的列表在 JSON 设置中指定。
- 解决方案文档：为此，请输入 `#` 并选择所需的文档。

### 聊天上下文

聊天上下文是 LLM 的附加信息，例如，您的解决方案中的文档通常会添加到上下文中。您可以添加到上下文：

- 您的解决方案文档，为此，请键入 `#`，从列表中选择一个文档，然后按 Ctrl+Enter。
- 外部文档，为此，请单击 `添加自定义文件` 按钮。

如果您的项目配置为使用 Microsoft Copilot，则在创建聊天时，`copilot-instructions.md` 文件将自动添加到聊天上下文中。

如果您的项目是用 C# 编写的，您可以将所有依赖于已添加文件的文件添加到聊天上下文中，以便 LLM 获得更多上下文。为此，文档名称旁边有一个相应的按钮。

## 支持操作

FreeAIr 可以向用户提供的操作未在 FreeAIr 代码中编码。操作列表是 `Supports/Actions` 部分中 Json 设置文件的一部分。

每个操作包括：
- 名称。
- 范围集合：此支持操作应应用于哪些用户操作（例如，在处理文档代码时；在 codelens 内部；在解决方案资源管理器中处理文件时；在形成提交消息时等）。
- 带有锚点的提示。
- 应执行此操作的代理的名称。如果未指定代理名称，FreeAIr 将建议用户手动选择代理。
- 图像名字对象 - 用作在 FreeAIr 聊天的提示输入控件中显示的图标。

锚点是添加相应上下文信息的占位符。它们可以是：
- 聊天上下文中一个或多个文档的名称。
- 编译错误的文本。
- 发生编译错误的行。
- 发生编译错误的列。
- 首选的单元测试框架。
- 包含您更改的 git diff。
- 自然语言搜索查询。

您可以编辑现有的和添加自己的支持操作。

## MCP 服务器及其工具

MCP 服务器是模型上下文协议服务器，可为 LLM 提供附加功能（例如数据库访问或 git 访问）。FreeAIr 完全支持 MCP 服务器。

MCP 服务器分为三类：

- Visual Studio 嵌入式 MCP 服务器 - FreeAIr 内置的 MCP 服务器，可为 LLM 提供在 Visual Studio 内部执行操作的功能（例如编译项目、更改文档文本等）。
- GitHub.com MCP 服务器 - 这是标准的 github.com MCP 服务器；您可以从 FreeAIr 控制面板安装它。
- 其他 MCP 服务器 - 可以通过编辑 FreeAIr Json 设置的相应部分来“安装”它们。格式是标准的 Claude 格式。

每个 MCP 服务器都提供自己的一套工具。您可以编辑 MCP 服务器及其工具集并将此文件提交到存储库。创建聊天时，所选工具会复制到聊天中，您可以在聊天中启用/禁用工具，这不会影响全局工具的状态。

如果为 LLM 提供了适当的工具，LLM 可以执行的提示示例：

- `用消息“newcommit”提交我的更改`
- `在 TestSubject 项目中安装“Ninject”nuget 包的 3.3.6 版本`

### 嵌入式 Visual Studio MCP 服务器

此 MCP 服务器为 LLM 提供了在 Visual Studio 内部工作的功能，例如：
- 进行 git 提交。
- 构建解决方案并收集错误。
- 安装 nuget 包。
- 获取解决方案树结构。
- 获取文档正文。
- 替换文档正文。
- 以及其他。

此 MCP 服务器立即可用，无需任何配置步骤。

### Github.com MCP 服务器

Github.com MCP 服务器是一个服务器，它为 LLM 提供了在 github.com 上使用存储库的功能。例如，通过此服务器，您可以要求 LLM 获取问题列表并要求 LLM 修复其中一个问题。

要安装此服务器，请单击 FreeAIr 命令中心中的相应按钮。将下载并安装最新版本的 github.com MCP 服务器。

## 构建提交消息

FreeAIr 允许 Visual Studio 用户使用 LLM 生成提交消息。为此，请切换到 `Git 更改` 选项卡并单击相应的按钮。将开始与 LLM 的新聊天，当收到响应时，提交消息将被复制到 `输入消息` 字段。

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="提交信息" />
</a>

提示示例：`为我未提交的更改建议最佳提交消息`。

## 自然语言搜索

使用自然语言搜索解决方案或项目可帮助您使用模糊查询来搜索代码，这些查询侧重于代码的含义而不是其文本。

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="使用自然语言搜索" />
</a>

解决方案（项目）中的每个文本文件都与搜索查询一起传递给 LLM，LLM 确定该文件中是否有任何合适的内容。处理完所有文件后，结果将收集在搜索结果窗口中：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="使用自然语言搜索" />
</a>

由于处理所有项目文件，搜索可能会花费大量时间。为了加快速度，FreeAIr 支持使用自然语言大纲的 RAG。

## 自然语言大纲

自然语言大纲是一种直接嵌入到源代码中的特殊注释。这些注释由 LLM 生成，用于创建在 RAG 中使用的嵌入。在 FreeAIr 中，NLO 根据[论文](https://arxiv.org/html/2408.04820v4)实现。

### 生成 NLO

第一步是生成 NLO 并将其添加到源代码文件中。开始这项工作时，您应该通过此菜单为整个解决方案生成 NLO：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/wholefilecommand.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="整个文件命令" />
</a>

接下来，您可以使用此菜单仅将 NLO 增量添加到已修改的文件中：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="提交信息" />
</a>

NLO 查看和保存窗口本身设计得非常简单：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="生成的提纲列表" />
</a>

通过取消选中该框，您可以启用或禁用添加特定大纲。通过单击 `应用`，您将保存设置了复选框的大纲。

通过左键单击文件名或大纲本身，您可以查看建议的更改：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlog1.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="显示大纲创建的差异" />
</a>

### 构建 NLO Json 文件

在源代码文件中添加（更新）NLO 后，有必要创建（更新）特殊类型的 JSON 文件：NLO 嵌入 Json 文件。这可以通过菜单为整个解决方案完成：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/mainmenu.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="主菜单" />
</a>

或者仅通过菜单对修改后的文件执行此操作：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/commitmessage.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="提交信息" />
</a>

在任何情况下，都会打开一个窗口：

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlof0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="生成 NLO 嵌入 json 文件" />
</a>

设置您需要的一切并创建（更新）json 文件。这些文件包含：

- 有关所有 NLO 的信息（以及代码中的常规注释）
- 所有 NLO 的嵌入

如果创建了文件，则在菜单中

<a href="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" target="_blank">
  <img src="https://raw.githubusercontent.com/lsoft/FreeAIr/main/nlos0.png" style="height: 150px; width: auto; object-fit: contain; border: 1px solid #ccc;" alt="使用自然语言搜索" />
</a>

`使用 RAG` 复选框将变为可用。选中后，FreeAIr 首先选择合适的嵌入，重建包含与所选嵌入关联的 NLO 的文档，并将这些文档传递给搜索引擎（而不是所有解决方案或项目文档）。

建议将这些 Json 文件保存到 git 存储库，以便自然语言搜索功能对所有团队成员都有效。

警告：在 FreeAIr 3.0 版本中，`使用 RAG` 复选框尚未实现。

# 如果我的国家被禁止使用 Copilot 和任何其他 LLM 提供商，我该如何访问 AI？

这是可能的。如果您仅被禁止使用 Copilot：

0. 将此 VSIX 安装到您的 Visual Studio 中。您至少需要 Visual Studio 2022 v.17.14。
1. 在 [openrouter.ai](openrouter.ai) 上注册。这可以很容易地通过 github.com 帐户完成。此外，您还可以获得对任何具有 OpenAI 兼容 API 的 LLM 的访问权限。FreeAIr 本身不包含任何 LLM。
2. 在 [openrouter.ai](openrouter.ai) 上选择任何“免费”模型并创建访问令牌。
3. 将您的令牌放入选项页面（参见上面的屏幕截图）。
4. 就这样，尽情享受吧！但请记住：对于免费的 LLM，[openrouter.ai](openrouter.ai) 有每日限制（如果我没记错的话，每天 50 个提示）。

FreeAIr 本身没有任何限制，您可以切换到另一个与 OpenAI 兼容的 API。

如果您被禁止使用任何远程 LLM，则可以在本地运行 LLM，这非常简单，例如使用 KoboldCpp：运行 KoboldCpp，选择模型，等待 KoboldCpp 启动（它会打开浏览器），然后使用正确的与 OpenAI 兼容的端点，例如 `http://localhost:5001/v1`。

# 感谢

- [openrouter.ai](openrouter.ai) 提供免费访问。
- [CCodeAI](https://github.com/TimChen44/CCodeAI) 提供灵感。
- [L.AI](https://github.com/cntseesharp/L.AI) 提供灵感。
- [KoboldCpp](https://github.com/LostRuins/koboldcpp/) 用于无每日限制的测试。
- [Yandex](https://ya.ru) 提供访问 Yandex LLM 的资助。
- 感谢您，访客。感谢您阅读本文。如果您喜欢它，请考虑在 github 存储库中给它一个 ★，并在 [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=lsoft.FreeAIr) 上给予 ★★★★★ 评级。
