using UnityEngine;
using System.Collections.Generic;
using VRTCore;

namespace VRTPilots
{
    public class BaseConfigDistributor : MonoBehaviour
    {
        // Note there is an AddTypeIdMapping(420, typeof(TilingConfigDistributor.TilingConfigMessage))
        // in MessageForwarder that is part of the magic to make this work.
        public class BaseConfigMessage : BaseMessage
        {

        }
        protected string selfUserId;

        public BaseConfigDistributor Init(string _selfUserId)
        {
            selfUserId = _selfUserId;
            return this;
        }

        public void RegisterPipeline(string userId, BasePipeline pipeline)
        {
            Debug.LogError("Programmer error: BaseConfigDistributor: RegisterPipeline not overridden");
        }
    }
}