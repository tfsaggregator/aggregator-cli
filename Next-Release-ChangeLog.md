This release has a few fixes and a new feature.


CLI Commands and Options
========================
* Fixes upgrading a previous instance to the latest run-time (PR #263).
* New algorithm to generate unique Azure resource names.


Docker and Azure Function Hosting
========================
No changes.


Rule Language
========================
* New pre-defined constant `ruleName`, returns the name of executing rule.
* New `store.TransitionToState` method to change the state of a Work Item (see #255).


Rule Interpreter Engine
========================
No changes.


Build, Test, Documentation
========================
* Renamed `aggregator-cli.sln` to `aggregator3.sln`.
* Moved assembly properties, in particoular `VersionPrefix` and `VersionSuffix`, to common `Directory.Build.props` file.
* Added tests to upgrade from an older version.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
