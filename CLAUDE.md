# Notes for Claude

Working notes for automated agents. For the design overview read [ARCHITECTURE.md](ARCHITECTURE.md);
for the user manual read [README.md](README.md).

## Building the solution

`FreeAIr.sln` still holds one legacy-format project (`CodeLens`: `ToolsVersion="15.0"`,
`TargetFrameworkVersion v4.8`), and `FreeAIr.csproj` — although SDK-style — is a VSIX that needs
the VSSDK targets. So the solution can only be built by Visual Studio's MSBuild, not by the .NET
SDK.

Restore first, then build:

```bash
"C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" FreeAIr.sln -t:Restore
```

```bash
"C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" FreeAIr.sln -t:Build -p:Configuration=Release "-p:Platform=Any CPU" -m
```

Both `Debug` and `Release` build clean (0 errors). A full build takes roughly 20-40 seconds.

Quote `"-p:Platform=Any CPU"` as shown — the space in the value needs the whole switch inside quotes.

## Running the tests

`Search.Tests\FreeAIr.Search.Tests.csproj` (net8.0, xunit) covers `Search\FreeAIr.Search.csproj` — the
index format, the vector codec, the outline tree and the ranking of the natural language search,
plus the `Grep\` text matching behind the SearchFileContent MCP tool.

`SetupWizard.Tests\FreeAIr.SetupWizard.Tests.csproj` (net8.0, xunit) covers
`SetupWizard\FreeAIr.SetupWizard.csproj` the same way — the first-run setup wizard's step
navigation, agent-field validation and known-endpoint catalog.

`Llm.Tests\FreeAIr.Llm.Tests.csproj` (net8.0, xunit) covers `Llm\FreeAIr.Llm.csproj` — the two wire
protocols FreeAIr speaks. Nothing reaches a server: a request is asserted as the JSON it becomes and
an answer is fed in as the bytes a server would have sent, through a fake `HttpMessageHandler`
(`Fakes\CannedHttpHandler.cs`). Add a case here before touching either transport.

`MCP\Tests\FreeAIr.Mcp.Tests.csproj` (**net9.0**, xunit) covers the hop to the MCP servers: the
argument conversions in `MCP\Dto`, the JSON-RPC channel to `Proxy.exe`, and `Proxy`'s own
`BaseServer2` speaking `tools/list` and `tools/call`. Both ends are real — a live `JsonRpc` pair and
a live MCP server on an in-process duplex stream — so nothing is spawned and the whole file runs in
under a second. It is net9.0 rather than net8.0 because it references `MCP\Proxy`, which is net9.0.

Use the script; it builds with MSBuild and only then hands over to the SDK:

```bash
run-tests.bat
```

Anything you add to the command line goes on to `dotnet test`, e.g.
`run-tests.bat --filter FullyQualifiedName~VectorCodec`. `FREEAIR_CONFIG` picks the configuration
(`Release` by default), `FREEAIR_MSBUILD` overrides the compiler path.

Only the four test projects and what they reference (`FreeAIr.Search`, `FreeAIr.SetupWizard`,
`FreeAIr.Llm`, `Dto`, `Proxy`) are built by the script — all SDK style, so it takes a couple of seconds and does
not go near the VSIX. Build the solution yourself when you need the VSIX too; the script will then
find everything up to date.

By hand it is the same two steps per project, and the second one must **not** let the .NET SDK build
anything:

```bash
dotnet test Search.Tests/FreeAIr.Search.Tests.csproj --no-build -c Release --nologo
```

`--no-build` is what makes this safe: it implies `--no-restore`, so the SDK never rewrites the
`obj\` of the VSIX projects (see the next section). Dropping that flag reproduces exactly the
breakage described below.

### Integration tests

`EmbeddingIntegrationFacts` talks to a real embedding server through the shipping
`OpenAIEmbeddingVectorizer`. It skips itself unless `FREEAIR_TEST_EMBEDDING_ENDPOINT` names one, so
a plain `run-tests.bat` stays green without a model:

```bash
run-integration-tests.bat
```

That defaults to a local OpenAI compatible server at `http://localhost:5001/v1` and runs the
integration tests only, with the console logger verbose enough to show what the model answered.
`FREEAIR_TEST_EMBEDDING_MODEL` (default: the first model the server lists) and
`FREEAIR_TEST_EMBEDDING_TOKEN` (default: none) override the rest.

## Never use `dotnet build`

It fails, and it also **breaks the next MSBuild build**.

The failure itself is expected: `dotnet build` cannot resolve `Microsoft.VisualStudio.*` or even
`System.ComponentModel` for these projects, and reports a wall of `CS0234`.

The real problem is the restore it runs first. It rewrites `obj\project.assets.json` and
`obj\*.nuget.g.props` with .NET SDK semantics, after which MSBuild refuses to build with:

```
error : Your project file doesn't list 'win' as a "RuntimeIdentifier".
```

To recover, re-run `-t:Restore` with full MSBuild. No clean is needed.

## Locating MSBuild

`vswhere.exe` in `Program Files (x86)\Microsoft Visual Studio\Installer\` exists but returns an
**empty list** on this machine — VS 18 Insiders is not registered in its instance store. The
`Program Files\Microsoft Visual Studio\2022` directory is a leftover and contains no MSBuild.

So the path has to be given explicitly, and anything that discovers the toolchain through `vswhere`
will silently find nothing. If the hard-coded path above stops working, search for `MSBuild.exe`
under `Program Files\Microsoft Visual Studio` rather than trusting `vswhere`.

## Build output

| Path | Notes |
| --- | --- |
| `FreeAIr\bin\<Config>\FreeAIr.vsix` | The extension. ~29 MB in Debug, ~13 MB in Release. |
| `.art\Proxy.zip` | `MCP/Proxy`'s output directory, zipped by the `PostBuild` target in `Proxy.csproj`. Embedded into the VSIX. |
| `.art\WhisperNet.Runtime.zip` | Whisper native runtimes, zipped by the `ZipWhisperRuntimes` target in `Voice\FreeAIr.Voice.csproj` via `.CreateZipArchive.ps1`. |

Both `.art` zips are regenerated on every build. If they are missing or stale the VSIX still builds,
but the MCP proxy or voice recording will fail at runtime.

## Expected warnings

A successful full build emits ~800 warnings. These are pre-existing and not a regression:

- `NU1902` / `NU1903` — known vulnerabilities in `MessagePack` 3.1.4.
- `VSTHRD010` — access to VS objects off the main thread.
- `CVST005` — commands not initialized in `FreeAIrPackage`.
- `NU1603` / `NU1608` — dependency version resolution mismatches.

Judge a build by the error count, not the warning count.

## Writing comments the NLO index can use

FreeAIr indexes its own source through `CSharpFileScanner`
(`FreeAIr\Nlo\NLOutline\Tree\Builder\File\FileScanner.cs`), and the comments are the *only* thing that
ends up in the embedding index — no code is ever vectorized. A node whose outline text equals its
own identifier is dropped by `OutlineEmbedder.SelectNodesToEmbed`, so **an uncommented member is
absent from the RAG search entirely**. Comments here are a searchable artifact, not decoration.

What the scanner actually reads:

| Declaration | What it takes |
| --- | --- |
| class / struct / interface / record / enum / delegate | leading trivia only: the `<summary>` plus any `//` lines directly above it |
| method / ctor / property / field / event / indexer / enum constant | the `<summary>` **and every `//` comment inside the body**, joined by newlines |
| the file node itself | nothing — it is always empty for C# |

Consequences worth knowing before writing anything:

- Only `<summary>` is read. `<remarks>`, `<param>`, `<returns>` and `<example>` are ignored, so
  nothing that matters may live there alone. Nested markup inside `<summary>` *is* read, including
  the names in `<see cref="..."/>` and `<paramref name="..."/>`.
- A comment at the very top of a file, above the `using`s, reaches nothing. The purpose of the file
  belongs on its main type.
- Inline `//` comments partitioning a method body land in that method's embedding. This is the
  natural-language-outline mechanism and the cheapest way to make a long method findable.
- Overloads share the target `Type.Method` and therefore one node id; only one of their summaries
  survives into the index. Put anything that distinguishes them where it is not lost.

How to write them (following the NL outlines paper, `NLO.pdf`):

- One to three sentences of plain prose. Say what the thing is *for* and how it fits with the rest,
  never what the signature already states.
- Use the nouns somebody would type into the search box: the domain terms, the file formats, the
  protocols, the name of the window or the settings page. Those words are the entire retrieval
  surface.
- Explain *why* where the reasoning is not obvious; do not narrate the code.
- Inside a body, one `//` line per logical section — at most three for a short method, five for a
  long one. Never comment every line.
- Detail costs the reader time and blurs the vector. Enough to understand, no more.

`run-coverage` is not a script; to measure, count the nodes whose outline differs from their target.
The baseline when this section was written was 918 of 4299 nodes (21%).

## Odds and ends

- **A new `.cs` file in `CodeLens\` has to be added to the `.csproj` by hand.** It is still legacy
  format and lists every file in a `<Compile Include="..." />` item; a file which is not listed is
  silently not compiled, and the build still reports 0 errors. Nothing fails, the code is simply
  not there at runtime — which for something found by reflection looks like the feature not
  working rather than like a build problem. Every other project globs its sources and needs
  nothing of the sort (`Shared\` did too until it was converted to SDK format).
- **A new assembly has to be wired into the VSIX in three places**, and forgetting any of them
  builds clean and fails at run time: an `<IncludeOutputGroupsInVSIX/>` on the `ProjectReference`
  in `FreeAIr.csproj` (or the dll is not in the package), a `MefComponent` asset in
  `source.extension.vsixmanifest` if it exports MEF parts (or the exports vanish), and
  `SatelliteDllsProjectOutputGroup` if it is localized. `FreeAIr.Voice` is the worked example.
- **Nothing weakly typed may cross the JSON-RPC channel to `Proxy.exe`.** `JsonRpc.Attach` without
  a formatter uses the Newtonsoft one, which revives an `object` member as a `JObject`/`JArray` for
  everything that is not a primitive. Handing that to System.Text.Json — which is what the MCP SDK
  serializes `tools/call` with — writes out the token's *children* rather than its value, because
  every `JToken` implements `IEnumerable<JToken>`: a `JValue` becomes `[]`, an object becomes an
  array of its properties. Nothing throws, the call succeeds, and the server silently receives
  nested empty arrays (issue #70). Carry such payloads as raw JSON text — `ToolArguments` and
  `GetToolReply.Parameters` both do — and pin it with a test in `MCP\Tests`.
- **Nothing above `ILlmTransport` may name a wire protocol.** Two are spoken and they disagree
  about more than names: Anthropic has no system role (the prompt is a field of the request), no
  tool role (a result is a block inside a *user* message), requires `max_tokens`, calls a schema
  `input_schema`, and wants every tool call of a turn in one assistant message rather than paired
  with its result. A chat content which built request messages itself could therefore only ever
  serve one of them. Add to `FreeAIr.Llm`'s neutral model and let the transports differ — and add
  the case to `Llm.Tests` first, since neither endpoint is reachable from a test run.
- **`Microsoft.Bcl.AsyncInterfaces` is pinned at the version the VSIX ships, and `FreeAIr.Llm` must
  not override it.** `FreeAIr.Search` does override it (System.ClientModel wants a newer one on
  netstandard2.0) and gets away with it because that version never reaches its public API;
  `FreeAIr.Llm` returns `IAsyncEnumerable`, so a higher version there fails the VSIX build with
  `CS1705` rather than warning. Referencing `FreeAIr.Llm` from a project which inherits the central
  pin — the setup wizard's logic assembly, say — fails the *restore* with `NU1109` instead, which is
  why `KnownEndpointCatalog` recognises the Anthropic endpoint with a string comparison rather than
  by holding an `LlmProtocol`.
- **`clr-namespace:` in XAML means the current assembly unless `;assembly=` says otherwise.** Moving
  a type that XAML names into another assembly compiles the C# fine and then fails the markup
  compiler with `MC3050: cannot find type`. Every `xmlns:resources="clr-namespace:FreeAIr.Resources"`
  needed `;assembly=FreeAIr.Resources` appended when the strings moved out.
- **Satellite assemblies of a referenced project reach the VSIX packer twice** — from the project's
  `obj\` with the culture folder in `TargetPath`, and from its `bin\` with no `TargetPath` at all.
  The second copy lands in the root of the `.vsix`, where nothing probes for it, and since every
  culture flattens to one name there the packer silently drops all but the first. The
  `RemoveFlattenedSatellites` target in `FreeAIr.csproj` throws those away. Check for a stray
  `*.resources.dll` in the root of the package if you add another localized assembly.
- **Do not use a `PostBuildEvent` property in an SDK-style project here.** Property values are
  expanded during evaluation, before the SDK targets define `$(TargetDir)`, so the command runs
  with an empty path. Use a `<Target AfterTargets="Build">` with `<Exec/>` instead — see
  `ZipWhisperRuntimes` in `Voice\FreeAIr.Voice.csproj`.
- **F5 is configured through `AdditionalArguments`, and `StartArguments` does nothing.** Now that
  the VSIX project is SDK-style there is no VSIX project flavor (`ProjectTypeGuids`) owning the
  Debug page; the launch comes from Visual Studio's own extensibility project system
  (`Common7\IDE\Extensions\VSSDK\ProjectSystem`, rule `VsixDebugger`), which starts
  `devenv /rootSuffix Exp` on its own and reads only that rule's properties -
  `AdditionalArguments`, `VSSDKTargetPlatformRegRootSuffix`, `DeployTargetInstanceId`. They are
  `Persistence="UserFile"`, so they live unconditioned in `FreeAIr.csproj.user`. Anything set in
  `StartAction`/`StartProgram`/`StartArguments` evaluates fine on the command line and is ignored
  by the IDE, which makes it a good way to waste an afternoon.
- `FreeAIr\FreeAIr_*_wpftmp.csproj` is a transient project the WPF build generates. It is not
  part of the solution; ignore it and never edit it.
- `TestSubject\` is a sample solution for manual testing and is not part of the product. Its files
  are often dirty in `git status`; leave them alone unless asked.
