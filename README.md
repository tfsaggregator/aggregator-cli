# aggregator-cli

This is the successor to TFS Aggregator.

The main scenario will be support for VSTS.

## Underlying objects

An Aggregator Instance is an Azure Function Application in its own Resource Group,
sharing the same VSTS token.

An Aggregator Rule is an Azure Function in the above instance.

An Aggregator Rule Mapping is a VSTS Service Hook Subscription that invokes the above Function.

## Authentication

See [Use portal to create an Azure Active Directory application and service principal that can access resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal) and [Create personal access tokens to authenticate access](https://docs.microsoft.com/en-us/vsts/git/_shared/personal-access-tokens?view=vsts).


## Open Issues

### Rules as Functions
Pre-compiled Function cannot dynamically add endpoints/functions.
So we create a new `.csx` Function and upload a package with common code when App is created.

### Authentication
Pushing PAT token in the App configuration works, but creates maintenance issues.

### Security
Logon credentials are stored locally indefinitely: add a timeout.

### Code quality
Is is not robust, lacks logging and is neither well factored.

### Build complexity
The `install.instance` command assumes `function-bin.zip` is present in the same folder.
That file is created by _Publishing_ the `aggregator-function` project.

### Support for HA
Slots and Availability zones.
List Outbound IP Addresses `azure.AppServices.WebApps.GetByResourceGroup(instance.ResourceGroupName,instance.FunctionAppName).OutboundIPAddresses`
