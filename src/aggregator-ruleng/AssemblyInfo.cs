using System;
using System.Reflection;

[assembly: AssemblyCompany("TFS Aggregator Team")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCopyright("TFS Aggregator Team")]
[assembly: AssemblyFileVersion("0.5.0.0")]
[assembly: AssemblyInformationalVersion("0.5.0")]
[assembly: AssemblyProduct("Aggregator CLI")]
[assembly: AssemblyTitle("Aggregator Rule Engine")]
[assembly: AssemblyVersion("0.5.0.0")]

[assembly:InternalsVisibleTo("unittests-core")]