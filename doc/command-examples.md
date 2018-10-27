# Sample Aggregator CLI usage

Remember that the Instance name must be unique in Azure.

```
# logon
logon.azure --subscription 9c********08 --client 5a********b6 --password P@assword1 --tenant 3c********1d
logon.ado --url https://someaccount.visualstudio.com --mode PAT --token 2**************************************q

# create an Azure Function Application
install.instance --verbose --name my1 --location westeurope
list.instances

# create three Azure Functions
add.rule --verbose --instance my1 --name test1 --file test\test1.rule
add.rule --verbose --instance my1 --name test2 --file test\test2.rule
add.rule --verbose --instance my1 --name test3 --file test\test3.rule
list.rules --verbose --instance my1

# adds two Service Hook to Azure DevOps, each invoking a different rule
map.rule --verbose --project SampleProject --event workitem.created --instance my1 --rule test1
map.rule --verbose --project SampleProject --event workitem.updated --instance my1 --rule test2
list.mappings --verbose --instance my1

# disable a rule
configure.rule --verbose --instance my1 --name test1 --disable
# re-enable a rule
configure.rule --verbose --instance my1 --name test1 --enable
# update the code of a rule
configure.rule --verbose --instance my1 --name test --update test.rule

# updates the Azure DevOps credential stored by the rules
configure.instance --authentication

# remove a Service Hook from Azure DevOps
unmap.rule --verbose --event workitem.created --instance my1 --rule test1

# deletes two Azure Functions
remove.rule --verbose --instance my1 --name test1
remove.rule --verbose --instance my1 --name test2

# delete the Azure Function Application
uninstall.instance --verbose --name my1 --location westeurope
```