# aggregator-cli

[![Build Status](https://github.com/tfsaggregator/aggregator-cli/workflows/CI/badge.svg?branch=master)](https://github.com/tfsaggregator/aggregator-cli/actions)
[![Release Status](https://github.com/tfsaggregator/aggregator-cli/workflows/release-to-GitHub/badge.svg?branch=master)](https://github.com/tfsaggregator/aggregator-cli/actions)

This is the successor to renowned TFS Aggregator.
The current Server Plugin version (2.x) will be maintained to support TFS.
The Web Service flavour will be discontinued in favour of this new tool for two reasons:
- deployment and configuration of Web Service was too complex for most users;
- both the Plugin and the Service rely heavily on TFS Object Model which is [deprecated](https://docs.microsoft.com/en-us/azure/devops/integrate/concepts/wit-client-om-deprecation).

The main scenario for Aggregator (3.x) is supporting Azure DevOps and cloud scenario. In the future, we might work to support the on-premise scenario to permit replacement of the Server Plugin.

> *Note*: This README is a synopsis of the documentation available at <https://tfsaggregator.github.io/docs/v3/>.

You might also find useful Richard Fennell's post [Getting started with Aggregator CLI for Azure DevOps Work Item Roll-up](https://blogs.blackmarble.co.uk/rfennell/2020/06/12/getting-started-with-aggregator-cli-for-azure-devops-work-item-roll-up/).


## Major features

- use of new Azure DevOps REST API
- simple deployment via CLI tool
- Rule object model similar to Aggregator v2


### Planned features (post v1.0)

- Support for Deployment Slots for blue/green-style deployments
- OAuth support to avoid maintain access tokens
- Additional Azure DevOps events and objects (e.g. Git)


## How it works

As the name implies, this is a command line tool: you download the latest CLI.zip from GitHub [releases](https://github.com/tfsaggregator/aggregator-cli/releases) and unzip on your client machine.
Read more below in the [Usage](#usage) section.

Through the CLI you create one or more Azure Functions in your Subscription. The Functions use the Aggregator **Runtime** to run your **Rules**.
A Rule is code that reacts to one or more Azure DevOps event.
The Rule language is only C#, currently.
When you create an Instance, a Rule or update them, CLI checks GitHub Releases
to ensure that Aggregator Runtime is up-to-date. You can specify a different Runtime version or point to a specific Runtime package, e.g. on a network share.

An Aggregator **Mapping** is an Azure DevOps Service Hook triggered by a specific event. Currently we support only Work Item events.
When triggered the Azure DevOps Service Hook invokes a single Aggregator Rule i.e. the Azure Function hosting the Rule code. Azure DevOps saves the Azure Function Key in the Service Hook configuration.

You can deploy the same Rule in different Instances, map the same Azure DevOps event to many Rules or map multiple events to the same Rule: it is up to you choosing the best way to organize.


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

See [Commands](https://tfsaggregator.github.io/docs/v3/commands/) for further details on verbs and options.


## Rule language

See [Rule Language](https://tfsaggregator.github.io/docs/v3/rules/) for a list of objects and properties to use.
For examples see [Rule Examples](https://tfsaggregator.github.io/docs/v3/rules/rule-examples-basic/).


## Maintenance

Aggregator stores the PAT in the Azure Function configuration. Before the PAT expire you should refresh it from Azure DevOps or save a new PAT using the `configure.instance` command.

Read [Production Configuration and Administration](https://tfsaggregator.github.io/docs/v3/setup/production/) for recommendations on running Aggregator in production.


## Contributing

Use the Application Insight instance that was created aside the Azure Function.
Details on building your own version and testing are in the [Contribute](https://tfsaggregator.github.io/docs/v3/contrib/) section.
