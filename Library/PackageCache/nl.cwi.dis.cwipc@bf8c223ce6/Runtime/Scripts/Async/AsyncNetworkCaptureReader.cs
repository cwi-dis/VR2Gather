using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class AsyncNetworkCaptureReader : AsyncPointCloudReader
    {
        string url;
        bool compressed;
        AsyncTCPPCReader tcpPCReader;
        AbstractPointCloudDecoder decoder;
        private QueueThreadSafe decoderQueue;
        private QueueThreadSafe myQueue;

        public AsyncNetworkCaptureReader(string _url, bool _compressed, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(_outQueue, _out2Queue)
        {
            reader = null;
            url = _url;
            compressed = _compressed;
            myQueue = new QueueThreadSafe($"{Name()}.queue");
            decoderQueue = new QueueThreadSafe($"{Name()}.decoderQueue");
            InitReader();
            Start();
        }

        private void InitReader()
        {

            string fourcc = compressed ? "cwi1" : "cwi0";
            StreamSupport.IncomingTileDescription[] tileDescriptions = new StreamSupport.IncomingTileDescription[1]
            {
                new StreamSupport.IncomingTileDescription
                {
                    name="0",
                    outQueue=decoderQueue
                }
            };
            tcpPCReader = new AsyncTCPPCReader(url, fourcc, tileDescriptions);
            if (compressed)
            {
                decoder = new AsyncPCDecoder(decoderQueue, myQueue);
            }
            else
            {
                decoder = new AsyncPCNullDecoder(decoderQueue, myQueue);
            }
        }

        public override PointCloudTileDescription[] getTiles()
        {
            PointCloudTileDescription[] rv = new PointCloudTileDescription[1]
            {
                new PointCloudTileDescription
                {
                    cameraMask = 0,
                    cameraName = "0",
                    normal = Vector3.zero
                }
            };
            return rv;
        }

        public override void AsyncOnStop()
        {
            tcpPCReader?.Stop();
            tcpPCReader = null;
            decoder?.Stop();
            decoder = null;
            base.AsyncOnStop();
        }
        protected override cwipc.pointcloud GetOnePointcloud()
        {
            cwipc.pointcloud pc = null;
            if (dontWait) {
                pc = (cwipc.pointcloud)myQueue.TryDequeue(0);
            } 
            else
            {
                pc = (cwipc.pointcloud)myQueue.Dequeue();
            }
            return pc;
        }
    }
}
