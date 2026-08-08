@echo off
rem ---------------------------------------------------------------------------------------------
rem  Runs the integration tests, i.e. the ones which need a real embedding model.
rem
rem  Without an endpoint those tests skip themselves, which is why they need a script of their own:
rem  it names the server. The default is a local OpenAI compatible one on port 5001; override it
rem  from the environment or on the command line of the calling shell:
rem
rem      set "FREEAIR_TEST_EMBEDDING_ENDPOINT=http://localhost:1234/v1"
rem      set "FREEAIR_TEST_EMBEDDING_MODEL=text-embedding-qwen3-8b"
rem      set "FREEAIR_TEST_EMBEDDING_TOKEN=sk-..."
rem      run-integration-tests.bat
rem
rem  The model is optional: when it is not named, the first model the server lists is used, which is
rem  what a local single-model server wants. The token is optional too.
rem
rem  Anything on the command line is passed on to `dotnet test`.
rem ---------------------------------------------------------------------------------------------

setlocal

if not defined FREEAIR_TEST_EMBEDDING_ENDPOINT (
    set "FREEAIR_TEST_EMBEDDING_ENDPOINT=http://localhost:5001/v1"
)

echo Embedding endpoint: %FREEAIR_TEST_EMBEDDING_ENDPOINT%
if defined FREEAIR_TEST_EMBEDDING_MODEL echo Embedding model:    %FREEAIR_TEST_EMBEDDING_MODEL%
if not defined FREEAIR_TEST_EMBEDDING_MODEL echo Embedding model:    ^<the first one the server lists^>

rem The detailed console logger is what makes the tests print: the model list, the number of
rem dimensions and the similarities they measured are only shown at this verbosity, and those lines
rem are what tells a wrong port from a wrong model when the run fails.

call "%~dp0run-tests.bat" --filter "FullyQualifiedName~Integration" --logger "console;verbosity=detailed" %*
exit /b %errorlevel%
