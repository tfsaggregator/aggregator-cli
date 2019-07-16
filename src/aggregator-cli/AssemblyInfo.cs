using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyCompany("TFS Aggregator Team")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCopyright("TFS Aggregator Team")]
[assembly: AssemblyFileVersion("0.9.0.0")]
[assembly: AssemblyInformationalVersion("0.9.0+local.development")]
[assembly: AssemblyProduct("Aggregator CLI")]
[assembly: AssemblyTitle("Aggregator CLI")]
[assembly: AssemblyVersion("0.9.0.0")]

[assembly:InternalsVisibleTo("unittests-ruleng")]
