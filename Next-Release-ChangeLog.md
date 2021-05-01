This release fixes a number of issues and introduce an additional CLI command.


CLI Commands and Options
========================
TODO 
```
I am seeing the following odd message on running any command, however:

A new version (v1.0.1) of Aggregator CLI is available, please upgrade.
aggregator-cli v1.0.1 (build: 1.0.1.0 Release) (c) Copyright © TFS Aggregator Team

As you can see, the CLI identifies itself as 1.0.1, but still says I should upgrade to 1.0.1.
```


Docker and Azure Function Hosting
========================
No changes.


Rule Language
========================
- Added support for `.bypassrules` directive (#83, #228).


Rule Interpreter Engine
========================
- Fixes #229 (Updating a work item field with impersonation enabled fails with the message: _Remove requires Value to be null_)
- Fixes #234 (_Object reference not set_ when removing a work item link).


Build, Test, Documentation
========================
- SonarCloud now requires Java 11.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
