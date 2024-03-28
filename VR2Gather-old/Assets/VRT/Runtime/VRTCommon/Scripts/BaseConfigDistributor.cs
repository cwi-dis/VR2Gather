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
        protected Dictionary<string, BasePipeline> pipelines = new Dictionary<string, BasePipeline>();

        public virtual string Name()
        {
            return $"{GetType().Name}";
        }

        public virtual void SetSelfUserId(string _selfUserId)
        {
            selfUserId = _selfUserId;
        }

        public virtual void RegisterPipeline(string userId, BasePipeline pipeline)
        {

            if (pipelines.ContainsKey(userId) && pipelines[userId] != pipeline)
            {
                Debug.Log($"{Name()}: replacing pipeline for userId {userId}");
            }
            pipelines[userId] = pipeline;
        }

    }
}