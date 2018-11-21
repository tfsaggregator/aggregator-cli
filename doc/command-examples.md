# Sample Aggregator CLI usage

Remember that the Instance name must be unique in Azure.

```
# logon
logon.azure --subscription 9c********08 --client 5a********b6 --password P@assword1 --tenant 3c********1d
logon.ado --url https://someaccount.visualstudio.com --mode PAT --token 2**************************************q

# create an Azure Function Application
install.instance --verbose --name myRG1 --location westeurope
install.instance --name my3 --resourceGroup myRG1 --location westeurope --requiredVersion latest
# search instances in the Azure subscription
list.instances
# search instances in the Azure Resource Group
list.instances --resourceGroup myRG1

# create two Azure Functions
add.rule --verbose --instance my1 --name test1 --file test\test1.rule
add.rule --verbose --instance my1 --name test2 --file test\test2.rule
list.rules --verbose --instance my1

# create Azure Function in specified App and Resource Group
add.rule --verbose --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule

# adds two Service Hook to Azure DevOps, each invoking a different rule
map.rule --verbose --project SampleProject --event workitem.created --instance my1 --rule test1
map.rule --verbose --project SampleProject --event workitem.updated --instance my1 --rule test2
map.rule --verbose --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3


list.mappings --verbose --instance my1
list.mappings --verbose --project SampleProject
list.mappings --instance my1 --project SampleProject

# disable an existing rule
configure.rule --verbose --instance my1 --name test1 --disable
# re-enable a rule
configure.rule --verbose --instance my1 --name test1 --enable
# update the code and runtime of a rule
update.rule --verbose --instance my1 --name test1 --file test1.rule --requiredVersion 0.4.0
update.rule --verbose --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule

# updates the Azure DevOps credential stored in Azure Function and used by rules to connect back
configure.instance --name my1 --location westeurope --authentication
configure.instance --name my3 --resourceGroup myRG1 --location westeurope --authentication

# remove a Service Hook from Azure DevOps
unmap.rule --verbose --event workitem.created --instance my1 --rule test1
unmap.rule --verbose --event workitem.updated --project SampleProject --instance my1 --rule test2
unmap.rule --verbose --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3

# deletes an Azure Function and all Service Hooks referring to it
remove.rule --verbose --instance my1 --name test1
remove.rule --verbose --instance my3 --resourceGroup myRG1 --name test3

# delete the Azure Function Application leaving the Service Hooks in place
uninstall.instance --name my1 --location westeurope --dont-remove-mappings
# delete the Azure Function Application and any Service Hooks referring to it
uninstall.instance --verbose --name my3 --resourceGroup myRG1 --location westeurope

# run rule locally, no change is sent to Azure DevOps
invoke.rule --dryrun --project SampleProject --event workitem.created --workItemId 14 --local --source test\test2.rule
```
