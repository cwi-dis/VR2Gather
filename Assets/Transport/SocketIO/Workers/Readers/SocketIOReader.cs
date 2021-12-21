using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using VRT.Core;

namespace VRT.Transport.SocketIO
{
    public class SocketIOReader : BaseReader, ISocketReader
    {
        PCSubReader.TileDescriptor[] descriptors;

        User user;

        public SocketIOReader(User user, string remoteStream, string fourcc, PCSubReader.TileDescriptor[] descriptors) : base(WorkerType.End)
        {
            this.user = user;
            if (descriptors == null)
            {
                throw new System.Exception($"{Name()}: descriptors is null");
            }
            this.descriptors = descriptors;
            try
            {
                for (int i = 0; i < this.descriptors.Length; ++i)
                {
                    this.descriptors[i].name = $"{user.userId}.{remoteStream}.{fourcc}#{i}";
                    Debug.Log($"[FPA] RegisterForDataStream userId {user.userId} StreamType {this.descriptors[i].name}");
                    OrchestratorWrapper.instance.RegisterForDataStream(user.userId, this.descriptors[i].name);
                }
                OrchestratorWrapper.instance.OnDataStreamReceived += OnDataPacketReceived;
                stats = new Stats(Name());
                Start();
                Debug.Log($"{Name()}: Started {remoteStream}.");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw;
            }
        }

        public SocketIOReader(User user, string remoteStream, string fourcc, QueueThreadSafe outQueue)
        : this(user,
            remoteStream,
            fourcc,
              new PCSubReader.TileDescriptor[]
              {
                  new PCSubReader.TileDescriptor()
                  {
                      outQueue = outQueue
                  }
              }
            )
        {
            stats = new Stats(Name());
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public override void Stop()
        {
            base.Stop();
            for (int i = 0; i < descriptors.Length; ++i)
            {
                descriptors[i].outQueue?.Close();
                Debug.Log($"[FPA] {Name()}: Stopped.");
                if (OrchestratorWrapper.instance != null && OrchestratorController.Instance.SelfUser != null)
                    OrchestratorWrapper.instance.UnregisterFromDataStream(OrchestratorController.Instance.SelfUser.userId, descriptors[i].name);
            }
        }

        private void OnDataPacketReceived(UserDataStreamPacket pPacket)
        {
            // This callback method is called for _every_ socketIO reader. We select only the packets that are ours.
            string streamName = pPacket.dataStreamType;
            for(int i = 0; i < descriptors.Length; i++)
            {
                if (streamName == descriptors[i].name)
                {
                    // This packet is for us.
                    byte[] hdr_timestamp = new byte[sizeof(long)];
                    Array.Copy(pPacket.dataStreamPacket, hdr_timestamp, sizeof(long));
                    long timestamp = BitConverter.ToInt64(hdr_timestamp, 0);
                    BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.dataStreamPacket.Length - sizeof(long));
                    chunk.info.timestamp = timestamp;
                    System.Runtime.InteropServices.Marshal.Copy(pPacket.dataStreamPacket, sizeof(long), chunk.pointer, chunk.length);
                    stats.statsUpdate(chunk.length, i);
                    descriptors[i].outQueue.Enqueue(chunk);
                }
            }
           
        }

        public void OnData(byte[] data)
        {
        }

        protected override void Update()
        {
            base.Update();
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            System.DateTime statsConnectionStartTime;
            double statsTotalBytes;
            double statsTotalPackets;
            bool statsGotFirstReception;

            public void statsUpdate(int nBytes, int streamId)
            {
                if (!statsGotFirstReception)
                {
                    statsConnectionStartTime = System.DateTime.Now;
                    statsGotFirstReception = true;
                }

                System.TimeSpan sinceEpoch = System.DateTime.Now - statsConnectionStartTime;
             
                statsTotalBytes += nBytes;
                statsTotalPackets++;
                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, last_stream_id={streamId}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                }
            }
        }

        protected Stats stats;

    }

}
