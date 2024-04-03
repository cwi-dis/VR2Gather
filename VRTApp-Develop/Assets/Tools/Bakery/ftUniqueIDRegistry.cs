using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ftUniqueIDRegistry
{
    public static Dictionary<Int32, Int32> Mapping = new Dictionary<int, int>();
    public static Dictionary<Int32, Int32> MappingInv = new Dictionary<int, int>();

    public static void Deregister(Int32 id)
    {
        int instanceId = GetInstanceId(id);
        if (instanceId < 0) return;
        MappingInv.Remove(instanceId);
        Mapping.Remove(id);
    }

    public static void Register(Int32 id, Int32 value)
    {
        if (!Mapping.ContainsKey(id)) {
            Mapping[id] = value;
            MappingInv[value] = id;
        }
    }

    public static Int32 GetInstanceId(Int32 id)
    {
        Int32 instanceId;
        if (!Mapping.TryGetValue(id, out instanceId))
        {
            return -1;
        }
        else
        {
            return instanceId;
        }
    }

    public static Int32 GetUID(Int32 instanceId)
    {
        return MappingInv[instanceId];
    }
}
