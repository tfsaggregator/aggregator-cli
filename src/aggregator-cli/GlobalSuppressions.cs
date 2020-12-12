// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Critical Code Smell", "S3966:Objects should not be disposed more than once", Justification = "We dispose only BCL objects and we trust how they implemented Dispose()")]
[assembly: SuppressMessage("Info Code Smell", "S1135:Track uses of \"TODO\" tags", Justification = "Leave markers")]
[assembly: SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "THey are TODOs")]
[assembly: SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Do not warn")]
