using System.Runtime.CompilerServices;

// The package's Play mode tests reset AutoStart between sessions (#416).
[assembly: InternalsVisibleTo("VRT.Tests.Runtime")]
