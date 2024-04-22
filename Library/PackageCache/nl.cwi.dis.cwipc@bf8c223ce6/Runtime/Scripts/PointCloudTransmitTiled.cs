using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;
    using OutgoingStreamDescription = StreamSupport.OutgoingStreamDescription;

    public class PointCloudTransmitTiled : AbstractPointCloudSink
    {
        [Tooltip("Output Stream Descriptions")]
        [SerializeField] protected OutgoingStreamDescription[] transmitterDescriptions;
        [Tooltip("Output encoder parameters. Number and order must match transmitterDescriptions")]
        [SerializeField] protected EncoderStreamDescription[] encoderDescriptions;

        protected QueueThreadSafe[] TransmitterInputQueues;
       

        public override void InitializeTransmitter(PointCloudTileDescription[] tileDescriptions)
        {
            // 
            //
            // Override tile information from source.
            //
            if (tileDescriptions != null)
            {
                transmitterDescriptions = null;
                encoderDescriptions = null;
                if (tileDescriptions != null && tileDescriptions.Length > 0)
                {
                    int tileNumberToTileIndex = 0;
                    if (tileDescriptions.Length > 1 && tileDescriptions[0].cameraMask == 0)
                    {
                        // Workaround for design issue in tile filtering: tile zero
                        // is the unfiltered pointcloud. So if it is described in the
                        // tile descriptions we skip it.
                        if (tileDescriptions[0].cameraMask != 0)
                        {
                            Debug.LogWarning($"PointCloudInputTiled: tile[0] of multitiled source has unexpected mask {tileDescriptions[0].cameraMask}");
                        }
                        tileDescriptions = tileDescriptions[1..];
                        tileNumberToTileIndex = -1;
                    }
                    int nTile = tileDescriptions.Length;
                    transmitterDescriptions = new OutgoingStreamDescription[nTile];
                    encoderDescriptions = new EncoderStreamDescription[nTile];
                    for (int i = 0; i < nTile; i++)
                    {
                        int tileNumber = tileDescriptions[i].cameraMask;
                        uint tileIndex = (uint)(tileNumber + tileNumberToTileIndex);
                        transmitterDescriptions[i] = new OutgoingStreamDescription()
                        {
                            name = tileDescriptions[i].cameraName,
                            tileNumber = tileIndex,
                            qualityIndex = 0,
                            orientation = tileDescriptions[i].normal
                        };
                        encoderDescriptions[i] = new EncoderStreamDescription()
                        {
                            octreeBits = defaultOctreeBits,
                            tileFilter = tileNumber
                        };
                    }
                }
            }
            //
            // Override descriptions if not already initialized.
            //

            if (transmitterDescriptions == null || transmitterDescriptions.Length == 0)
            {
                Debug.Log($"PointCloudInputTiled: creating default transmitterDescriptions");
                transmitterDescriptions = new OutgoingStreamDescription[1]
                {
                new OutgoingStreamDescription
                {
                    name="single",
                    tileNumber=0,
                    qualityIndex=0,
                    orientation=Vector3.zero
                }
                };
            }
            if (encoderDescriptions == null || encoderDescriptions.Length == 0)
            {
                Debug.Log($"PointCloudInputTiled: creating default encoderDescriptions");
                encoderDescriptions = new EncoderStreamDescription[1]
                {
                new EncoderStreamDescription
                {
                    octreeBits=defaultOctreeBits,
                    tileFilter=0
                }
                };
            }
            //
            // Create queues from encoder to transmitter.
            //
            // The encoders and transmitters are tied together using their unique queue.
            //
            TransmitterInputQueues = new QueueThreadSafe[transmitterDescriptions.Length];
            for (int i = 0; i < transmitterDescriptions.Length; i++)
            {
                var name = transmitterDescriptions[i].name;
                // Note that it is a bit unclear whether to drop or not for the transmitter queue.
                // Not dropping means that all encoders and transmitters will hang if there is no
                // consumer for a specific tile. But dropping means that we may miss (on the receiver side)
                // one tile, and therefore have done a lot of encoding and decoding and transmission for nothing.
                var queue = new QueueThreadSafe($"TransmitterInputQueue#{name}", 2, true);
                TransmitterInputQueues[i] = queue;
                transmitterDescriptions[i].inQueue = queue;
                encoderDescriptions[i].outQueue = queue;
            }
            //
            // Create transmitter.
            //
            string fourcc = compressedOutputStreams ? "cwi1" : "cwi0";
            //
            // Create Encoder
            //
            if (compressedOutputStreams)
            {
                PCencoder = new AsyncPCEncoder(ReaderEncoderQueue, encoderDescriptions);
            }
            else
            {
                PCencoder = new AsyncPCNullEncoder(ReaderEncoderQueue, encoderDescriptions);
            }
            if (sinkType == SinkType.WebRTC)
            {
                PCtransmitter = new AsyncWebRTCWriter(outputUrl, fourcc, transmitterDescriptions);
            }
            else
            {
                PCtransmitter = new AsyncTCPWriter(outputUrl, fourcc, transmitterDescriptions);
            }
        }
    }
}