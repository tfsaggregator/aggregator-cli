# Build
Building locally requires
- Visual Studio 2017 15.8.9
- Azure Functions and Web Jobs Tools

# Debug

## Custom/development Aggregator runtime
In Visual Studio, `src\aggregator-function\Directory.Build.targets` will automatically package and copy the runtime needed by CLI.
You might have to change the version number in `src\aggregator-function\aggregator-manifest.ini` to force your local version.

You can also use the *Pack* right-click command on the `aggregator-function` project and make sure to copy the created zip into your CLI directory so it uploads the correct one when creating an instance.

## CLI
Set `aggregator-cli` as Start-up project
Use the Visual Studio Project properties to set the Command line arguments

## Runtime
Set `aggregator-function` as Start-up project
Use **Postman** or similar tool to send the request at http://localhost:7071/api/name_of_rule

# Integration tests
`git update-index --assume-unchanged src/integrationtests-cli/logon-data.json` and edit the file content 
