# Aggregator CLI

This is the successor to renowned TFS Aggregator.
The current Server Plugin version (2.x) will be maintained to support TFS.
The Web Service flavour will be discontinued in favour of this new tool for two reasons:
- deployment and configuration of Web Service was too complex for most users;
- both the Plugin and the Service rely heavily on TFS Object Model which is [deprecated](https://docs.microsoft.com/en-us/azure/devops/integrate/concepts/wit-client-om-deprecation).

The main scenario for Aggregator (3.x) is to support Azure DevOps Services and Azure. In the future, we might support the on-premise scenario to permit replacement of the Server Plugin.



## Major features

- use of new Azure DevOps REST API
- simple deployment via CLI tool
- Rule object model similar to TFS Aggregator v2



## Requirements

- an Azure DevOps Services Project
- a Personal Access Token with sufficient permissions on the Project
- an Azure Subscription
- a Service Principal with, at least, Contributor permission on a Resource Group of the Subscription
- basic knowledge of C# language, Azure Boards and Azure


## How it works

As the name implies, this is a command line tool: you download the latest aggregator-cli*.zip appropriate for your platform from GitHub [releases](https://github.com/tfsaggregator/aggregator-cli/releases) and unzip it on your client machine.
Read more below in the [Usage](#usage) section.

Through the CLI you create one or more Azure Functions in your Subscription. The Functions use a library named Aggregator **Runtime** to run your **Rules**.
A Rule is code that reacts to one or more Azure DevOps event; currently, the only language for writing rules is C#.

The CLI automatically checks GitHub Releases to ensure that you use the more recent version of Aggregator Runtime available. To avoid automatic upgrades, specify the Runtime version or point to a specific Runtime package file, using an `http:` or `file:` URL.

After you setup the Rules in Azure, you must add at least one **Mapping**. A mapping is an Azure DevOps Service Hook that send a message to the Azure Function when a specific event occurs. Currently we support only Work Item events.
When triggered, the Azure DevOps Service Hook invokes a single Aggregator Rule i.e. the Azure Function hosting the Rule code. Azure DevOps saves the Azure Function Key in the Service Hook configuration.

You can deploy the same Rule in different Instances, map the same Azure DevOps event to many Rules or map multiple events to the same Rule: you choose the best way to organize your code.



## Authentication

You must instruct Aggregator which credential to use.
To do this, run the `login.azure` and `login.ado` commands.

To create the credentials, you need an Azure Service Principal and a Azure DevOps Personal Access Token. Full details in the [Setup](https://tfsaggregator.github.io/docs/v3/setup/) section.

Aggregator stores the logon credentials locally and expires them after 2 hours.

The PAT is also stored in the Azure Function settings: **whoever has access to the Resource Group can read it!**

The Service Principal must have Contributor permission to the Azure Subscription or, in alternative, pre-create the Resource Group in Azure and give the service account Contributor permission to the Resource Group.
![Permission on existing Resource Group](https://tfsaggregator.github.io/docs/v3/setup/contributor-on-rg.png)
If you go this route, remember add the `--resourceGroup` to all commands requiring an instance.



## Usage

Download and unzip the latest CLI.zip file from [Releases](https://github.com/tfsaggregator/aggregator-cli/releases).
It requires [.Net Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) installed on the machine.
To run Aggregator run `aggregator-cli.exe` (Windows), `aggregator-cli` (Linux) or `dotnet aggregator-cli.dll` followed by a verb and its options.

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

See [Commands](https://tfsaggregator.github.io/docs/v3/commands/) for further details.



## Rule language

See [Rule Language](https://tfsaggregator.github.io/docs/v3/rules/) for a list of objects and properties to use.
For examples see [Rule Examples](https://tfsaggregator.github.io/docs/v3/rules/rule-examples-basic/).



## Maintenance

Aggregator stores the PAT in the Azure Function configuration. Before the PAT expire you should refresh it from Azure DevOps or save a new PAT using the `configure.instance` command.

Read [Production Configuration and Administration](https://tfsaggregator.github.io/docs/v3/setup/production/) for recommendations on running Aggregator in production.


## Troubleshooting

Use the Application Insight instance that was created aside the Azure Function.
Details on building your own version and testing are in the [Contribute](https://tfsaggregator.github.io/docs/v3/contrib/) section.
