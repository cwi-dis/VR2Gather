using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Teleporter
{
    public abstract class BaseTeleporter : MonoBehaviour
    {

        // Turn teleport ray on or off
        public abstract void SetActive(bool active);

        public abstract bool teleporterActive { get; }

        // Teleport target transform to ground position
        public abstract void Teleport();

        // Return true if the current destination of the ray can be teleported to.
        public abstract bool canTeleport();

        // Draw the teleport ray
        public abstract void UpdatePath();

        // Change direction and strength of teleport ray and draw it
        public abstract void CustomUpdatePath(Vector3? _origin, Vector3? _direction, float _strength);
    }
}