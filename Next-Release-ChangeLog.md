This release fixes a number of issues and introduce an additional CLI command.


CLI Commands and Options
========================
- Removed annoying messages about new version check.
- Deprecation warning on missing `--resourceGroup` option.
- Changed `IDataProtectionProvider.CreateProtector` purpose string to prevent clashes.
- New `update.mappings` command.


Docker and Azure Function Hosting
========================
- Retry after Http 429 using Polly, address #71.
- Harden Azure resources (see #225).


Rule Language
========================
No changes.


Rule Interpreter Engine
========================
- Address #185 by removing field when set to null value.
- Impersonation does not work when a rule updates the same work item bug (#206).


Build, Test, Documentation
========================
- Terraform and PowerShell scripts to setup a dev VM.
- Use latest GitVersion.
- Drop log streaming in favour of reading the application log file: integration tests should become more reliable.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
