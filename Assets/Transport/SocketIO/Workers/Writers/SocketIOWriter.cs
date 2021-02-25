using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRTCore;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;

namespace VRT.Transport.SocketIO
{
    public class SocketIOWriter : BaseWriter
    {
        B2DWriter.DashStreamDescription[] streams;

        public SocketIOWriter(User user, string remoteStream, B2DWriter.DashStreamDescription[] streams) : base(WorkerType.End)
        {
            if (streams == null)
            {
                throw new System.Exception($"[FPA] {Name()}: outQueue is null");
            }
            this.streams = streams;
            for (int i = 0; i < streams.Length; ++i)
            {
                streams[i].name = $"{user.userId}{remoteStream}#{i}";
                Debug.Log($"[FPA] DeclareDataStream userId {user.userId} StreamType {streams[i].name}");
                OrchestratorWrapper.instance.DeclareDataStream(streams[i].name);
            }
            try
            {
                Start();
                Debug.Log($"[FPA] {Name()}: Started.");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw e;
            }
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
            for (int i = 0; i < streams.Length; ++i)
            {
                if (!streams[i].inQueue.IsClosed())
                {
                    Debug.LogWarning($"[FPA] {Name()}: inQueue not closed, closing");
                    streams[i].inQueue.Close();
                }
            }
            Debug.Log($"[FPA] {Name()}: Stopped.");
            OrchestratorWrapper.instance.RemoveDataStream("AUDIO");
        }

        protected override void Update()
        {
            base.Update();
            if (OrchestratorWrapper.instance != null && OrchestratorController.Instance.ConnectedToOrchestrator)
            {
                for (int i = 0; i < streams.Length; ++i)
                {
                    BaseMemoryChunk chk = streams[i].inQueue.Dequeue();
                    if (chk == null) return; // xxxjack shouldn't this be continue?????

                    var buf = new byte[chk.length];
                    System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, 0, chk.length);
                    OrchestratorWrapper.instance.SendData(streams[i].name, buf);
                    stats.statsUpdate(chk.length);
                    chk.free();

                }
            }
        }
        // FPA: Ask Jack about GetSyncInfo().

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            return new SyncConfig.ClockCorrespondence();
        }
        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalBytes;
            double statsTotalPackets;
           
            public void statsUpdate(int nBytes)
            {
                statsTotalBytes += nBytes;
                statsTotalPackets++;
                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}");
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
