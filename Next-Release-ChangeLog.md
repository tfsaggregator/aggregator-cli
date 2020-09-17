This is the inital **1.0** release.

CLI Commands and Options
========================
* Resolve code analysis warnings.
* Fix incorrect HTTP Header in web hook Subscriptions made by `map.local.rule`.
* Fix incorrect URL in web hook Subscriptions made by `map.local.rule`.


Docker and Azure Function Hosting
========================
* Resolve code analysis warnings.
* Fix CS1705 error.
* Support for `Aggregator_AzureDevOpsCertificate` when Azure DevOps is using a certificate issued by non-trusted Certification Authority (e.g.self-signed).
* Better handling of API keys.


Rule Language
========================
* New optional `events` directive.


Rule Interpreter Engine
========================
* Resolve code analysis warnings.
* Improved some messages.


Build, Test, Documentation
========================
* Updated to the latest version of NuGet packages except for Function SDK and Logging.
* Stick to `Microsoft.NET.Sdk.Functions` 3.0.3 until they resolve [#465](https://github.com/Azure/azure-functions-vs-build-sdk/issues/465). This locks `Microsoft.Extensions.Logging` to 3.1.6 too.
* Resolve code analysis warnings.
* Fix default branch reference in CI workflow.
* Push docker images to GitHub Container Registry (beta) in addition to Docker Hub.
* Local version number is now `0.0.1-localdev`.
* Fix build badge, added SonarQube badge.
* Trimmed `.dockerignore`.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
