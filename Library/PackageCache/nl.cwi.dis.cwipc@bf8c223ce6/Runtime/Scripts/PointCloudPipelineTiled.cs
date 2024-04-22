using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using IncomingTileDescription = StreamSupport.IncomingTileDescription;
    using IncomingStreamDescription = StreamSupport.IncomingStreamDescription;
    /// MonoBehaviour that controls a pointcloud pipeline for tiled streams: capture/reception and display.
    /// This is the only class for controlling the display of pointcloud streams.
    /// There is always a source (network source, multi-file tiled playback).
    /// Renderers are created on initialization, one per tile stream, based on a prefab.
    /// 
    /// There is always a renderer (MonoBehaviour), but it may be disabled by subclasses (before Start() is called).
    /// There is never a transmitter, but subclasses (such as PointCloudSelfPipelineSimple) may override that.
    ///
    /// Usually an application will override InitializeTileAndStreamDescriptions, because
    /// it will use some out-of-band method whereby the sender application tells the receiver application which tiles (and which qualities per
    /// tile) can be expected. The receiver application will then set the descriptors for the PointCloudPipelineTiled
    /// that shows the pointclouds from that sender app.
    public class PointCloudPipelineTiled : MonoBehaviour
    {
        public enum SourceType
        {
            TCP,
            WebRTC
        };
        [Tooltip("Type of source to create")]
        [SerializeField] public SourceType sourceType;
        [Tooltip("Renderer to clone, one for each tile")]
        [SerializeField] protected GameObject PCrendererPrefab;
        
        [Header("Settings shared by (some) sources")]
        [Tooltip("Rendering cellsize, if not specified in pointcloud")]
        [SerializeField] protected float Preparer_DefaultCellSize = 1.0f;
        [Tooltip("Multiplication factor for pointcloud cellsize")]
        [SerializeField] protected float Preparer_CellSizeFactor = 1.0f;
        [Header("Source type: WebRTC")]
        [SerializeField] public int clientId;
        [Header("Source type: TCP")]
        [Tooltip("Specifies TCP server to contact for source, in the form tcp://host:port")]
        [SerializeField] public string inputUrl;
        [Tooltip("Insert a compressed pointcloud decoder into the stream")]
        public bool compressedInputStream;

        [Header("Tile and per-tile stream descriptions")]
        [Tooltip("Number of tiles to receive. Should match the number on the other side")]
        [SerializeField] protected int nTiles = 1;
        [Tooltip("The tiles that are expected")]
        [SerializeField] protected IncomingTileDescription[] tileDescription;

        [Header("Introspection/debugging")]
        [Tooltip("Renderers created")]
        [SerializeField] protected PointCloudRenderer[] PCrenderers;

        protected QueueThreadSafe[] ReaderDecoderQueues;
        protected QueueThreadSafe[] DecoderPreparerQueues;
        protected AsyncReader PCreceiver;
        protected AbstractPointCloudDecoder[] PCdecoders;
        protected AsyncPointCloudPreparer[] PCpreparers;

        // Start is called before the first frame update

        void Start()
        {
            InitializeTileDescription();
            InitializePipeline(); 
        }

        /// <summary>
        /// Initialize nTiles and tileDescription.
        /// Usually overridden to implement application business logic, unless the business logic uses SetTileDescription
        /// to set the tiling information.
        /// </summary>
        protected virtual void InitializeTileDescription()
        {
            if (tileDescription != null && tileDescription.Length != 0)
            {
                // descriptions have been initialized already. Use that information.
                nTiles = tileDescription.Length;
                return;
            }
            if (nTiles == 0) nTiles = 1;
            tileDescription = new IncomingTileDescription[nTiles];
            for (int i = 0; i < nTiles; i++)
            {
                
                tileDescription[i] = new IncomingTileDescription()
                {
                    name = $"tile#{i}",
                    tileNumber = i,
                    streamDescriptors = null
                };
            }
        }

        /// <summary>
        /// Set the information on incoming tiles. Must be called before Start().
        /// Alternatively a subclass can override InitializeTileDescription.
        /// </summary>
        /// <param name="descr"></param>
        /// <exception cref="System.Exception"></exception>
        public void SetTileDescription(IncomingTileDescription[] descr)
        {
            if (tileDescription != null && tileDescription.Length != 0)
            {
                throw new System.Exception("PointCloudPipelineTiled: attempting to override SetTileDescription");
            }
            if (descr == null || descr.Length == 0)
            {
                throw new System.Exception("PointCloudPipelineTiled: attempting to set empty SetTileDescription");
            }
            tileDescription = descr;
        }

        protected virtual void InitializePipeline()
        {
            //
            // Create the queues
            //
            ReaderDecoderQueues = new QueueThreadSafe[nTiles];
            DecoderPreparerQueues = new QueueThreadSafe[nTiles];
            //
            // Create the queues and store the per-tile decoded output queue into the tile description.
            //
            string fourcc = compressedInputStream ? "cwi1" : "cwi0";
            for (int tileIndex=0; tileIndex<nTiles; tileIndex++)
            {
                ReaderDecoderQueues[tileIndex] = new QueueThreadSafe($"ReaderDecoderQueue#{tileIndex}", 2, true);
                DecoderPreparerQueues[tileIndex] = new QueueThreadSafe($"DecoderPreparerQueue#{tileIndex}", 2, false);
                tileDescription[tileIndex].outQueue = ReaderDecoderQueues[tileIndex];
            }
            //
            // Create the receiver
            //
            if (sourceType == SourceType.WebRTC)
            {
                PCreceiver = new AsyncWebRTCPCReader(inputUrl, clientId, fourcc, tileDescription);
            }
            else
            {
                PCreceiver = new AsyncTCPPCReader(inputUrl, fourcc, tileDescription);
            }
            //
            // Create the decoders, preparers and renderers. Tie them together using the correct queues.
            //

            PCdecoders = new AbstractPointCloudDecoder[nTiles];
            PCpreparers = new AsyncPointCloudPreparer[nTiles];
            PCrenderers = new PointCloudRenderer[nTiles];
            for (int tileIndex = 0; tileIndex < nTiles; tileIndex++)
            {
                AbstractPointCloudDecoder newDecoderObject = CreateDecoder(ReaderDecoderQueues[tileIndex], DecoderPreparerQueues[tileIndex]);
                GameObject newGameObject = Instantiate<GameObject>(PCrendererPrefab, transform);
                PointCloudRenderer newRendererObject = newGameObject.GetComponent<PointCloudRenderer>();
                AsyncPointCloudPreparer newPreparerObject = new AsyncPointCloudPreparer(DecoderPreparerQueues[tileIndex], Preparer_DefaultCellSize, Preparer_CellSizeFactor);
                PCdecoders[tileIndex] = newDecoderObject;
                PCpreparers[tileIndex] = newPreparerObject;
                PCrenderers[tileIndex] = newRendererObject;
                newRendererObject.SetPreparer(newPreparerObject);
            }
        }


        AbstractPointCloudDecoder CreateDecoder(QueueThreadSafe inQueue, QueueThreadSafe outQueue)
        {
            if (compressedInputStream)
            {
                return new AsyncPCDecoder(inQueue, outQueue);
            }
            else
            {
                return new AsyncPCNullDecoder(inQueue, outQueue);
            }

        }

        protected virtual void OnDestroy()
        {
            PCreceiver?.StopAndWait();
            if (PCdecoders != null)
            {
                foreach(var d in PCdecoders)
                {
                    d.StopAndWait();
                }
            }
            if (PCpreparers != null)
            {
                foreach(var p in PCpreparers)
                {
                    p.StopAndWait();
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
