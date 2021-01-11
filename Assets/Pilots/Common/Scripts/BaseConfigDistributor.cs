using UnityEngine;
using System.Collections.Generic;
using VRT.Core;

namespace VRT.Pilots.Common
{
    abstract public class BaseConfigDistributor : MonoBehaviour
    {
        // Note there is an AddTypeIdMapping(420, typeof(TilingConfigDistributor.TilingConfigMessage))
        // in MessageForwarder that is part of the magic to make this work.
        public class BaseConfigMessage : BaseMessage
        {

        }
        protected string selfUserId;

        abstract public BaseConfigDistributor Init(string _selfUserId);

        abstract public void RegisterPipeline(string userId, BasePipeline pipeline);
    }
}