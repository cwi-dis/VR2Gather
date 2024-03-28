using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

namespace VRT.UserRepresentation.PointCloud
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class PrerecordedPlaybackReader : AsyncPrerecordedBaseReader
    {

        public PrerecordedPlaybackReader(string _dirname, float _voxelSize, float _frameRate)
        : base(_dirname, _voxelSize, _frameRate)
        {
            multireader = true;
        }

        public StaticPredictionInformation GetStaticPredictionInformation()
        {
            return new StaticPredictionInformation()
            {
                baseDirectory = baseDirectory,
                tileNames = tileSubdirs,
                qualityNames = qualitySubdirs,
                predictionFilename = "tiledescription.csv"
            };
        }

        public override void ReportCurrentTimestamp(Timestamp curIndex)
        {
            //xxxshishir set current position for tile selection
            PrerecordedTileSelector.curIndex = curIndex;
        }
    }
}

