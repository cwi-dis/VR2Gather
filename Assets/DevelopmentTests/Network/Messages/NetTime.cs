using System.Runtime.InteropServices;
using UnityEngine;

public class NetTime : MessageBase {
    public ushort       id;
    public float        timeStamp;
    public float        serverTime;

    public MessageBase Prepare() {
        timeStamp = NetController.LocalClock;
//        Debug.Log($">>> NetTime.Prepare {id} {timeStamp} serverTime {serverTime} LocalClock {NetController.LocalClock}");
        return this;
    }

    public override void Process() {
        float ping = (NetController.LocalClock - timeStamp ) / 2f;
        NetController.OffsetClock = serverTime  - NetController.LocalClock;
//        Debug.Log($">>> NetTime {id} {timeStamp} serverTime {serverTime} OffsetClock {NetController.OffsetClock}");
    }
}
