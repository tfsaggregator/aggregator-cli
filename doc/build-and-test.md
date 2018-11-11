# Build
Visual Studio 2017 15.8.9
Azure Functions and Web Jobs Tools

# Debug

## CLI
Set `aggregator-cli` as Start-up project
Use the project properties to set the Command line arguments

## Runtime
Set `aggregator-function` as Start-up project
Use **Postman** or similar tool to send the request at http://localhost:7071/api/name_of_rule

# Integration tests
`git update-index --assume-unchanged src/integrationtests-cli/logon-data.json` and edit the file content 
