This release fixes a number of issues and introduce an additional CLI command.


CLI Commands and Options
========================
- Fixes spurious upgrade message noted in #225.


Docker and Azure Function Hosting
========================
No changes.


Rule Language
========================
- Added support for `.bypassrules` directive (#83, #228).
- Fixes #231 (Directive check revision false missing after `update.rule`).


Rule Interpreter Engine
========================
- Fixes #229 (Updating a work item field with impersonation enabled fails with the message: _Remove requires Value to be null_).
- Fixes #234 (_Object reference not set_ when removing a work item link).


Build, Test, Documentation
========================
- SonarCloud now requires Java 11.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
