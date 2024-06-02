using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Transport.SocketIO
{
    public interface ISocketReader
    {
        void OnData(byte[] data);
    }
}