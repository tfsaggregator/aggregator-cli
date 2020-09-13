This is the final **Release Candidate** version.

CLI Commands and Options
========================
* Resolve code analysis warnings.
* Fix incorrect HTTP Header in web hook Subscriptions made by `map.local.rule`.


Docker and Azure Function Hosting
========================
* Resolve code analysis warnings.
* Fix error CS1705


Rule Language
========================
* optional `events` directive


Rule Interpreter Engine
========================
* Resolve code analysis warnings.


Build, Test, Documentation
========================
* Updated to the latest version of NuGet packages except for Function SDK and Logging.
* Stick to `Microsoft.NET.Sdk.Functions` 3.0.3 until they resolve [#465](https://github.com/Azure/azure-functions-vs-build-sdk/issues/465). This locks `Microsoft.Extensions.Logging` to 3.1.6 too.
* Resolve code analysis warnings.
* Fix default branch reference in CI workflow.
* Push docker images to GitHub Container Registry (beta) in addition to Docker Hub.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
