Fixes version.

CLI Commands and Options
========================
- Removed annoying messages about new version check.
- Deprecation warning on missing `--resourceGroup` option.
- Changed `IDataProtectionProvider.CreateProtector` purpose string to prevent clashes.


Docker and Azure Function Hosting
========================
- Retry after Http 429 using Polly, address #71.


Rule Language
========================
No changes.


Rule Interpreter Engine
========================
- address #185 by removing field when set to null value.


Build, Test, Documentation
========================
- Terraform and PowerShell scripts to setup a dev VM.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
