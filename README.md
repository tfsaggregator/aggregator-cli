# aggregator-cli

## Underlying objects

An Aggregator Instance is an Azure Function Apps in its own Resource Group
An Aggregator Rule is an Azure Function in the above instance.
A Aggregator Rule Mapping is a VSTS Service Hook Subscription that invokes the above Function.

## Authentication

See [Use portal to create an Azure Active Directory application and service principal that can access resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal) and [Create personal access tokens to authenticate access](https://docs.microsoft.com/en-us/vsts/git/_shared/personal-access-tokens?view=vsts).

