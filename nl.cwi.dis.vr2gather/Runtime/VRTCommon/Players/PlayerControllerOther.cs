using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{

    public class PlayerControllerOther : PlayerControllerBase
    {
        public override void SetUpPlayerController(bool _isLocalPlayer, VRT.Orchestrator.Responses.User user)
        {
            if (_isLocalPlayer)
            {
                Debug.LogError($"{Name()}: isLocalPlayer==true");
            }
            isLocalPlayer = false;
            _SetupCommon(user);
        }

        /// <summary>
        /// Get position in world coordinates. Should only be called on receiving pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public Vector3 GetPosition()
        {
       
            return transform.position;
        }
        /// <summary>
        /// Get rotation in world coordinates. Should only be called on receiving pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public Vector3 GetRotation()
        {
          
            return transform.rotation * Vector3.forward;
        }

        // Update is called once per frame
        System.DateTime lastUpdateTime;
        
        protected override void Update()
        {
            base.Update();
            if (debugTiling)
            {
                // Debugging: print position/orientation of camera and others every 10 seconds.
                if (lastUpdateTime == null || System.DateTime.Now > lastUpdateTime + System.TimeSpan.FromSeconds(10))
                {
                    lastUpdateTime = System.DateTime.Now;
                   
                    Vector3 position = GetPosition();
                    Vector3 rotation = GetRotation();
                    Debug.Log($"{Name()}: Tiling: other: pos=({position.x}, {position.y}, {position.z}), rotation=({rotation.x}, {rotation.y}, {rotation.z})");
                }
            }
        }
    }
}