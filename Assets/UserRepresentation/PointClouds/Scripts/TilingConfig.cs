using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    // Used only for Prerecorded: information allowing the tile selector to find
    // the files with the bandwidth prediction information.
    // 
    public struct StaticPredictionInformation
    {
        public string baseDirectory;
        public string[] tileNames;
        public string[] qualityNames;
        public string predictionFilename;
    };
   

}