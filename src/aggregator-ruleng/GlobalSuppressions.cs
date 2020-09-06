using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Sonar Code Smell", "S3966:Objects should not be disposed more than once", Justification = "We dispose only BCL objects and we trust how they implemented Dispose()")]
[assembly: SuppressMessage("Major Code Smell", "S0125:Sections of code should not be commented out", Justification = "We only comment code as TODOs")]
[assembly: SuppressMessage("Info Code Smell", "S1135:Track uses of \"TODO\" tags", Justification = "True TODOs")]
