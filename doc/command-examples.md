# Sample Aggregator CLI usage

In the following you will find many sample usage of Aggregator CLI commands.
Run `dotnet aggregator-cli.dll` followed by the command and any option.
All commands accept the `--verbose` option to print additional messages, useful in troubleshooting.


### Logon
This is a required step, credentials are cached locally and expire after 2 hours.
```
logon.azure --subscription ************ --client ************ --password *********** --tenant ************
logon.ado --url https://dev.azure.com/youraccount --mode PAT --token ***************************************q
```
Clearly you have to use valid values instead of asterisks (`*`).


### Create an Azure Function Application
You need an Azure Function Application plus the Aggregator runtime to execute the Rules.
These commands will do everything for you.
```
install.instance --verbose --name my1 --location westeurope
install.instance --name my3 --resourceGroup myRG1 --location westeurope --requiredVersion latest
```
Remember that the Instance name must be unique in Azure (CLI automatically append `aggregator` suffix to minimize the chance of a clash).
You can specify the version of Aggregator Runtime using the `requiredVersion` option.
Look in https://github.com/tfsaggregator/aggregator-cli/releases for valid version numbers.


### Search Aggregator instances
That is Azure Functions previously created via the CLI.

This command searches in the entire Azure subscription defined at logon.
```
list.instances
```

To scope search in an Azure Resource Group use
```
list.instances --resourceGroup myRG1
```


### Add Azure Functions (i.e. Aggregator Rules) to an existing Azure Function Application (i.e. an Aggregator Instance)
Creates two rules
```
add.rule --verbose --instance my1 --name test1 --file test\test1.rule
add.rule --verbose --instance my1 --name test2 --file test\test2.rule
list.rules --verbose --instance my1
```
`list.rules` shows which Rules (i.e. Azure Function) are deployed in an Instance (i.e. Azure Function Application).

Creates an Aggregator Rule in specified App and Resource Group
```
add.rule --verbose --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule
```


### Adds two Service Hook to Azure DevOps, each invoking a different rule
This is the last step: gluing Azure DevOps to the Rule hosted in Azure Functions

```
map.rule --verbose --project SampleProject --event workitem.created --instance my1 --rule test1
map.rule --verbose --project SampleProject --event workitem.updated --instance my1 --rule test2
map.rule --verbose --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3
```
Currently only these events are supported:<br/>
`workitem.created`<br/>
`workitem.updated`<br/>
`workitem.deleted`<br/>
`workitem.restored`<br/>
`workitem.commented`<br/>
The same rule can be triggered by multiple Events from different Azure DevOps Projects.

List the mappings with various filter options.
```
list.mappings --verbose --instance my1
list.mappings --verbose --project SampleProject
list.mappings --instance my1 --project SampleProject
```


### Disable and enable rules
Disabling a broken rule leaves any Mappings in place.
```
configure.rule --verbose --instance my1 --name test1 --disable
configure.rule --verbose --instance my1 --name test1 --enable
```


### Update the code and runtime of a rule
This command updates the code and potentially the Aggregator runtime

```
update.rule --verbose --instance my1 --name test1 --file test1.rule --requiredVersion 0.4.0
update.rule --verbose --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule
```
You can fix the Aggregator runtime using the `requiredVersion` option.


### Updates the Azure DevOps credential stored in Azure Function
The Azure Function configuration saves the credential to connect back to Azure DevOps.

```
configure.instance --name my1 --location westeurope --authentication
configure.instance --name my3 --resourceGroup myRG1 --location westeurope --authentication
```
Note that a Personal Access Token (PAT) has a limited duration and must be periodically replaced to ensure service.


### Remove a Service Hook from Azure DevOps
Run these command, Azure DevOps stops sending the notification to the Rule.

```
unmap.rule --verbose --event workitem.created --instance my1 --rule test1
unmap.rule --verbose --event workitem.updated --project SampleProject --instance my1 --rule test2
unmap.rule --verbose --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3
```
The first example removes all subscriptions sending `workitem.created` notifications to *test1* rule.
Note the options to filter for a specific Azure DevOps project.


### Deletes an Azure Function
It also removes all Service Hooks referring to the Rule.

```
remove.rule --verbose --instance my1 --name test1
remove.rule --verbose --instance my3 --resourceGroup myRG1 --name test3
```

The `dont-remove-mappings` delete the Azure Function Application leaving the Service Hooks in place
```
uninstall.instance --name my1 --location westeurope --dont-remove-mappings
```


### Delete the Azure Function Application and any Service Hooks referring to it
Deletes the Instance and all the Rules in it.
```
uninstall.instance --verbose --name my3 --resourceGroup myRG1 --location westeurope
```


### Stream
It connects for 30 minutes to the Azure Application and prints the logging messages.
```
stream.logs --instance my7 --resourceGroup test-aggregator7 --verbose
```
You can stop the program using `Ctrl+C` keystroke or closing the command window.


### Trigger rules by faking the Azure DevOps event
Runs a rule code locally, no change is sent to Azure DevOps
```
invoke.rule --dryrun --project SampleProject --event workitem.created --workItemId 14 --local --source test\test2.rule
```

Runs existing rule in Azure, no change is sent to Azure DevOps thanks to the `dryrun` option
```
invoke.rule --instance my7 --resourceGroup test-aggregator7  --name r1 --event workitem.created --account giuliovaad --project WorkItemTracking --workItemId 14 --verbose --dryrun
```
If you want to see the log messages, run a `stream.logs` command in another window.
This can be used (without the `dryrun` flag) to apply rules to existing work items.