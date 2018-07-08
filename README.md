# aggregator-cli

## Underlying objects

An Aggregator Instance is an Azure Function Application in its own Resource Group

An Aggregator Rule is an Azure Function in the above instance.

A Aggregator Rule Mapping is a VSTS Service Hook Subscription that invokes the above Function.

## Authentication

See [Use portal to create an Azure Active Directory application and service principal that can access resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal) and [Create personal access tokens to authenticate access](https://docs.microsoft.com/en-us/vsts/git/_shared/personal-access-tokens?view=vsts).

## Open Issues
We can have only one pre-compiled Function per Application unless there 
is a way to change the `FunctionNameAttribute` and regenerate the package.
The alternative is to use `.csx`.