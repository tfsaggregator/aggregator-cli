# Sample Aggregator CLI usage

Run `aggregator-cli` or `dotnet aggregator-cli.dll` followed by the command and any option.
All commands accept the `--verbose` option to print additional messages for troubleshooting.


### Logon
You are required to log into both Azure and ADO. The credentials are cached locally and expire after 2 hours. _(Replace the below asterisks `*` with valid values.)_
```Batchfile
logon.azure --subscription ************ --client ************ --password *********** --tenant ************
logon.ado --url https://dev.azure.com/youraccount --mode PAT --token ***************************************
```


### Create an Azure Function Application
You need an Azure Function Application plus the Aggregator runtime to execute rules. Both are created by a single call to `install.instance`.

Create a new instance -- and a new resource group -- in the West Europe region.
```Batchfile
install.instance --verbose --name my1 --location westeurope
```

Create a new instance -- and a new resource group named myRG1 or re-use existing -- in the West Europe region.
```Batchfile
install.instance --name my3 --resourceGroup myRG1 --location westeurope --requiredVersion latest
```

- The Aggregator instance name must be unique in Azure (CLI automatically appends `aggregator` suffix to minimize the chance of a clash).
- You can specify the version of Aggregator Runtime using the `requiredVersion` option. Look in [our releases](https://github.com/tfsaggregator/aggregator-cli/releases) for valid version numbers.
- You can use the [Azure CLI](https://github.com/Azure/azure-cli) to get a list of regions: `az account list-locations -o table`


### Search Aggregator instances
That is Azure Functions previously created via the CLI.

Search the entire Azure subscription (previously connected to via logon.azure):
```Batchfile
list.instances
```

Scope search to a particular resource group:
```Batchfile
list.instances --resourceGroup myRG1
```


### Add Azure Functions (i.e. Aggregator Rules) to an existing Azure Function Application (i.e. an Aggregator Instance)
Creates two rules where the file parameter is a local file relative to the working directory.
```Batchfile
add.rule --verbose --instance my1 --name test1 --file test\test1.rule
add.rule --verbose --instance my1 --name test2 --file test\test2.rule
list.rules --verbose --instance my1
```
`list.rules` shows which Rules (i.e. Azure Function) are deployed in an Instance (i.e. Azure Function Application).

Creates an Aggregator Rule in specified App and Resource Group
```Batchfile
add.rule --verbose --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule
```


### Adds two service hooks to Azure DevOps, each invoking a different rule
This is the last step: gluing Azure DevOps to the Rule hosted in Azure Functions

```Batchfile
map.rule --verbose --project SampleProject --event workitem.created --instance my1 --rule test1
map.rule --verbose --project SampleProject --event workitem.updated --instance my1 --rule test2
map.rule --verbose --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3
```
The same rule can be triggered by multiple events from different Azure DevOps projects. Currently only these events are supported:  
`workitem.created`  
`workitem.updated`  
`workitem.deleted`  
`workitem.restored`  
`workitem.commented`  

List the mappings with various filter options.
```Batchfile
list.mappings --verbose --instance my1
list.mappings --verbose --project SampleProject
list.mappings --instance my1 --project SampleProject
```


### Disable and enable rules
Disabling a broken rule leaves any mappings in place.
```Batchfile
configure.rule --verbose --instance my1 --name test1 --disable
configure.rule --verbose --instance my1 --name test1 --enable
```


### Update the code and runtime of a rule
This command updates the code and potentially the Aggregator runtime

```Batchfile
update.rule --verbose --instance my1 --name test1 --file test1.rule --requiredVersion 0.4.0
update.rule --verbose --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule
```
You can fix the Aggregator runtime using the `requiredVersion` option.


### Updates the Azure DevOps credential stored in Azure Function
The Azure Function configuration saves the credential to connect back to Azure DevOps.

```Batchfile
configure.instance --name my1 --location westeurope --authentication
configure.instance --name my3 --resourceGroup myRG1 --location westeurope --authentication
```
Note that a Personal Access Token (PAT) has a limited duration and must be periodically replaced to ensure service.


### Remove a Service Hook from Azure DevOps
Run these command, Azure DevOps stops sending the notification to the Rule.

```Batchfile
unmap.rule --verbose --event workitem.created --instance my1 --rule test1
unmap.rule --verbose --event workitem.updated --project SampleProject --instance my1 --rule test2
unmap.rule --verbose --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3
```
The first example removes all subscriptions sending `workitem.created` notifications to *test1* rule.
Note the options to filter for a specific Azure DevOps project.


### Delete Azure Function (Rule)
Delete rule and remove all service hooks referring to it.
```Batchfile
remove.rule --verbose --instance my1 --name test1
```

Delete rule from a specific resource group and remove all service hooks referring to it.
```Batchfile
remove.rule --verbose --instance my3 --resourceGroup myRG1 --name test3
```

Delete rule but leave the service hooks in place.
```Batchfile
uninstall.instance --name my1 --location westeurope --dont-remove-mappings
```


### Delete the Azure Function Application and any Service Hooks referring to it
Delete entire Aggregator instance and all its rules.
```
uninstall.instance --verbose --name my3 --resourceGroup myRG1 --location westeurope
```


### Stream
It connects for 30 minutes to the Azure Application and prints the logging messages.
```Batchfile
stream.logs --instance my7 --resourceGroup test-aggregator7 --verbose
```
You can stop the program using `Ctrl+C` keystroke or closing the command window.


### Trigger rules by faking the Azure DevOps event
Runs a rule code locally, no change is sent to Azure DevOps
```Batchfile
invoke.rule --dryrun --project SampleProject --event workitem.created --workItemId 14 --local --source test\test2.rule
```

Runs existing rule in Azure, no change is sent to Azure DevOps thanks to the `dryrun` option
```Batchfile
invoke.rule --instance my7 --resourceGroup test-aggregator7  --name r1 --event workitem.created --account giuliovaad --project WorkItemTracking --workItemId 14 --verbose --dryrun
```
If you want to see the log messages, run a `stream.logs` command in another window.
This can be used (without the `dryrun` flag) to apply rules to existing work items.