# План реализации `Use RAG` (issue #64)

Рабочий документ. Удалить после завершения работ.
Ветка: `fix/mcp-proxy-restart-and-concurrency` (на момент составления).

---

## 0. Цель

Реализовать функционал чекбокса `Use RAG` в окне «Find in Files». Сейчас чекбокс вставляется в
диалог, но `IsEnabled = false` и тултип `"DOES NOT IMPLEMENTED YET"`.

Issue: https://github.com/lsoft/FreeAIr/issues/64 (заведён как «Embedding engine is not available»,
в комментариях превратился в запрос на реализацию RAG).

Попутно решается вторая, не менее важная задача: **сокращение размера `*_embeddings.json`**,
который в текущем формате делает RAG нежизнеспособным на реальных солюшенах.

---

## 1. Как сейчас устроено (результаты изучения)

### 1.1. Генерация NLO-json

Точка входа — `GenerateEmbeddingOutlineFilesBackgroundTask`
в `FreeAIr/UI/ViewModels/BuildNaturalLanguageOutlinesJsonFileToolViewModel.cs:600`.

Конвейер:

1. `TreeBuilder.BuildAsync` (`FreeAIr/NLOutline/Tree/Builder/TreeBuilder.cs:15`) строит дерево
   `OutlineNode`: solution → project → file → class → member (`OutlineKindEnum` 1..5).
2. Листья наполняет `FileOutlineTreeProcessor`
   (`FreeAIr/NLOutline/Tree/Builder/File/FileScanner.cs:496`), выбирая сканер по расширению:
   - `CSharpFileScanner` (Order=1000, `.cs`) — Roslyn, вытаскивает `///`-summary и обычные
     комментарии. **Если комментария нет — в `OutlineText` кладётся само имя идентификатора.**
   - `FileScanner` (Order=int.MaxValue, любые расширения) — спрашивает LLM пофайлово.
   - Галка `ForceUseNLOAgent` заставляет всё гнать через LLM-сканер.
3. Инкрементальность: git diff → `CheckedPaths`; неотмеченные файлы берутся узлами из старого
   дерева (`TreeBuilderParameters.TryGetFileOutlineNode`, `TreeBuilder.cs:144`).
4. `EmbeddingGenerator.GenerateEmbeddingsAsync` (`FreeAIr/Embedding/EmbeddingGenerator.cs:49`)
   одним батчем векторизует все узлы, у которых `Embedding is null`.
5. `EmbeddingOutlineJsonObject.SerializeAsync` (`FreeAIr/Embedding/Json/Objects.cs`) пишет
   **четыре** файла.

### 1.2. Структура файлов (формат v1, до наших правок)

Путь: `<solution folder>\.freeair\<solution name>_embeddings.json`,
считается `FreeAIrOptions.ComposeEmbeddingsFilePathAsync()` → `ComposeFilePathAsync("embeddings")`
(`FreeAIr/Options2/FreeAIrOptions.cs:456`).

| Файл | Содержимое | TestSubject |
|---|---|---|
| `<sln>_embeddings.json` | `FilePath` + `GenerateDateTime` | 163 Б |
| `<sln>_embeddings.outlineTree.json` | дерево из одних `Id` → `Children` | 8 КБ |
| `<sln>_embeddings.outlines.json` | плоский список `{Id, Kind, FullPath, Target, OutlineText}` | 13 КБ |
| `<sln>_embeddings.embeddings.json` | плоский список `{Id, Embedding}` | **1.5 МБ** |

- Связка трёх файлов — по `Id` = `MD5(Kind + ":" + Target + ":" + RelativePath)`
  (`FreeAIr/NLOutline/Tree/OutlineNode.cs:96`), детерминированный.
- Сборка обратно — `EmbeddingOutlineJsonObject.CreateTripleDictionary()`.
- Вектор кодируется `PseudoX16BlobJsonConverter` (`FreeAIr/Helper/JsonConverters.cs:14`):
  байт → 2 символа алфавита `A..P`, т.е. **8 байт на float**.
- `FullPath` у **каждого** узла (включая member) — относительный путь файла. Значит узел → файл
  маппится напрямую, `outlineTree.json` для RAG вообще не нужен.

### 1.3. Natural language поиск

- `FreeAIr/Find/FindWindowModifier.cs:33` — в цикле 250 мс ищет в визуальном дереве VS диалог
  `FindFilesDialogControl`, находит кнопку `FindAll`, вставляет в её `WrapPanel` свою кнопку
  и чекбокс `Use RAG`. Текстбоксы опознаются по порядку сверху вниз: `[0]` — запрос,
  `[2]` — маска файлов. Тэги контролов: `NaturalSearchButtonTag`, `UseRagCheckBoxTag`.
  Пересканирование при закрытии диалога — через `WatchForDialogTeardown`/`Unloaded`.
- `FreeAIr/Find/DoSearch.cs:11` — три модальных выбора подряд (scope → support action → agent),
  закрытие окна Find, открытие панели результатов.
  `NaturalLanguageSearchParameters` уже несёт `UseRAG`, но **его никто не читает**.
- `FreeAIr/UI/ViewModels/NaturalLanguageResultsViewModel.cs:248`
  (`ProcessSolutionDocumentsAsync`) — обходит **все** текстовые файлы солюшена/проекта, режет на
  порции по `ContextSize` (`SplitByItemsSize`), каждую порцию кладёт в контекст чата и просит LLM
  вернуть `{"matches":[{fullpath, found_text, confidence_level, line, reason}]}`.
  Парсинг терпим к сбоям: regex-починка бэкслэшей, рекурсивный поиск ключа `matches`,
  `HasRequiredProperties`.
- GUI: `FreeAIr/UI/ToolWindows/NaturalLanguageResultsToolWindowControl.xaml` — одна строка
  `Status` + кнопка Cancel + `ListView` с колонками (пропорции `3,14,2,1`).
- Промпт поиска лежит в json-настройках, action `"Search by natural language"`,
  scope `NaturalLanguageSearch`, переменная `{NATURAL_LANGUAGE_SEARCH_QUERY}`.

### 1.4. Измерения (TestSubject, dim = 4096)

Векторы приходят уже L2-нормированными (`‖v‖ = 1.0000`), но полагаться на это нельзя.

Размер кодировок, байт на вектор:

| Формат | Б/вектор | Выигрыш |
|---|---|---|
| псевдо-hex float32 (v1) | 32 768 | 1.0× |
| base64 float32 | 21 848 | 1.5× |
| base64 float16 | 10 924 | 3.0× |
| **base64 int8** | **5 464** | **6.0×** |
| бинарный сайдкар int8 | 4 096 | 8.0× |

Качество квантования (каждый из 47 векторов как запрос):

```
float16   max|cos err| = 7.1e-05   топ-10 совпал для 47/47
int8      max|cos err| = 5.4e-03   топ-10 совпал для 47/47
```

Состав узлов TestSubject: всего 56, из них
- 9 с пустым `OutlineText` (solution + 2 project + 6 file) — вектора нет;
- **32 вырожденных** (`OutlineText.Trim() == Target.Trim()`, т.е. эмбеддинг имени
  идентификатора: `"Voyage.Id"`, `"TransferSearcher._repository"`) — **68% файла**;
- 15 с настоящим outline.

Итог: 1509 КБ → 253 КБ (JSONL + int8) → **~82 КБ** с отсевом вырожденных (**19×**).
Экстраполяция на солюшен размером с сам FreeAIr (335 `.cs`, ~2.5–3 тыс. узлов):
~96 МБ → ~5 МБ.

gzip рассматривался и **отвергнут**: git и так хранит блобы zlib-сжатыми, экономия только в
рабочей копии, ценой потери diff и читаемости.

### 1.5. Дефекты, найденные попутно

| # | Что | Где | Статус |
|---|---|---|---|
| D1 | В json нет метаданных эмбеддера — нечем определить, какой моделью считать вектор запроса | `Embedding/Json/Objects.cs` | **исправлено** |
| D2 | В json пишется абсолютный путь машины-генератора, а от него считаются имена sibling-файлов → на другой машине файлы не найдутся (а README советует коммитить их в git) | `Objects.cs`, `DeserializeAsync` | **исправлено** |
| D3 | Узлы с пустым `OutlineText` уходят в API батчем; если провайдер молча выкинет пустые входы — все последующие векторы съедут на чужие узлы | `EmbeddingGenerator.cs:61` | **исправлено** |
| D4 | `triple.EmbeddingItself.Embedding` без null-проверки, хотя `CreateTripleDictionary` кладёт туда null при промахе | `NLOutline/Tree/OutlineNode.cs:306` | **исправлено** |
| D5 | `OutlineNode.TryCreateAsync(false)` всегда падает NRE: при `full: false` под-объекты остаются null, а `CreateTripleDictionary()` сразу лезет в `Outlines.Outlines` | `OutlineNode.cs:153` | **исправлено** (метода больше нет, см. §8) |
| D6 | `GetAllSolutionFilesTool` грузит весь индекс **с эмбеддингами** на каждый вызов MCP-тула | `MCP/ServerProxy/VS/Tools/GetAllSolutionFilesTool.cs:51` | **исправлено** (общий кэш + чтение без векторов) |
| D7 | Cancel в панели результатов привязан только к `_chat.StopAsync`, фазы RAG им не прервать | `NaturalLanguageResultsViewModel.cs:235` | **исправлено** |
| D8 | `OutlineNode.ApplyRecursive(Func<,bool>)` возвращает `false` в конце обхода, из-за чего родитель прекращает перебор после первого же поддерева — инкрементальная пересборка видела только левую ветку дерева | `NLOutline/Tree/OutlineNode.cs` | **исправлено** |
| D9 | Второй поиск в панели диспозил `CancellationTokenSource` ещё работающего первого → `ObjectDisposedException` в середине | `NaturalLanguageResultsViewModel.cs` | **исправлено** |
| D10 | Колбэк отмены читал поле `_chat`, а не захваченный чат: отмена старого поиска могла остановить новый | `NaturalLanguageResultsViewModel.cs` | **исправлено** |

---

## 2. Принятые решения

1. **GUI панели результатов:** фазы + `ProgressBar` + шапка с метаданными индекса и раскрываемым
   списком отобранных файлов со score, показываемым **до** первого ответа LLM. Плюс починить Cancel.
2. **Эмбеддинг-агент:** писать в json при генерации (имя/модель/endpoint/размерность).
   Спрашивать пользователя только если агент не найден или json старого формата.
3. **Файлы без NLO в индексе:** исключать из поиска, но явно показывать сообщение
   («N файлов вне индекса»), по клику — messagebox со списком.
4. **Точность хранения:** int8 + base64, формат JSONL.
5. **Вырожденные узлы** (`OutlineText == Target`): не эмбеддить вообще. В `outlines.json` они
   остаются (нужны MCP-тулу и инкрементальной сборке), но вектор для них не запрашивается.

---

## 3. Формат файлов v2

`<sln>_embeddings.json` (метаданные):

```json
{
  "EmbeddingAgentName": "My embedder",
  "EmbeddingModel": "text-embedding-3-large",
  "EmbeddingEndpoint": "https://...",
  "EmbeddingDimensions": 4096,
  "VectorEncoding": "int8-b64-v1"
}
```

**Принцип: в файлах индекса не должно быть ничего, что меняется само по себе.** Файлы
коммитятся, и любое такое поле — гарантированный git-конфликт при слиянии двух веток, в котором
нечего разрешать. Поэтому:

- `FilePath` — **не сериализуется** (`[JsonIgnore]`). Это абсолютный путь машины-генератора,
  т.е. поле, различающееся у каждого разработчика. На чтении заполняется путём, откуда файл
  реально прочитан (это же чинит D2).
- `GenerateDateTime` — **не сериализуется**. Дата берётся из `File.GetLastWriteTime`. После
  checkout это время checkout'а, но для вопроса «индекс старше исходников или нет» это как раз
  правильный ответ: у лежащих рядом исходников время то же самое. Записанная же в файл дата
  говорила бы про чужую машину и чужой часовой пояс.
- `VectorCount` — **убран совсем**: выводится из числа строк `.jsonl`, а менялся при каждой
  перегенерации.
- Оставшиеся поля меняются только тогда, когда пользователь реально сменил модель эмбеддинга —
  это осмысленное изменение, и конфликт в нём заслужен.

Порядок строк в `.jsonl` — сортировка по `(RelativePath, Target, Id)`, т.е. чистая функция от
содержимого. По той же причине исправлен `OutlineNode.SortChildren`: он сравнивал только
`RelativePath`, общий у всех member-узлов одного файла, а `List.Sort` нестабилен — порядок в
`outlines.json` и `outlineTree.json` мог меняться между запусками на одних и тех же данных.
Теперь сравнение — полный порядок по тем же трём ключам.

Вместе с построчным форматом это даёт то, ради чего всё затевалось: два человека, правившие
разные файлы, меняют разные строки индекса, и git сливает их без конфликта.

`<sln>_embeddings.outlines.json`, `<sln>_embeddings.outlineTree.json` — **без изменений**.
В них по-прежнему лежат все 56 узлов, включая те, для которых вектор не считается: они нужны
MCP-тулу и инкрементальной сборке дерева.

`<sln>_embeddings.embeddings.jsonl` — **заменяет** `.embeddings.json`, по строке на вектор:

```
{"Id":"e534b8d1-1212-4646-21ac-8cb7b9ba50cd","V":"<base64 int8>"}
```

Строки без вектора не пишутся вообще, поэтому `VectorCount` заметно меньше числа outline.

Масштаб квантования **не хранится**: вектор всё равно L2-нормируется при загрузке, а нормировка
сокращает любой положительный масштаб.

**Обратная совместимость не нужна и не делается.** Фича RAG нигде не используется, а v1-файлы —
это только тестовые данные `TestSubject`, которые уже сконвертированы разовой утилитой (утилита
после прогона удалена). Читатель v1 (`PseudoX16BlobJsonConverter`) в коде больше не используется;
если у кого-то остался старый `.embeddings.json`, индекс надо просто перестроить.

---

## 4. Что уже сделано

### 4.1. `FreeAIr/Embedding/Json/Objects.cs`

- В `EmbeddingOutlineJsonObject` добавлены поля `EmbeddingAgentName`, `EmbeddingModel`,
  `EmbeddingEndpoint`, `EmbeddingDimensions` (с xml-doc, объясняющим зачем).
- Конструктор стал `EmbeddingOutlineJsonObject(OutlineNode root, AgentJson? embeddingAgent = null)`,
  заполняет метаданные и вычисляет `EmbeddingDimensions` по первому непустому вектору.
- В `DeserializeAsync` после десериализации `result.FilePath = filePath` (фикс D2).
- Три приватных `Get*FileName()` заменены на делегирование в новый
  `public static string GetSiblingFileName(string filePath, string part)`.
- Добавлен `using FreeAIr.Options2.Agent;`.

### 4.2. `FreeAIr/NLOutline/Tree/OutlineNode.cs`

- `triple.EmbeddingItself?.Embedding` (фикс D4).

### 4.3. `FreeAIr/Embedding/EmbeddingGenerator.cs`

- В `GenerateEmbeddingsAsync` узлы с пустым/пробельным `OutlineText` пропускаются (фикс D3).
- Добавлен `GenerateQueryEmbeddingAsync(string query, CancellationToken)`.

### 4.4. `FreeAIr/UI/ViewModels/BuildNaturalLanguageOutlinesJsonFileToolViewModel.cs`

- `new EmbeddingOutlineJsonObject(outlineRoot, _embeddingAgent)`.

### 4.5. `FreeAIr/Embedding/VectorCodec.cs` — НОВЫЙ ФАЙЛ

Готов. Содержит:
- `const string Int8Base64EncodingName = "int8-b64-v1"`, `LegacyFloat32EncodingName`;
- `string Encode(float[])` — нормировка не нужна, берётся `max|x|/127`, клип, base64;
- `float[]? DecodeNormalized(string?)` — base64 → sbyte → float → нормировка; null при мусоре;
- `bool NormalizeInPlace(float[])`;
- `float DotProduct(float[], float[])` — развёрнут по 4.

Файл прописан в `FreeAIr/FreeAIr.csproj` (строка 95).

### 4.6. Отсев вырожденных узлов — `EmbeddingGenerator.cs`

В `GenerateEmbeddingsAsync` пропускаются узлы, у которых `OutlineText.Trim() == Target.Trim()`.
Заодно добавлен `using System.Threading.Tasks;` — без него `Task<float[]>` не компилируется:
в `FreeAIrPackage.cs:4` объявлен `global using Task = System.Threading.Tasks.Task`, и алиас
перекрывает generic-версию.

### 4.7. Формат v2 — `Objects.cs`

- `EmbeddingOutlineJsonObject`: добавлен `VectorEncoding`; `FilePath` и `GenerateDateTime` стали
  `[JsonIgnore]` и заполняются при чтении (путь — откуда прочитали, дата — `LastWriteTime`).
- `GetSiblingFileName(filePath, part, extension = null)`; `GetEmbeddingsFileName()` → `.jsonl`.
- `LoadEmbeddingsAsync(IProgress<long>? bytesRead = null, CancellationToken = default)`.
- `EmbeddingsJsonObject`:
  - конструктор от `OutlineNode` больше не добавляет узлы без вектора и сортирует оставшиеся
    по `(RelativePath, Target, Id)`;
  - `SerializeAsync` пишет JSONL через `StreamWriter`, по строке на вектор;
  - `DeserializeAsync(path, IProgress<long>?, CancellationToken)` читает построчно, репортит
    `fs.Position`, возвращает уже нормированные векторы; битая строка пропускается.
- Новый DTO строки `EmbeddingLineJsonObject { Guid Id; string V; }`.
- `EmbeddingItselfJsonObject` больше не носит `[JsonConverter(typeof(PseudoX16BlobJsonConverter))]`
  — это чисто in-memory тип.

### 4.8. Конвертация тестовых данных

`TestSubject/.freeair/` переведён в v2 разовой утилитой (удалена после прогона):

```
векторов в v1 : 56   →  без outline 9, вырожденных 32, оставлено 15 (dim 4096)
1 545 219 Б  →  82 770 Б   (18.7×)
```

`TestSubject_embeddings.embeddings.json` удалён, `TestSubject_embeddings.json` приведён к v2
(`EmbeddingAgentName`/`Model`/`Endpoint` = `null` — в v1 их взять неоткуда, так что на этих данных
проверяется как раз ветка «спросить агента у пользователя»).

### 4.9. Сборка

Release собирается, 0 ошибок, `FreeAIr.vsix` на месте.

---

### 4.10. Шаги 3–11 выполнены

Реализовано, собирается, 0 ошибок:

- **`Embedding/EmbeddingIndex.cs`** (новый) — `EmbeddingIndexLoadPhaseEnum`,
  `EmbeddingIndexLoadProgress`, `EmbeddingIndexEntry`, `EmbeddingIndex` (со `Search` и
  `CoveredRelativePaths`/`IsCovered`), `EmbeddingIndexMetadata` и `[Export] EmbeddingIndexContainer`
  (`IndexExistsAsync`, `TryReadMetadataAsync`, `GetAsync`, `Invalidate`). Кэш по пути + временам
  записи трёх файлов; загрузка через `await TaskScheduler.Default`; `outlineTree.json` не читается.
  `Invalidate` не берёт семафор (её зовут с UI-потока), вместо этого счётчик поколений не даёт
  «догоняющей» загрузке опубликовать устаревший индекс.
- **`Find/RagShortlist.cs`** (новый) — `RagCandidate`, `RagShortlistResult`, `BuildAsync`,
  `FindUncovered`. Score файла = максимум по его узлам (сумма поощряла бы длинные файлы за
  количество членов). `ModelMismatch` ловит случай «выбран не тот агент».
- **`Options2/Unsorted/UnsortedJson.cs`** — `RagTopOutlineCount` (50), `RagMaxFileCount` (15),
  `RagMinScore` (0.25). Заодно в `Clone()` добавлен потерянный `WholeLineCompletionAnchorName`.
- **`Find/DoSearch.cs`** — эмбеддинг-агент определяется здесь, рядом с остальными модальными
  выборами: из метаданных индекса, а если там пусто или агент переименован — спросить у
  пользователя. Модальный диалог посреди уже идущего поиска был бы и неожиданностью, и проблемой с
  потоками. `NaturalLanguageSearchParameters.EmbeddingAgent`, `UseRAG` = `useRAG && agent is not null`.
- **`UI/ViewModels/NaturalLanguageResultsViewModel.cs`** — `ApplyRagAsync` сужает `foundRootItems`,
  фазы/прогресс (`ProgressValue`, `ProgressMaximum`, `IsProgressIndeterminate`, `ProgressVisibility`),
  шапка индекса, `Candidates`, `UncoveredText` + `ShowUncoveredFilesCommand`. Прогресс загрузки
  векторов дросселируется до целых МБ (репорт приходит на каждую строку файла). После `GetAsync`
  обязательный `SwitchToMainThreadAsync` — дальше идут привязанные к UI коллекции. D7 закрыт: токен
  прокинут в загрузку индекса и в векторизацию.
- **`UI/ToolWindows/NaturalLanguageResultsToolWindowControl.xaml`** — `ProgressBar`, блок RAG
  (описание индекса + `Expander` с кандидатами и score + кликабельная строка «файлов вне индекса»).
- **`Find/FindWindowModifier.cs`** — чекбокс включается асинхронно, если индекс есть; тултип с датой
  сборки индекса, иначе — «постройте индекс».
- **Ресурсы** — 16 строк в трёх resx + `Resources1.Designer.cs`.
- **Документация** — README (блок NOT IMPLEMENTED YET заменён описанием реального поведения),
  ARCHITECTURE.md (раздел про поиск переписан), RELEASE_NOTES.md (4.2.12).

---

## 5. Что осталось сделать

Только шаг 12, он опциональный (см. ниже). Шаги 3–11 закрыты, см. 4.10.

### Шаг 3. `FreeAIr/Embedding/EmbeddingIndex.cs` — НОВЫЙ ФАЙЛ — **СДЕЛАНО**

```csharp
public enum EmbeddingIndexLoadPhaseEnum { ReadingMetadata, ReadingOutlines, ReadingVectors, Preparing }

public sealed class EmbeddingIndexLoadProgress
{
    public EmbeddingIndexLoadPhaseEnum Phase { get; }
    public long Processed { get; }   // байты или узлы
    public long Total { get; }
}

public sealed class EmbeddingIndexEntry
{
    public Guid Id;
    public OutlineKindEnum Kind;
    public string RelativePath;   // == OutlineItselfJsonObject.FullPath
    public string Target;
    public string OutlineText;
    public float[] Vector;        // нормированный
}

public sealed class EmbeddingIndex
{
    public string FilePath { get; }
    public DateTime GenerateDateTime { get; }
    public string? EmbeddingAgentName { get; }
    public string? EmbeddingModel { get; }
    public int Dimensions { get; }
    public IReadOnlyList<EmbeddingIndexEntry> Entries { get; }
    /// все RelativePath из outlines.json, включая узлы без вектора —
    /// нужно, чтобы отличить «файл вне индекса» от «файл в индексе, но не подошёл»
    public IReadOnlyCollection<string> CoveredRelativePaths { get; }

    public List<(EmbeddingIndexEntry Entry, float Score)> Search(float[] queryVector, int topK);
}

[Export(typeof(EmbeddingIndexContainer))]
public sealed class EmbeddingIndexContainer
{
    public static Task<bool> IndexExistsAsync();
    public Task<EmbeddingIndex?> GetAsync(IProgress<EmbeddingIndexLoadProgress>?, CancellationToken);
    public void Invalidate();
}
```

Требования:
- кэш в `EmbeddingIndexContainer`: ключ — путь + `LastWriteTimeUtc` метаданных и файла векторов;
  повторный поиск не должен перечитывать сотни МБ;
- загрузка **обязательно** off-UI-thread: `await TaskScheduler.Default;`
  (паттерн уже используется, напр. `FreeAIr/BLogic/Reader/LLMReader.cs:177`);
- прогресс по байтам уже есть: `LoadEmbeddingsAsync(IProgress<long> bytesRead, ct)`; общий размер
  берётся из `FileInfo.Length` файла `.embeddings.jsonl`;
- `outlineTree.json` **не читать вообще** — для RAG не нужен;
- `Search` — линейный проход, `VectorCodec.DotProduct`, отбор топ-K.
  Для 3 тыс. × 4096 это единицы мс; ANN-индекс не нужен.

### Шаг 4. `FreeAIr/Find/RagShortlist.cs` — НОВЫЙ ФАЙЛ — **СДЕЛАНО**

Логика отбора:

1. Векторизовать запрос (`EmbeddingGenerator.GenerateQueryEmbeddingAsync`) агентом из метаданных.
2. `index.Search(query, topK)` → список узлов со score.
3. Агрегировать по `RelativePath`: score файла = max по его узлам (или сумма топ-3 — выбрать
   при реализации, начать с max).
4. Отбросить файлы со score ниже порога; отсортировать по убыванию; обрезать по `MaxFiles`.
5. Вернуть:
   - упорядоченный список относительных путей со score и лучшим совпавшим outline
     (его показываем в GUI как объяснение, почему файл отобран);
   - список путей, которых **нет** в `CoveredRelativePaths` (для сообщения «вне индекса»).

### Шаг 5. `NaturalLanguageResultsViewModel` — **СДЕЛАНО**

`FreeAIr/UI/ViewModels/NaturalLanguageResultsViewModel.cs`:

- В `ProcessSolutionDocumentsAsync` после получения `foundRootItems` (обход дерева решения уже
  есть, он дешёвый — контент файлов не читается): если `parameters.UseRAG` — прогнать RAG и
  **сузить** `foundRootItems` до отобранных путей, сохранив порядок по score.
- Файлы, отсутствующие в `CoveredRelativePaths`, собрать в `UncoveredFiles` (для GUI).
- Новые свойства для биндинга:
  - `PhaseText` (строка фазы), `ProgressValue`/`ProgressMaximum`/`IsProgressIndeterminate`;
  - `RagHeaderVisible`, `IndexGeneratedAt`, `IndexModelName`, `IndexNodeCount`;
  - `ObservableCollection2<RagCandidateItem> Candidates` (путь, score, outline);
  - `UncoveredFileCount` + `ShowUncoveredFilesCommand` → `VS.MessageBox` со списком.
- Фазы: `Загрузка индекса NLO… X/Y МБ` → `Векторизация запроса…` →
  `Ранжирование N outline…` → `Отобрано K файлов из M` → `Опрос LLM (i/K)…`.
  Строки — через ресурсы, не хардкодом.
- Починить D7: `_cancellationTokenSource.Token` прокидывать в загрузку индекса и в
  векторизацию запроса, а не только в `_chat.StopAsync`.
- Деградации:
  - индекса нет → сообщение + продолжить без RAG (или прервать — решить; предпочтительно
    честно сказать и продолжить полным поиском, спросив пользователя);
  - в json нет `EmbeddingAgentName` (старый формат) → спросить агента через
    `AgentContextMenu.ChooseAgentWithTokenAsync`;
  - агент с таким именем не найден в настройках → то же самое.

### Шаг 6. XAML панели результатов — **СДЕЛАНО**

`FreeAIr/UI/ToolWindows/NaturalLanguageResultsToolWindowControl.xaml`:

- Заменить единственный `Label` со `Status` на блок: фаза + `ProgressBar`
  (`IsIndeterminate` для сетевых шагов) + существующая кнопка Cancel.
- Добавить `Expander` «Отобрано K файлов» со списком кандидатов и score.
- Добавить кликабельный `TextBlock` «N файлов вне индекса» → `ShowUncoveredFilesCommand`.
- Стилевые хаки уже есть в файле (скрытая `StyleButtonName` для `Foreground`,
  `toolkit:Themes.UseVsTheme`) — переиспользовать, не изобретать.

### Шаг 7. `FindWindowModifier` — **СДЕЛАНО**

`FreeAIr/Find/FindWindowModifier.cs`, `CreateUseRAGCheckBox`:

- Убрать `useRAGCheckBox.IsEnabled = false;` и тултип `"DOES NOT IMPLEMENTED YET"`.
- `IsEnabled` = наличие файлов индекса (`EmbeddingIndexContainer.IndexExistsAsync()`).
  Метод синхронный по природе вызова — придётся либо считать заранее и передать флаг,
  либо выставлять асинхронно после вставки контрола.
- Тултип: восстановить закомментированный текст (строка 263) + дописать дату генерации
  индекса, если он есть; если индекса нет — «постройте NLO-embedding json файлы».

### Шаг 8. Настройки — **СДЕЛАНО**

`FreeAIr/Options2/Unsorted/UnsortedJson.cs` — добавить с `[Description(...)]`:

- `RagTopOutlineCount` (int, default ~50) — сколько узлов брать до агрегации по файлам;
- `RagMaxFileCount` (int, default ~15) — потолок файлов, уходящих в LLM;
- `RagMinScore` (double, default ~0.25) — порог косинуса.

Не забыть `Clone()` в том же файле.

### Шаг 9. Ресурсы — **СДЕЛАНО**

Новые строки нужны в **трёх** файлах + Designer:
- `FreeAIr/Resources/Resources.resx`
- `FreeAIr/Resources/Resources.ru.resx`
- `FreeAIr/Resources/Resources.zh-Hans.resx`
- `FreeAIr/Resources/Resources1.Designer.cs` (генерируемый, но в репозитории лежит —
  прописан в `FreeAIr.csproj:231`)

Существующие подходящие строки: `Idle`, `Cancelled`, `Error`, `Use_RAG`, `In_progress___0___1`.

### Шаг 10. csproj — **СДЕЛАНО**

Добавить в `FreeAIr/FreeAIr.csproj` рядом со строкой 95 (`Embedding\VectorCodec.cs` уже там):

```xml
<Compile Include="Embedding\EmbeddingIndex.cs" />
<Compile Include="Find\RagShortlist.cs" />
```

### Шаг 11. Документация — **СДЕЛАНО**

- `README.md`: удалить блок `WARNING: as of FreeAIr 4.2.12 the Use RAG checkbox IS NOT
  IMPLEMENTED YET` (строка ~459), описать реальное поведение, упомянуть новый формат и
  необходимость пересборки индекса.
- `ARCHITECTURE.md`, раздел «Natural language search / outlines» (строка ~112): убрать абзац
  «Note that the `Use RAG` flag reaches `NaturalLanguageSearchParameters` but is not consumed by
  the search yet», описать `EmbeddingIndex`/`RagShortlist`.
- `RELEASE_NOTES.md` / `changelog.md`: запись о RAG и о смене формата.

### Шаг 12 — **ЗАКРЫТ**

- Фоновый прогрев индекса — **не нужен**, решение пользователя.
- D5 и D6 — исправлены, см. §8.

---

## 8. Рефакторинг под тесты (сделано после шагов 3–11)

Задача: покрыть юнит-тестами формирование файлов индекса и сам поиск, и подготовить шов для
интеграционных тестов с настоящей embedding-моделью.

### 8.1. Новые проекты

| Проект | TFM | Содержимое |
|---|---|---|
| `Rag\FreeAIr.Rag.csproj` | netstandard2.0 | всё, что не требует Visual Studio |
| `Rag.Tests\FreeAIr.Rag.Tests.csproj` | net8.0 + xunit | 50 юнит-тестов |

VSIX-проект нельзя загрузить тест-раннером, поэтому логика вынесена в отдельную сборку. TFM
`netstandard2.0` — чтобы её потребляли и `FreeAIr` (net48), и тесты (net8.0).

Переехало: `VectorCodec`, `OutlineNode`, `OutlineItselfJsonObject`/`OutlinesItselfJsonObject`,
`EmbeddingOutlineJsonObject`/`EmbeddingsJsonObject`, `EmbeddingIndex`, `RagShortlist`,
OpenAI-клиент эмбеддингов.

Появилось: `EmbeddingIndexFileSet` (пути и ключ версии), `EmbeddingIndexReader` (+`EmbeddingIndexContent`),
`OutlineTreeAssembler`, `OutlineEmbedder`, `IEmbeddingVectorizer` + `OpenAIEmbeddingVectorizer`,
`RagShortlistOptions`.

Осталось в VSIX: `EmbeddingIndexContainer` (MEF, пути от солюшена, кэш, потоки) и `AgentEmbedding`
(перевод `AgentJson`/`UnsortedJson` в простые типы).

### 8.2. Швы

- `IEmbeddingVectorizer` — единственное место, где конвейер идёт в сеть. Юнит-тесты подставляют
  фейк, интеграционные подставят `OpenAIEmbeddingVectorizer` (он лежит в тестируемой сборке
  специально: проверять надо тот код, который поедет пользователю).
- `RagShortlistOptions` вместо `UnsortedJson`.
- `EmbeddingIndex.Build(metadata, outlines, vectors)` — индекс собирается из данных, без диска.
- `EmbeddingIndexReader.TryReadAsync(fileSet, ...)` — путь передаётся, а не берётся из солюшена.

### 8.3. Удаление `outlineTree.json`

Файл хранил только `Id → Children`. Форму дерева восстанавливает `OutlineTreeAssembler` из
`Kind` + `RelativePath` + соглашения `Тип.Член` в `Target`. Потребителей формы двое —
инкрементальная пересборка (переиспользует поддерево файла) и MCP-тул (обходит всё подряд), — и
обоим достаточно восстановленного дерева.

Заодно `FullPath` в `outlines.json` переименован в `RelativePath`: там всегда лежал относительный
путь.

### 8.4. Отменяемость

Токен теперь доходит до каждой стадии: чтение метаданных, чтение `outlines.json`, чтение
`.jsonl` (проверка на каждой строке), сборка индекса и линейный скан поиска (проверка раз в 4096
записей — чаще стоит дороже самого скалярного произведения). Обход дерева солюшена перенесён
внутрь `try`, `CancelChatCommand` больше не пробрасывает `OperationCanceledException`.

---

## 6. Как собирать

Только полный MSBuild от Visual Studio, `dotnet build` **ломает** последующие сборки
(подробности — в `CLAUDE.md`).

```bash
"C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" FreeAIr.sln -t:Build -p:Configuration=Release "-p:Platform=Any CPU" -m
```

Оценивать по числу **ошибок**: ~800 warnings — это норма, не регрессия.

Тесты — только после сборки MSBuild и только с `--no-build`:

```bash
dotnet test Rag.Tests/FreeAIr.Rag.Tests.csproj --no-build -c Release --nologo
```

---

## 7. Ловушки

1. **csproj**: каждый новый `.cs` в проекте `FreeAIr` — руками в `<Compile Include>`.
   `Rag` и `Rag.Tests` — SDK-style, там файлы подхватываются сами.
2. **Потоки**: `ProcessSolutionDocumentsAsync` стартует с UI-потока; тяжёлую загрузку индекса
   явно уводить через `await TaskScheduler.Default;`.
3. **Тестовые данные**: `TestSubject/.freeair/*` — уже формат v2 (15 векторов, dim 4096,
   без имени агента). `TestSubject/` не часть продукта, его грязный `git status` — норма.
4. **`OutlineText` файла у C#-сканера пуст** — матчить надо на уровне class/member и подниматься
   к файлу через `FullPath`.
5. Векторы провайдера уже нормированы, но нормировать при загрузке всё равно обязательно.
