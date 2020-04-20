using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using UnityEngine;


// TODO(FPA): Fix new Queue mode.
namespace Workers
{
    public class NetReader : BaseWorker
    {
        string hostName;
        int port;


        System.IntPtr subHandle;
        System.IntPtr currentBuffer;
        int currentSize = 0;

        public NetReader(Config._User._NetConfig cfg) :base(WorkerType.Init) {
            hostName = cfg.hostName;
            port = cfg.port;
        }

        public override void OnStop()
        {
            base.OnStop();
            if (currentBuffer != System.IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(currentBuffer);
        }


        protected override void Update() {
            base.Update();
            TcpClient clt = new TcpClient(hostName, port);
            List<byte> allData = new List<byte>();
            using (NetworkStream stream = clt.GetStream())
            {
                byte[] data = new byte[1024];
                do {
                    int numBytesRead = stream.Read(data, 0, data.Length);
                    if (numBytesRead == data.Length) {
                        allData.AddRange(data);
                    }
                    else if (numBytesRead > 0)
                    {
                        allData.AddRange(data.Take(numBytesRead));
                    }
                } while (stream.DataAvailable);
            }
                
            byte[] bytes = allData.ToArray();

//                token.currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
//                token.currentSize = bytes.Length;
       }
    }
}

