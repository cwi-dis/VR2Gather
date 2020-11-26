using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeeX : BaseCodec {
    NSpeex.SpeexDecoder decoder;
    NSpeex.SpeexEncoder encoder;
    byte[]              sendBuffer;
    float[]             receiveBuffer;

    public SpeeX() : base() {
        encoder             = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
        encoder.Quality     = 5;
        bufferLeght         = encoder.FrameSize;
        recorderFrequency   = encoder.SampleRate;
        decoder             = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide);
        playerFrequency     = decoder.SampleRate;
    }

    public override byte[] Compress(float[] data, int offset) {
        if (sendBuffer == null) {
            byte[] tmp = new byte[data.Length];
            int len = encoder.Encode(data, 0, 1, tmp, offset, tmp.Length);
            sendBuffer = new byte[len + offset];
        }
        encoder.Encode(data, 0, 1, sendBuffer, offset, sendBuffer.Length-offset);
        return sendBuffer;
    }

    public override float[] Uncompress(byte[] data, int offset) {
        if (receiveBuffer == null) receiveBuffer = new float[bufferLeght];
        decoder.Decode(data, offset, data.Length - offset, receiveBuffer);
        return receiveBuffer;
    }
}
