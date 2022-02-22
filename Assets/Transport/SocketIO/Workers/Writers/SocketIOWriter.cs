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
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class SocketIOWriter : BaseWriter
    {
        B2DWriter.DashStreamDescription[] streams;

        public SocketIOWriter(User user, string remoteStream, string fourcc, B2DWriter.DashStreamDescription[] streams) : base()
        {
            if (streams == null)
            {
                throw new System.Exception($"[FPA] {Name()}: outQueue is null");
            }
            stats = new Stats(Name());
            this.streams = streams;
            for (int i = 0; i < streams.Length; ++i)
            {
                streams[i].name = $"{user.userId}.{remoteStream}.{fourcc}#{i}";
                Debug.Log($"[FPA] DeclareDataStream userId {user.userId} StreamType {streams[i].name}");
                BaseStats.Output(Name(), $"streamid={i}, tile={streams[i].tileNumber}, orientation={streams[i].orientation}, streamname={streams[i].name}");
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
                throw;
            }
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }


        public override void OnStop()
        {
            base.OnStop();
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
                    var hdr_timestamp = BitConverter.GetBytes(chk.info.timestamp);
                    var buf = new byte[chk.length+sizeof(long)];
                    Array.Copy(hdr_timestamp, buf, sizeof(long));
                    System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, sizeof(long), chk.length);
                    OrchestratorWrapper.instance.SendData(streams[i].name, buf);
                    stats.statsUpdate(chk.length, i);
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
           
            public void statsUpdate(int nBytes, int streamIndex)
            {
                statsTotalBytes += nBytes;
                statsTotalPackets++;
                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, last_stream_id={streamIndex}");
                    Clear();
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                }
            }
        }

        protected Stats stats;
    }
}
