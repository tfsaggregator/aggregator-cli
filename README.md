# aggregator-cli

[![Build Status](https://github.com/tfsaggregator/aggregator-cli/workflows/CI/badge.svg)](https://github.com/tfsaggregator/aggregator-cli/actions)
[![Release Status](https://github.com/tfsaggregator/aggregator-cli/workflows/release-to-GitHub/badge.svg)](https://github.com/tfsaggregator/aggregator-cli/actions)

This is the successor to renowned TFS Aggregator.
The current Server Plugin version (2.x) will be maintained to support TFS.
The Web Service flavour will be discontinued in favour of this new tool for two reasons:
- deployment and configuration of Web Service was too complex for most users;
- both the Plugin and the Service rely heavily on TFS Object Model which is [deprecated](https://docs.microsoft.com/en-us/azure/devops/integrate/concepts/wit-client-om-deprecation).

The main scenario for Aggregator (3.x) is supporting Azure DevOps and cloud scenario. In the future, we will work to support the on-premise scenario to permit replacement of the Server Plugin.

> **This is an early version (beta)**: we might change verbs and rule language before the final release!
*Note*: The documentation is limited to this page and the content of the `doc` folder.



## Major features

- use of new Azure DevOps REST API
- simple deployment via CLI tool
- Rule object model similar to v2



## Planned features (post v1.0)

- Support for Deployment Slots for blue/green-style deployments
- OAuth support to avoid maintain access tokens
- Additional Azure DevOps events and objects (e.g. Git)



## How it works

As the name implies, this is a command line tool: you download the latest CLI.zip from GitHub [releases](https://github.com/tfsaggregator/aggregator-cli/releases) and unzip on your client machine.
Read more below at the Usage section.

Through the CLI you create one or more Aggregator **Instance** in Azure. 
An Aggregator Instance is an Azure Function Application in its own Resource Group,
sharing the same Azure DevOps credential and version of Aggregator **Runtime**.
If the Resource Group does not exists, Aggregator will try to create it.
*Note*: The name you pick for the Instance must be **unique** amongst all
Aggregator Instances in Azure!
If you specify the Resource Group, you can have more than one Instance in the Resource Group.

After creating the Instance, you upload the code of Aggregator **Rules**.
A Rule is code that reacts to one or more Azure DevOps event.
Each Aggregator Rule becomes an Azure Function in the Aggregator instance i.e. the Azure Function Application.
The Rule language is C# (hopefully more in the future) and uses Aggregator Runtime and [Azure Functions Runtime](https://docs.microsoft.com/en-us/azure/azure-functions/functions-versions) 2.0
to do its work.
When you create an Instance, a Rule or update them, CLI checks GitHub Releases
to ensure that Aggregator Runtime is up-to-date or match the specified version.

An Aggregator **Mapping** is an Azure DevOps Service Hook triggered by a specific event. Currently we support only Work Item events.
When triggered the Azure DevOps Service Hook invokes a single Aggregator Rule i.e. the Azure Function hosting the Rule code. Azure DevOps saves the Azure Function Key in the Service Hook configuration.

You can deploy the same Rule in different Instances, map the same Azure DevOps event to many Rules or map multiple events to the same Rule: it is up to you choosing the best way to organize.



## Authentication

You must instruct Aggregator which credential to use.
To do this, run the `login.azure` and `login.ado` commands.

To create the credentials, you need an Azure Service Principal and a Azure DevOps Personal Access Token.

These documents will guide you in creating the credentials
* [Use portal to create an Azure Active Directory application and service principal that can access resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal)        
* [Create personal access tokens to authenticate access](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate).

Aggregator stores the logon credentials locally and expires them after 2 hours.

The PAT is also stored in the Azure Function settings: **whoever has access to the Resource Group can read it!**

The Service Principal must have Contributor permission to the Azure Subscription or, in alternative, pre-create the Resource Group in Azure and give the service account Contributor permission to the Resource Group.
![Permission on existing Resource Group](doc/images/contributor-on-rg.png)
If you go this route, remember add the `--resourceGroup` to all commands requiring an instance, otherwise the `instance` parameter adds an `aggregator-` prefix to find the Resource Group.



## Usage

Download and unzip the latest CLI.zip file from [Releases](https://github.com/tfsaggregator/aggregator-cli/releases).
It requires [.Net Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) installed on the machine.
To run Aggregator run `aggregator-cli` or `dotnet aggregator-cli.dll` followed by a verb and its options.

### Verbs

 Verb               | Use
--------------------|----------------------------------------
logon.azure         | Logon into Azure. This must be done before other verbs.
logon.ado           | Logon into Azure DevOps. This must be done before other verbs.
install.instance    | Creates a new Aggregator instance in Azure. 
add.rule            | Add a rule to existing Aggregator instance in Azure.
map.rule            | Maps an Aggregator Rule to existing Azure DevOps Projects, DevOps events are sent to the rule.
list.instances      | Lists Aggregator instances in the specified Azure Region or Resource Group or in the entire Subscription.
list.rules          | List the rules in an existing Aggregator instance in Azure.
list.mappings       | Lists mappings from existing Azure DevOps Projects to Aggregator Rules.
invoke.rule         | Executes a rule locally or in an existing Aggregator instance.
configure.instance  | Configures an existing Aggregator instance (currently the Azure DevOps authentication).
configure.rule      | Change a rule configuration (currently only enabling/disabling).
update.rule         | Update the code of a rule and/or its runtime.
unmap.rule          | Unmaps an Aggregator Rule from a Azure DevOps Project.
remove.rule         | Remove a rule from existing Aggregator instance in Azure, removing any mapping to the Rule.
uninstall.instance  | Destroy an Aggregator instance in Azure, removing any mapping to the Rules.
help                | Display more information on a specific command.
version             | Display version information.

You can see a few Command examples in [Sample Aggregator CLI usage](doc/command-examples.md).



## Rule language

See [Rule Language](doc/rule-language.md) for a list of objects and properties to use.
For examples see [Rule Examples](doc/rule-examples-basic.md).



## Maintenance

Aggregator stores the PAT in the Azure Function configuration. Before the PAT expire you should refresh it from Azure DevOps or save a new PAT using the `configure.instance` command.

Read [Production Configuration and Administration](doc/production.md) for recommendations on running Aggregator in production.


## Troubleshooting

Use the Application Insight instance that was created aside the Azure Function.
