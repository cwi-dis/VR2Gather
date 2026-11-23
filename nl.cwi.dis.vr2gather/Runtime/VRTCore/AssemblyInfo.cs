using System.Runtime.CompilerServices;

// The package's tests load config.json the way VRTConfig does, and point it at their own config (#416).
[assembly: InternalsVisibleTo("VRT.Tests.Editor")]
[assembly: InternalsVisibleTo("VRT.Tests.Runtime")]
