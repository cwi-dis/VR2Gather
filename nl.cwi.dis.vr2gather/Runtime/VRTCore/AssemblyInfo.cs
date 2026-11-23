using System.Runtime.CompilerServices;

// The package's Editor tests load config.json the way VRTConfig does (#416).
[assembly: InternalsVisibleTo("VRT.Tests.Editor")]
