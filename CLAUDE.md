# Notes for Claude

Working notes for automated agents. For the design overview read [ARCHITECTURE.md](ARCHITECTURE.md);
for the user manual read [README.md](README.md).

## Building the solution

`FreeAIr.sln` is a **legacy-format** solution: `ToolsVersion="15.0"`, `TargetFrameworkVersion v4.8`,
VSIX `ProjectTypeGuids`. It needs full MSBuild plus the VSSDK targets, so it can only be built by
Visual Studio's MSBuild — not by the .NET SDK.

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

`Rag.Tests\FreeAIr.Rag.Tests.csproj` (net8.0, xunit) covers `Rag\FreeAIr.Rag.csproj` — the index
format, the vector codec, the outline tree and the ranking of the natural language search, plus the
`Grep\` text matching behind the SearchFileContent MCP tool.

Use the script; it builds with MSBuild and only then hands over to the SDK:

```bash
run-tests.bat
```

Anything you add to the command line goes on to `dotnet test`, e.g.
`run-tests.bat --filter FullyQualifiedName~VectorCodec`. `FREEAIR_CONFIG` picks the configuration
(`Release` by default), `FREEAIR_MSBUILD` overrides the compiler path.

Only the test project and `FreeAIr.Rag` are built by the script — both are SDK style, so it takes a
couple of seconds and does not go near the VSIX. Build the solution yourself when you need the VSIX
too; the script will then find everything up to date.

By hand it is the same two steps, and the second one must **not** let the .NET SDK build anything:

```bash
dotnet test Rag.Tests/FreeAIr.Rag.Tests.csproj --no-build -c Release --nologo
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
| `.art\WhisperNet.Runtime.zip` | Whisper native runtimes, zipped by the `PostBuildEvent` in `FreeAIr.csproj` via `.CreateZipArchive.ps1`. |

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
(`FreeAIr\NLOutline\Tree\Builder\File\FileScanner.cs`), and the comments are the *only* thing that
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

- **A new `.cs` file in `FreeAIr\` has to be added to `FreeAIr.csproj` by hand.** The project is
  legacy format and lists every file in a `<Compile Include="..." />` item; a file which is not
  listed is silently not compiled, and the build still reports 0 errors. Nothing fails, the code
  is simply not there at runtime — which for something found by reflection (an MCP tool, a MEF
  export) looks like the feature not working rather than like a build problem. `Rag\`, `Shared\`
  and the other SDK style projects need nothing of the sort.
- `FreeAIr\FreeAIr_4kvisczc_wpftmp.csproj` is a transient project the WPF build generates. It is not
  part of the solution; ignore it and never edit it.
- `TestSubject\` is a sample solution for manual testing and is not part of the product. Its files
  are often dirty in `git status`; leave them alone unless asked.
