using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// MonoBehaviour that controls a pointcloud pipeline: capture/reception, display and optionally transmission.
    /// This is the main class for controlling the display of pointcloud streams.
    /// There is always a source (volumetric camera, network source, something else).
    /// There is always a renderer (MonoBehaviour), but it may be disabled by subclasses (before Start() is called).
    /// There is never a transmitter, but subclasses (such as PointCloudSelfPipelineSimple) may override that.
    /// </summary>
    public class PointCloudPipelineSimple : MonoBehaviour, IPointCloudPositionProvider
    {
        public enum SourceType
        {
            Synthetic,
            Auto,
            Realsense,
            Kinect,
            Prerecorded,
            Networked,
            // The following shouldn't be used as capturer
            TCP,
            WebRTC
        };
        [Tooltip("Type of source to create")]
        [SerializeField] public SourceType sourceType;
        [Tooltip("Renderer to use")]
        [SerializeField] protected PointCloudRenderer PCrenderer;

        [Header("Settings shared by (some) sources")]
        [Tooltip("Frame rate wanted")]
        [SerializeField] protected float framerate = 15;
        [Tooltip("Rendering cellsize, if not specified in pointcloud")]
        [SerializeField] protected float Preparer_DefaultCellSize = 0;
        [Tooltip("Multiplication factor for pointcloud cellsize")]
        [SerializeField] protected float Preparer_CellSizeFactor = 1.0f;

        [Header("Source type: Synthetic settings")]
        [Tooltip("Number of points per cloud")]
        [SerializeField] protected int Synthetic_NPoints = 8000;

        [Header("Source type: Realsense/Kinect/Auto settings")]
        [Tooltip("Camera configuration filename")]
        [SerializeField] protected string configFileName;
        [Tooltip("If non-zero: voxelize captured pointclouds to this cellsize")]
        [SerializeField] protected float voxelSize;

        [Header("Source type: networked settings")]
        [Tooltip("URL for the camera server")]
        [SerializeField] protected string networkedCameraURL;
        [Tooltip("True if camera server produces compressed pointclouds")]
        [SerializeField] protected bool networkedCameraCompressed;

        [Header("Source type: prerecorded")]
        [Tooltip("Path of directory with pointcloud files")]
        [SerializeField] protected string directoryPath;

        [Header("Source type: TCP or WebRTC")]
        [Tooltip("Specifies TCP server to contact for source, in the form tcp://host:port")]
        [SerializeField] public string inputUrl;
        [Tooltip("Insert a compressed pointcloud decoder into the stream")]
        public bool compressedInputStream;
        [Tooltip("WebRTC Client ID")]
        public int clientId;

        /// <summary>
        /// Simple pipeline will always force untiled transmission and report untiled stream.
        /// Can be overridden by subclasses.
        /// </summary>
        public virtual bool forceUntiled {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Overridden by subclasses that want to transmit the pointcloud stream.
        /// </summary>
        public virtual AbstractPointCloudSink transmitter { get { return null; } }

        /// <summary>
        /// Overridden by subclasses that want to disable display of the pointclouds.
        /// </summary>
        protected virtual bool enableOutput { get { return true; } }
        protected QueueThreadSafe ReaderRenderQueue;
        protected QueueThreadSafe RendererInputQueue;
        protected QueueThreadSafe ReaderEncoderQueue = null;
        protected AsyncPointCloudReader PCcapturer;
        protected AsyncReader PCreceiver;
        protected AbstractPointCloudDecoder PCdecoder;
        protected AsyncPointCloudPreparer PCpreparer;
        protected bool _initialized = false;

        // Start is called before the first frame update

        void Start()
        {
            InitializePipeline(); 
        }

        /// <summary>
        /// Return an array of TileDescriptions that describes the available tiles from the capturer of this pipeline.
        /// </summary>
        /// <returns></returns>
        public PointCloudTileDescription[] getTiles()
        {
            InitializePipeline();
            if (forceUntiled) return null;
            PointCloudTileDescription[] tileDescriptions = PCcapturer?.getTiles();
            return tileDescriptions;
        }

        /// <summary>
        /// Initialize the full pipeline.
        /// Usually not overridden, unless there is special application logic needed.
        /// </summary>
        protected virtual void InitializePipeline()
        {
            if (_initialized) return;
            _initialized = true;
            if (enableOutput)
            {
                ReaderRenderQueue = new QueueThreadSafe("ReaderRenderQueue", 2, true);
            }
            if (transmitter != null)
            {
                ReaderEncoderQueue = transmitter.InitializeTransmitterQueue();
            }
            InitializeReader();
            if (transmitter != null)
            {
                PointCloudTileDescription[] tileDescriptions = null;
                if (!forceUntiled)
                {
                    tileDescriptions = PCcapturer.getTiles();
                }
                transmitter.InitializeTransmitter(tileDescriptions);
            }
            if (RendererInputQueue == null)
            {
                RendererInputQueue = ReaderRenderQueue;
            }
            if (enableOutput)
            {
                PCpreparer = new AsyncPointCloudPreparer(RendererInputQueue, Preparer_DefaultCellSize, Preparer_CellSizeFactor);
                PCrenderer.SetPreparer(PCpreparer);
            }
            else
            {
                PCrenderer.enabled = false;
            }
        }

        /// <summary>
        /// Allocate the queue between the point cloud source and the transmitter (if there is a transmitter).
        /// </summary>
        /// <returns></returns>
        protected virtual QueueThreadSafe InitializeTransmitterQueue()
        {
            if (transmitter == null) return null;
            return transmitter.InitializeTransmitterQueue();
        }

        /// <summary>
        /// Initialize the transmitter (if there is one).
        /// Separate call from InitializeTransmitterQueue, because the source needs to be iniaitlized before the transmitter
        /// (because the transmitter may need to query the source for things like number of tiles).
        /// </summary>
        protected virtual void InitializeTransmitter()
        {

        }

        /// <summary>
        /// Initialize the source.
        /// </summary>
        void InitializeReader()
        {
            switch(sourceType)
            {
                case SourceType.Synthetic:
                    PCcapturer = new AsyncSyntheticReader(framerate, Synthetic_NPoints, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Auto:
                    PCcapturer = new AsyncAutoReader(configFileName, voxelSize, framerate, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Realsense:
                    PCcapturer = new AsyncRealsenseReader(configFileName, voxelSize, framerate, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Kinect:
                    PCcapturer = new AsyncKinectReader(configFileName, voxelSize, framerate, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Prerecorded:
                    //PCreceiver = new AsyncPrerecordedReader(directoryPath, voxelSize, framerate, ReaderOutputQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Networked:
                    PCcapturer = new AsyncNetworkCaptureReader(networkedCameraURL, networkedCameraCompressed, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.TCP:
                    InitializeDecoder(false);
                    break;
                case SourceType.WebRTC:
                    InitializeDecoder(true);
                    break;
            }
        }

        void InitializeDecoder(bool isWebRTC)
        {
            string fourcc = compressedInputStream ? "cwi1" : "cwi0";
            RendererInputQueue = new QueueThreadSafe("DecoderOutputQueue", 2, false);
            if (isWebRTC)
            {
                PCreceiver = new AsyncWebRTCReader(inputUrl, clientId, fourcc, ReaderRenderQueue);
            }
            else
            {
                PCreceiver = new AsyncTCPReader(inputUrl, fourcc, ReaderRenderQueue);
            }
            if (compressedInputStream)
            {
                PCdecoder = new AsyncPCDecoder(ReaderRenderQueue, RendererInputQueue);
            }
            else
            {
                PCdecoder = new AsyncPCNullDecoder(ReaderRenderQueue, RendererInputQueue);
            }

        }

        protected virtual void OnDestroy()
        {
            PCcapturer?.StopAndWait();
            PCreceiver?.StopAndWait();
            PCdecoder?.StopAndWait();
            PCpreparer?.StopAndWait();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public Vector3? GetPosition()
        {
            if (PCcapturer == null)
            {
                return null;
            }
            return PCcapturer.GetPosition();
        }

        public int GetCameraCount()
        {
            return PCcapturer.GetCameraCount();
        }
    }
}
