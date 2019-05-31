using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCodec {
    public static BaseCodec Instance { get; private set; }

    public BaseCodec() {
        BaseCodec.Instance = this;
    }

    public virtual int recorderFrequency { get;  set; }
    public virtual int playerFrequency { get; set; }
    public virtual int bufferLeght { get; set; }

    public virtual byte[] Compress(float[] data, int offset) {
        return null;
    }

    // Update is called once per frame
    public virtual float[] Uncompress(byte[] data, int offset) {
        return null;
    }
}
