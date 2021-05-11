﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class PrerecordedPlaybackReader : PrerecordedBaseReader
    {

        public PrerecordedPlaybackReader(string _dirname, float _voxelSize, float _frameRate)
        : base(_dirname, _voxelSize, _frameRate)
        {
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
    }
}

