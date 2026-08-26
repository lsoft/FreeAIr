@echo off
rem ---------------------------------------------------------------------------------------------
rem  Runs the FreeAIr.Search, FreeAIr.SetupWizard and FreeAIr.Mcp unit tests.
rem
rem  The two rules this script exists to enforce (see CLAUDE.md):
rem
rem    * the projects are built by Visual Studio's MSBuild, never by `dotnet build` -- the .NET SDK
rem      cannot resolve the VSIX references and, worse, its restore rewrites obj\ so that the next
rem      MSBuild build fails until it is restored again;
rem    * `dotnet test` therefore runs with --no-build, which also implies --no-restore.
rem
rem  Everything on the command line is passed on to `dotnet test`, e.g.
rem
rem      run-tests.bat --filter FullyQualifiedName~VectorCodec
rem      run-tests.bat -v n
rem
rem  Overridable by environment:
rem      FREEAIR_MSBUILD    full path to MSBuild.exe
rem      FREEAIR_CONFIG     Debug or Release (default Release)
rem ---------------------------------------------------------------------------------------------

setlocal enabledelayedexpansion
pushd "%~dp0"

if not defined FREEAIR_CONFIG set "FREEAIR_CONFIG=Release"

rem --- find MSBuild -----------------------------------------------------------------------------
rem vswhere.exe is of no use here: VS 18 Insiders is not in its instance store and it returns an
rem empty list, so the known path is tried first and the tree is searched only if it has moved.

if not defined FREEAIR_MSBUILD (
    set "FREEAIR_MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
)

if not exist "!FREEAIR_MSBUILD!" (
    echo MSBuild is not at "!FREEAIR_MSBUILD!", searching...
    set "FREEAIR_MSBUILD="
    for /f "delims=" %%i in ('dir /b /s "%ProgramFiles%\Microsoft Visual Studio\MSBuild.exe" 2^>nul ^| findstr /i /c:"\Current\Bin\MSBuild.exe"') do (
        if not defined FREEAIR_MSBUILD set "FREEAIR_MSBUILD=%%i"
    )
)

if not defined FREEAIR_MSBUILD goto :no_msbuild
if not exist "!FREEAIR_MSBUILD!" goto :no_msbuild

echo Using MSBuild: !FREEAIR_MSBUILD!
echo Configuration: %FREEAIR_CONFIG%
echo.

rem --- build ------------------------------------------------------------------------------------
rem Only the test projects and what they reference (FreeAIr.Search, FreeAIr.SetupWizard, Dto,
rem Proxy) are built. All of them are SDK style and none pulls the VSIX in, so this is a few seconds
rem rather than a full solution build. To test against a freshly built solution, build the solution
rem yourself first -- this step will then find everything up to date.

"!FREEAIR_MSBUILD!" "Search.Tests\FreeAIr.Search.Tests.csproj" -t:Restore -nologo -v:minimal
if errorlevel 1 goto :failed

"!FREEAIR_MSBUILD!" "Search.Tests\FreeAIr.Search.Tests.csproj" -t:Build -p:Configuration=%FREEAIR_CONFIG% -nologo -v:minimal -m
if errorlevel 1 goto :failed

"!FREEAIR_MSBUILD!" "SetupWizard.Tests\FreeAIr.SetupWizard.Tests.csproj" -t:Restore -nologo -v:minimal
if errorlevel 1 goto :failed

"!FREEAIR_MSBUILD!" "SetupWizard.Tests\FreeAIr.SetupWizard.Tests.csproj" -t:Build -p:Configuration=%FREEAIR_CONFIG% -nologo -v:minimal -m
if errorlevel 1 goto :failed

"!FREEAIR_MSBUILD!" "MCP\Tests\FreeAIr.Mcp.Tests.csproj" -t:Restore -nologo -v:minimal
if errorlevel 1 goto :failed

"!FREEAIR_MSBUILD!" "MCP\Tests\FreeAIr.Mcp.Tests.csproj" -t:Build -p:Configuration=%FREEAIR_CONFIG% -nologo -v:minimal -m
if errorlevel 1 goto :failed

rem --- run --------------------------------------------------------------------------------------

echo.
dotnet test "Search.Tests\FreeAIr.Search.Tests.csproj" --no-build -c %FREEAIR_CONFIG% --nologo %*
if errorlevel 1 goto :failed

dotnet test "SetupWizard.Tests\FreeAIr.SetupWizard.Tests.csproj" --no-build -c %FREEAIR_CONFIG% --nologo %*
if errorlevel 1 goto :failed

dotnet test "MCP\Tests\FreeAIr.Mcp.Tests.csproj" --no-build -c %FREEAIR_CONFIG% --nologo %*
if errorlevel 1 goto :failed

popd
endlocal
exit /b 0

:no_msbuild
echo.
echo Could not find MSBuild.exe. Set FREEAIR_MSBUILD to its full path, for example:
echo     set "FREEAIR_MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
popd
endlocal
exit /b 1

:failed
echo.
echo FAILED.
popd
endlocal
exit /b 1
