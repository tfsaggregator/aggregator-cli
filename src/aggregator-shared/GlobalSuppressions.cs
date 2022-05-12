// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Info Code Smell", "S1135:Track uses of \"TODO\" tags", Justification = "Do not warn")]
[assembly: SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Do not warn")]
[assembly: SuppressMessage("Security", "CA5379:Ensure Key Derivation Function algorithm is sufficiently strong", Justification = "<Pending>", Scope = "member", Target = "~M:aggregator.SharedSecret.DeriveFromPassword(System.String)~System.String")]
