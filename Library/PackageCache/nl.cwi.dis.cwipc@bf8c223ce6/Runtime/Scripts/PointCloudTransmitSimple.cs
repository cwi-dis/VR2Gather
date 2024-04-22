using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;
    using OutgoingStreamDescription = StreamSupport.OutgoingStreamDescription;

    public class PointCloudTransmitSimple : AbstractPointCloudSink
    {
      
        protected QueueThreadSafe TransmitterInputQueue;

       
        public override void InitializeTransmitter(PointCloudTileDescription[] tileDescriptions)
        {
            // Simple transmitter ignores tile descriptions.
            tileDescriptions = null;
            //
            // Create queue from reader to encoder and queue from encoder to transmitter.
            // The first one is declared in our base class, and will be picked up by its
            // Initialize method.
            //
            TransmitterInputQueue = new QueueThreadSafe("TransmitterInputQueue", 2, false);
            //
            // Create transmitter.
            //
            string fourcc = compressedOutputStreams ? "cwi1" : "cwi0";

            OutgoingStreamDescription[] transmitterDescriptions = new OutgoingStreamDescription[1]
            {
                new OutgoingStreamDescription
                {
                    name="single",
                    tileNumber=0,
                    qualityIndex=0,
                    orientation=Vector3.zero,
                    inQueue=TransmitterInputQueue
                }
            };
            EncoderStreamDescription[] encoderDescriptions = new EncoderStreamDescription[1]
            {
                new EncoderStreamDescription
                {
                    octreeBits=defaultOctreeBits,
                    tileFilter=0,
                    outQueue=TransmitterInputQueue
                }
            };
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