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

A successful build emits ~739 warnings. These are pre-existing and not a regression:

- `NU1902` / `NU1903` — known vulnerabilities in `MessagePack` 3.1.4.
- `VSTHRD010` — access to VS objects off the main thread.
- `CVST005` — commands not initialized in `FreeAIrPackage`.
- `NU1603` / `NU1608` — dependency version resolution mismatches.

Judge a build by the error count, not the warning count.

## Odds and ends

- `FreeAIr\FreeAIr_4kvisczc_wpftmp.csproj` is a transient project the WPF build generates. It is not
  part of the solution; ignore it and never edit it.
- `TestSubject\` is a sample solution for manual testing and is not part of the product. Its files
  are often dirty in `git status`; leave them alone unless asked.
