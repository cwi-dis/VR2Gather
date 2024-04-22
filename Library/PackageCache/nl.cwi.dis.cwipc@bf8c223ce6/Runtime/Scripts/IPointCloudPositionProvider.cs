using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public interface IPointCloudPositionProvider
    {
        Vector3? GetPosition();
        int GetCameraCount();
    }
}