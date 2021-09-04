This release has a few fixes and a new feature.


CLI Commands and Options
========================
New `--filterOnlyLinks` option to `map.rule` command; coincide with the _Links are added or removed_ filter of the Azure DevOps subscription (#244).


Docker and Azure Function Hosting
========================
Fix an off-by-one error when getting unique name from the string (#243).
Fix invalid cast exception when the `IdentityRef` could not be parsed (#243).


Rule Language
========================
No changes.


Rule Interpreter Engine
========================
Improved performance and memory profile through caching compiled rules (#242).


Build, Test, Documentation
========================
Updated NuGet dependencies.


File Hashes
------------------------

SHA-256 Hash Value                                               |  File
-----------------------------------------------------------------|-------------------------------
