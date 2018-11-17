| Scenario 1                                                                                                    | Pass/Fail | Expected                                                             |
|---------------------------------------------------------------------------------------------------------------|:---------:|----------------------------------------------------------------------|
| logon.azure SUBSCRIPTION_OWNER                                                                                | 0.4.0     |                                                                      |
| logon.ado ACCOUNT_ADMIN                                                                                       | 0.4.0     |                                                                      |
| install.instance --name my1 --location westeurope                                                             | 0.4.0     | Creates Resource Group and Function App                              |
| list.instances                                                                                                | 0.4.0     |                                                                      |
| list.instances --resourceGroup aggregator-my1                                                                 | 0.4.0     |                                                                      |
| add.rule --instance my1 --name test1 --file test\test1.rule                                                   | 0.4.0     | Add Function                                                         |
| add.rule --instance my1 --name test2 --file test\test2.rule                                                   | 0.4.0     | Add Function                                                         |
| list.rules --instance my1                                                                                     | 0.4.0     |                                                                      |
| map.rule --project SampleProject --event workitem.created --instance my1 --rule test1                         | 0.4.0     | Add Subscription to AzDO                                             |
| map.rule --project SampleProject --event workitem.updated --instance my1 --rule test2                         | 0.4.0     | Add Subscription to AzDO                                             |
| list.mappings --instance my1                                                                                  | 0.4.0     |                                                                      |
| list.mappings --project SampleProject                                                                         | 0.4.0     |                                                                      |
| list.mappings --instance my1 --project SampleProject                                                          | 0.4.0     |                                                                      |
| configure.rule --instance my1 --name test1 --disable                                                          | 0.4.0     | Function is disabled                                                 |
| configure.rule --instance my1 --name test1 --enable                                                           | 0.4.0     | Function is enabled                                                  |
| update.rule --instance my1 --name test1 --file test\test1.rule --requiredVersion 99.99.99                     | 0.4.0     | Must fail because version does not exists                            |
| update.rule --instance my1 --name test1 --file test\test1.rule --requiredVersion 0.3.3                        | 0.4.0     | Uploads only the rule                                                |
| configure.instance --name my1 --location westeurope --authentication                                          | 0.4.0     |                                                                      |
| unmap.rule --event workitem.created --instance my1 --rule test1                                               | 0.4.0     | Subscription removed                                                 |
| unmap.rule --event workitem.updated --project SampleProject --instance my1 --rule test2                       | 0.4.0     | Subscription removed                                                 |
| remove.rule --instance my1 --name test1                                                                       | 0.4.0     | Function removed                                                     |
| uninstall.instance --name my1 --location westeurope --dont-remove-mappings                                    | 0.4.0     | Remove the entire Resource Group leaving the Subscriptions in AzDO   |

| Scenario 2                                                                                                    | Pass/Fail | Expected                                                             |
|---------------------------------------------------------------------------------------------------------------|:---------:|----------------------------------------------------------------------|
| logon.azure RG_CONTRIBUTOR                                                                                    | 0.4.0     |                                                                      |
| logon.ado ACCOUNT_ADMIN                                                                                       | 0.4.0     |                                                                      |
| install.instance --name my3 --resourceGroup myRG1 --location westeurope --requiredVersion latest              | 0.4.0     | Add Function App to existing Resource Group with most recent runtime |
| list.instances --resourceGroup myRG1                                                                          | 0.4.0     |                                                                      |
| add.rule --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule                             | 0.4.0     | Add Function                                                         |
| list.rules --instance my3 --resourceGroup myRG1                                                               | 0.4.0     |                                                                      |
| map.rule --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3   | 0.4.0     | Add Subscription to AzDO                                             |
| list.mappings --instance my3 --resourceGroup myRG1                                                            | 0.4.0     |                                                                      |
| list.mappings --project SampleProject                                                                         | 0.4.0     |                                                                      |
| list.mappings --instance my3 --resourceGroup myRG1 --project SampleProject                                    | 0.4.0     |                                                                      |
| configure.rule --instance my3 --resourceGroup myRG1 --name test3 --disable                                    | 0.4.0     | Function is disabled                                                 |
| configure.rule --instance my3 --resourceGroup myRG1 --name test3 --enable                                     | 0.4.0     | Function is enabled                                                  |
| update.rule --instance my3 --resourceGroup myRG1 --name test3 --file test\test3b.rule                         | 0.4.0     | Function code has additional line                                    |
| configure.instance --name my3 --resourceGroup myRG1 --location westeurope --authentication                    | 0.4.0     |                                                                      |
| unmap.rule --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3 | 0.4.0     | Subscription removed                                                 |
| remove.rule --instance my3 --resourceGroup myRG1 --name test3                                                 | 0.4.0     | Function removed                                                     |
| uninstall.instance --name my3 --resourceGroup myRG1 --location westeurope                                     | 0.4.0     | Function app removed                                                 |

| Scenario 3                                                                                                    | Pass/Fail | Expected                                                             |
|---------------------------------------------------------------------------------------------------------------|:---------:|----------------------------------------------------------------------|
| logon.azure RG_CONTRIBUTOR                                                                                    | 0.4.0     |                                                                      |
| logon.ado ACCOUNT_ADMIN                                                                                       | 0.4.0     |                                                                      |
| install.instance --name my3 --resourceGroup myRG1 --location westeurope                                       | 0.4.0     | Add Function App to existing Resource Group                          |
| list.instances --resourceGroup myRG1                                                                          | 0.4.0     |                                                                      |
| add.rule --instance my3 --resourceGroup myRG1 --name test3 --file test\test3.rule                             | 0.4.0     | Add Function                                                         |
| list.rules --instance my3 --resourceGroup myRG1                                                               | 0.4.0     |                                                                      |
| map.rule --project SampleProject --event workitem.created --instance my3 --resourceGroup myRG1 --rule test3   | 0.4.0     | Add Subscription to AzDO                                             |
| list.mappings --instance my3 --resourceGroup myRG1                                                            | 0.4.0     |                                                                      |
| uninstall.instance --name my3 --resourceGroup myRG1 --location westeurope                                     | 0.4.0     | Removes the Function and the Subscriptions                           |