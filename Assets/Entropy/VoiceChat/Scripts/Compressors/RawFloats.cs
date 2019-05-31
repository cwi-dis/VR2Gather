using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RawFloats : BaseCodec {

    byte[] sendBuffer;

    public RawFloats(int frequency) : base() {
        recorderFrequency = frequency;
        playerFrequency = frequency;
        bufferLeght = 735; // 40fps
    }

    public override byte[] Compress(float[] data, int offset) {
        if (sendBuffer == null) sendBuffer = new byte[data.Length * 4 + offset];
        System.Buffer.BlockCopy(data, 0, sendBuffer, offset, sendBuffer.Length - offset);
        return sendBuffer;
    }

    float[] receiveBuffer;
    public override float[] Uncompress(byte[] data, int offset)
    {
        if (receiveBuffer == null) receiveBuffer = new float[(data.Length-offset) / 4];
        System.Buffer.BlockCopy(data, offset, receiveBuffer, 0, data.Length - offset);

        return receiveBuffer;
    }
}
