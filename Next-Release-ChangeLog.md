This is the second **Release Candidate** version: version number start 1.0 Release sooner.

CLI Commands and Options
========================
* `map.local.rule` command to setup web hooks in Azure DevOps.
* Automatic check if a new version of CLI is available.
* `AGGREGATOR_NEW_VERSION_CHECK_DISABLED` environment variable to disable upgrade check.


Docker and Azure Function Hosting
========================
* Docker hosting image for Windows and Linux.
* ASP.NET Core hosting.
* Telemetry to collect anonymized command usage.
* `AGGREGATOR_TELEMETRY_DISABLED` environment variable to disable telemetry.


Rule Language
========================
No changes.


Rule Interpreter Engine
========================
* Fixes #88: _An item with same key already been added_ error when settings the same field twice in a Rule.


Build, Test, Documentation
========================
* Updated to the latest version of NuGet packages.
* Merged GitHub Actions build and deploy scripts.
* Added Code Coverage in build script.
* Added SonarCloud scan in build script.
* Added Docker build and push to deploy script.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
