//========= Copyright 2017, HTC Corporation. All rights reserved. ===========

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR
{
    public class ViveSR_DualCameraImageRenderer : MonoBehaviour
    {
        public static bool UpdateDistortedMaterial
        {
            get { return _UpdateDistortedMaterial; }
            set { if (value != _UpdateDistortedMaterial) _UpdateDistortedMaterial = value;}
        }
        public static bool UpdateUndistortedMaterial
        {
            get { return _UpdateUndistortedMaterial; }
            set { if (value != _UpdateUndistortedMaterial) _UpdateUndistortedMaterial = value;}
        }
        public static bool UpdateDepthMaterial
        {
            get { return _UpdateDepthMaterial; }
            set { if (value != _UpdateDepthMaterial) _UpdateDepthMaterial = value;}
        }
        public static UndistortionMethod UndistortMethod
        {
            get { return _UndistortMethod; }
            set { if (value != _UndistortMethod) SetUndistortMode(value); }
        }
        public static bool CallbackMode
        {
            get { return _CallbackMode; }
            set { if (value != _CallbackMode) SetCallbackEnable(value);}
        }        
        public static bool DepthImageOcclusion 
        { 
            get { return _DepthImageOcclusion; }
            set { if (value != _DepthImageOcclusion) SetDepthImageOcclusionEnable(value); } 
        }
        public static bool VisualizeDepthOcclusion
        {
            get { return _VisualizeDepthOcclusion; }
            set { if (value != _VisualizeDepthOcclusion) SetDepthOcclusionVisualized(value); }
        }
        public static float OcclusionNearDistance
        {
            get { return _OcclusionNearDistance; }
            set { if (value != _OcclusionNearDistance) SetDepthOcclusionNearDistance(value); }
        }
        public static float OcclusionFarDistance
        {
            get { return _OcclusionFarDistance; }
            set { if (value != _OcclusionFarDistance) SetDepthOcclusionFarDistance(value); }
        }
        private static bool _UpdateDistortedMaterial = false;
        private static bool _UpdateUndistortedMaterial = false;
        private static bool _UpdateDepthMaterial = false;
        private static bool _CallbackMode = false;
        private static bool _DepthImageOcclusion = false;
        private static bool _VisualizeDepthOcclusion = false;
        private static float _OcclusionNearDistance = 0.2f;
        private static float _OcclusionFarDistance = 2.0f;
        private static UndistortionMethod _UndistortMethod = UndistortionMethod.UNDISTORTED_BY_SRMODULE;


        public List<Material> DistortedLeftCameraImageMaterials;
        public List<Material> DistortedRightCameraImageMaterials;
        public List<Material> UndistortedLeftCameraImageMaterials;
        public List<Material> UndistortedRightCameraImageMaterials;
        public List<Material> DepthMaterials;

        private ViveSR_Timer DistortedTimer = new ViveSR_Timer();
        private ViveSR_Timer UndistortedTimer = new ViveSR_Timer();
        private ViveSR_Timer DepthTimer = new ViveSR_Timer();
        public static float RealDistortedFPS;
        public static float RealUndistortedFPS;
        public static float RealDepthFPS;
        private int LastDistortedTextureUpdateTime = 0;
        private int LastUndistortedTextureUpdateTime = 0;
        private int LastDepthTextureUpdateTime = 0;
        private Matrix4x4[] PoseDistorted = new Matrix4x4[2];
        private Matrix4x4[] PoseUndistorted = new Matrix4x4[2];
        private Texture2D[] TextureUndistorted = new Texture2D[2];
        private bool EnablePreRender = true;
        private Texture2D[,] TextureBuffer = new Texture2D[60,2];
        private Matrix4x4[,] PoseUndistortedBuffer = new Matrix4x4[60,2];
        private float[] bufferTime = new float[60];

        [Range(0, 59)]
        public int BufferDelay = 0;
        int ptrBuffer = 0;
        int maxptrbuffer = 60;
        int ptrDelayed = 0;
        int BufferDelayAux = 0;
        public bool poseDelayed = false;
        //int ptr = 0;
        bool bufferEnabler = false;
        public bool EnableDelay;


        public float MeasuredDelay = 0;
        float numberofDelays = 0;
        public float meanDelayValue = 0;

        //The region of Deprecation period API will remove in the future.
        #region Deprecation period API
        /**
        * The variable DistortedLeft has changed to DistortedLeftCameraImageMaterials.
        * @warning The variable will remove in the future.
        */
        public List<Material> DistortedLeft
        {
            get { return DistortedLeftCameraImageMaterials; }
            set { DistortedLeftCameraImageMaterials = value; }
        }
        /**
        * The variable DistortedRight has changed to DistortedRightCameraImageMaterials.
        * @warning The variable will remove in the future.
        */
        public List<Material> DistortedRight
        {
            get { return DistortedRightCameraImageMaterials; }
            set { DistortedRightCameraImageMaterials = value; }
        }
        /**
        * The variable UndistortedLeft has changed to UndistortedLeftCameraImageMaterials.
        * @warning The variable will remove in the future.
        */
        public List<Material> UndistortedLeft
        {
            get { return UndistortedLeftCameraImageMaterials; }
            set { UndistortedLeftCameraImageMaterials = value; }
        }
        /**
        * The variable UndistortedRight has changed to UndistortedRightCameraImageMaterials.
        * @warning The variable will remove in the future.
        */
        public List<Material> UndistortedRight
        {
            get { return UndistortedRightCameraImageMaterials; }
            set { UndistortedRightCameraImageMaterials = value; }
        }
        /**
        * The variable Depth has changed to DepthMaterials.
        * @warning The variable will remove in the future.
        */
        public List<Material> Depth
        {
            get { return DepthMaterials; }
            set { DepthMaterials = value; }
        }
        #endregion

        //private delegate void UnityRenderEvent(int eventID);
        //private System.IntPtr PtrIssuePluginEvent;

        private void Start()
        {
            //IEnumerator Start() 
            SetUndistortMode(_UndistortMethod);
            SetDepthOcclusionNearDistance(_OcclusionNearDistance);
            SetDepthOcclusionFarDistance(_OcclusionFarDistance);
            //PtrIssuePluginEvent = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((UnityRenderEvent)(ViveSR_Framework.UnityRenderEvent));
            Camera.onPreRender += PreRender;
        }

        private void OnApplicationQuit()
        {
            Camera.onPreRender -= PreRender;
        }


        private void SaveTexture(Texture2D texture)
        {
            Texture2D tex = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);

            // put buffer into texture
            tex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            tex.Apply();
            byte[] bytes = texture.EncodeToPNG();
            var dirPath = Application.dataPath + "/RenderOutput";
            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }
            System.IO.File.WriteAllBytes(dirPath + "/R_" + System.DateTime.Now.TimeOfDay.TotalMilliseconds + ".png", bytes);
            Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + dirPath);
#if UNITY_EDITOR
         UnityEditor.AssetDatabase.Refresh();
#endif
        }


        private void PreRender(Camera eye)
        {
            if (EnablePreRender == false)
                return;

            if (LastDistortedTextureUpdateTime != 0 || LastUndistortedTextureUpdateTime != 0)
            {
                if (ViveSR_DualCameraRig.Instance.DualCameraLeft == eye || ViveSR_DualCameraRig.Instance.DualCameraRight == eye)
                {
                    if (UpdateUndistortedMaterial == true && _UndistortMethod == UndistortionMethod.UNDISTORTED_BY_SRMODULE)
                    {
                        var positionLeft = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                        var rotationLeft = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                        var positionRight = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                        var rotationRight = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                        ViveSR_DualCameraRig.Instance.SetCameraPoses(positionLeft, rotationLeft, positionRight, rotationRight);
                    }
                    else if (_UpdateDistortedMaterial)
                    {
                        var positionLeft = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.LEFT]);
                        var rotationLeft = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.LEFT]);
                        var positionRight = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                        var rotationRight = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                        ViveSR_DualCameraRig.Instance.SetCameraPoses(positionLeft, rotationLeft, positionRight, rotationRight);
                    }
                }
            }
        }

        private IEnumerator UpdateGPUUndistortTexture()
        {
            yield return new WaitForEndOfFrame();
        }

        private void Update()
        {
            if (ViveSR_DualCameraRig.DualCameraStatus == DualCameraStatus.WORKING)
            {
                if (!CallbackMode)
                {
                    if (UpdateDistortedMaterial)
                    {
                        // native buffer ptr method 1: 
                        // get native buffer ptr & let native(cpp) do texture upload
                        //StartCoroutine(UpdateGPUFishEyeTexture());
                        ViveSR_DualCameraImageCapture.UpdateDistortedImage();
                    }
                    if (UpdateUndistortedMaterial)
                    {
                        ViveSR_DualCameraImageCapture.UpdateUndistortedImage();
                    }
                    if (UpdateDepthMaterial) ViveSR_DualCameraImageCapture.UpdateDepthImage();


                }

                #region Distorted Image
                if (_UpdateDistortedMaterial)
                {
                    int current_camera_time_index = ViveSR_DualCameraImageCapture.DistortedTimeIndex;
                    if (current_camera_time_index != LastDistortedTextureUpdateTime)
                    {
                        DistortedTimer.Add(current_camera_time_index - LastDistortedTextureUpdateTime);
                        RealDistortedFPS = 1000 / DistortedTimer.AverageLeast(100);
                        int frame_index, time_index;
                        Texture2D texture_camera_left, texture_camera_right;
                        ViveSR_DualCameraImageCapture.GetDistortedTexture(out texture_camera_left, out texture_camera_right, out frame_index, out time_index, out PoseDistorted[(int)DualCameraIndex.LEFT], out PoseDistorted[(int)DualCameraIndex.RIGHT]);
                        for (int i = 0; i < DistortedLeftCameraImageMaterials.Count; i++)
                        {
                            if (DistortedLeftCameraImageMaterials[i] != null)
                            {
                                DistortedLeftCameraImageMaterials[i].mainTexture = texture_camera_left;
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    DistortedLeftCameraImageMaterials[i].mainTextureScale = new Vector2(1, 0.5f);
                                    DistortedLeftCameraImageMaterials[i].mainTextureOffset = new Vector2(0, 0.5f);
                                }
                            }
                        }
                        for (int i = 0; i < DistortedRightCameraImageMaterials.Count; i++)
                        {
                            if (DistortedRightCameraImageMaterials[i] != null)
                            {
                                DistortedRightCameraImageMaterials[i].mainTexture = texture_camera_right;
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    DistortedRightCameraImageMaterials[i].mainTextureScale = new Vector2(1, 0.5f);
                                }
                            }
                        }
                        LastDistortedTextureUpdateTime = current_camera_time_index;

                        //change pose update flow to camera preRender
                        if (EnablePreRender == false)
                        {
                            var positionLeft = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.LEFT]);
                            var rotationLeft = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.LEFT]);
                            var positionRight = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                            var rotationRight = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                            ViveSR_DualCameraRig.Instance.SetCameraPoses(positionLeft, rotationLeft, positionRight, rotationRight);
                        }
                    }
                }
                #endregion

                #region Undistorted Image
                if (_UpdateUndistortedMaterial)
                {
                    if (BufferDelay != BufferDelayAux) EnableDelay = false; //if the delay has been changed during the last update by the user, the delay will be disabled.
                    BufferDelayAux = BufferDelay;
                    //here is the point where we should create the buffer, next step, create a buffer taking into account the  current_undistorted_time_iIndex
                    // and the  ViveSR_DualCameraImageCapture.GetUndistortedTexture

                    int current_undistorted_time_iIndex = ViveSR_DualCameraImageCapture.UndistortedTimeIndex;
                    if (current_undistorted_time_iIndex != LastUndistortedTextureUpdateTime)
                    {

                        UndistortedTimer.Add(current_undistorted_time_iIndex - LastUndistortedTextureUpdateTime);
                        RealUndistortedFPS = 1000 / UndistortedTimer.AverageLeast(100);
                        int frame_index_undistorted, time_index_undistorted;
                        ViveSR_DualCameraImageCapture.GetUndistortedTexture(out TextureUndistorted[(int)DualCameraIndex.LEFT], out TextureUndistorted[(int)DualCameraIndex.RIGHT], out frame_index_undistorted, out time_index_undistorted,
                            out PoseUndistorted[(int)DualCameraIndex.LEFT], out PoseUndistorted[(int)DualCameraIndex.RIGHT]);

                        //Here begin the modifications to the original file 
                        if (EnableDelay) {
                            if (TextureBuffer[ptrBuffer, (int)DualCameraIndex.LEFT] == null)
                            {
                                

                                TextureBuffer[ptrBuffer, (int)DualCameraIndex.LEFT] = new Texture2D(TextureUndistorted[(int)DualCameraIndex.LEFT].width, TextureUndistorted[(int)DualCameraIndex.LEFT].height,TextureFormat.RGBA32,false, false);
                                
                            }

                            if(PoseUndistortedBuffer[ptrBuffer,(int)DualCameraIndex.LEFT] == null)
                            {
                                PoseUndistortedBuffer[ptrBuffer, (int)DualCameraIndex.LEFT] = new Matrix4x4();
                            }

                           
                                
                            Graphics.CopyTexture(TextureUndistorted[(int)DualCameraIndex.LEFT], TextureBuffer[ptrBuffer, (int)DualCameraIndex.LEFT]);
                            if(BufferDelay == 0) TextureUndistorted[(int)DualCameraIndex.LEFT] = TextureBuffer[(ptrBuffer) % maxptrbuffer, (int)DualCameraIndex.LEFT];
                            bufferTime[ptrBuffer] = Time.realtimeSinceStartup;
                            if (poseDelayed == true) PoseUndistortedBuffer[(ptrBuffer) % maxptrbuffer, (int)DualCameraIndex.LEFT] = PoseUndistorted[(int)DualCameraIndex.LEFT];
                            ptrBuffer = (ptrBuffer + 1) % maxptrbuffer; //for preventing overflow

                            if (TextureBuffer[(ptrDelayed) % maxptrbuffer, (int)DualCameraIndex.LEFT] != null && bufferEnabler) //if there is enough samples, use the buffer, if not, continue in real time
                            {
                                //PoseUndistorted
                                TextureUndistorted[(int)DualCameraIndex.LEFT] = TextureBuffer[(ptrDelayed) % maxptrbuffer, (int)DualCameraIndex.LEFT]; //circular buffer
                                if (poseDelayed == true) PoseUndistorted[(int)DualCameraIndex.LEFT] = PoseUndistortedBuffer[(ptrDelayed) % maxptrbuffer, (int)DualCameraIndex.LEFT];
                                MeasuredDelay += Time.realtimeSinceStartup - bufferTime[(ptrDelayed) % maxptrbuffer];
                                numberofDelays++;
                                meanDelayValue = MeasuredDelay / numberofDelays;
                                Debug.Log("Mean Delay Values is: " + meanDelayValue.ToString() + "  fps value is: " + RealUndistortedFPS.ToString());
                                ptrDelayed = (ptrDelayed + 1) % maxptrbuffer; //for preventing overflow


                            }
                            else
                            {
                                if ((Mathf.Abs(ptrBuffer - ptrDelayed) % maxptrbuffer) >= BufferDelay && BufferDelay != 0) bufferEnabler = true;
                                else bufferEnabler = false;

                            }
                            
                        }
                        else
                        {
                            poseDelayed = false;
                            ptrDelayed = 0;
                            ptrBuffer = 0;
                            bufferEnabler = false;
                            meanDelayValue = 0;
                            MeasuredDelay = 0;
                            numberofDelays = 0;

                        }
                        if (Input.GetKeyDown("f5")) //GetStateDown takes as input argument the source (Righthand, Camera, Head, or whatever... )
                        {
                            SaveTexture(TextureUndistorted[(int)DualCameraIndex.LEFT]);
                        }
                        //Here the modifications end.

                        for (int i = 0; i < UndistortedLeftCameraImageMaterials.Count; i++)
                        {
                            if (UndistortedLeftCameraImageMaterials[i] != null)
                            {
                                UndistortedLeftCameraImageMaterials[i].mainTexture = TextureUndistorted[(int)DualCameraIndex.LEFT];
                                // restore the tiling / offset which may be modified
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    UndistortedLeftCameraImageMaterials[i].mainTextureScale = Vector2.one;
                                    UndistortedLeftCameraImageMaterials[i].mainTextureOffset = Vector2.zero;
                                }
                            }
                        }
                        for (int i = 0; i < UndistortedRightCameraImageMaterials.Count; i++)
                        {
                            if (UndistortedRightCameraImageMaterials[i] != null)
                            {
                                UndistortedRightCameraImageMaterials[i].mainTexture = TextureUndistorted[(int)DualCameraIndex.RIGHT];
                                // restore the tiling / offset which may be modified
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    UndistortedRightCameraImageMaterials[i].mainTextureScale = Vector2.one;
                                }
                            }
                        }
                        LastUndistortedTextureUpdateTime = current_undistorted_time_iIndex;

                        if (_UndistortMethod == UndistortionMethod.UNDISTORTED_BY_SRMODULE && EnablePreRender == false)
                        {
                            var positionLeft = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                            var rotationLeft = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                            var positionRight = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                            var rotationRight = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                            ViveSR_DualCameraRig.Instance.SetCameraPoses(positionLeft, rotationLeft, positionRight, rotationRight);
                        }
                    }
                }
                #endregion

                #region Depth Image
                if (_UpdateDepthMaterial)
                {
                    int current_depth_time_index = ViveSR_DualCameraImageCapture.DepthTimeIndex;
                    if (current_depth_time_index != LastDepthTextureUpdateTime)
                    {
                        DepthTimer.Add(current_depth_time_index - LastDepthTextureUpdateTime);
                        RealDepthFPS = 1000 / DepthTimer.AverageLeast(100);
                        int frame_index, time_index;
                        Texture2D textureDepth;
                        Matrix4x4 PoseDepth;
                        ViveSR_DualCameraImageCapture.GetDepthTexture(out textureDepth, out frame_index, out time_index, out PoseDepth);
                        for (int i = 0; i < DepthMaterials.Count; i++)
                        {
                            if (DepthMaterials[i] != null) DepthMaterials[i].mainTexture = textureDepth;
                        }
                        LastDepthTextureUpdateTime = current_depth_time_index;
                    }
                }
                #endregion
            }
        }

        private void OnDisable()
        {
            SetCallbackEnable(false);

            // restore the tiling / offset which may be modified
            for (int i = 0; i < DistortedLeftCameraImageMaterials.Count; i++)
            {
                if (DistortedLeftCameraImageMaterials[i] != null)
                {
                    DistortedLeftCameraImageMaterials[i].mainTextureScale = Vector2.one;
                    DistortedLeftCameraImageMaterials[i].mainTextureOffset = Vector2.zero;
                }
            }
            for (int i = 0; i < DistortedRightCameraImageMaterials.Count; i++)
            {
                if (DistortedRightCameraImageMaterials[i] != null)
                {
                    DistortedRightCameraImageMaterials[i].mainTextureScale = Vector2.one;
                }
            }
        }

        private static void SetUndistortMode(UndistortionMethod method)
        {
            _UndistortMethod = method; 
            if (_UndistortMethod == UndistortionMethod.UNDISTORTED_BY_SRMODULE)
            {
                UpdateDistortedMaterial = false;
                UpdateUndistortedMaterial = true;
            }
            else
            {
                UpdateDistortedMaterial = true;
                UpdateUndistortedMaterial = false;
            }
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.ImagePlane.SetUndistortMethod(UndistortMethod);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.ImagePlane.SetUndistortMethod(UndistortMethod);
        }

        private static void SetDepthImageOcclusionEnable(bool enable)
        {
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.gameObject.SetActive(enable);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.gameObject.SetActive(enable);
            _DepthImageOcclusion = enable;
        }

        private static void SetDepthOcclusionVisualized(bool enable)
        {
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.sharedMaterial.SetInt("_ColorWrite", enable? 15 : 0);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.sharedMaterial.SetInt("_ColorWrite", enable ? 15 : 0);
            _VisualizeDepthOcclusion = enable;
        }

        private static void SetDepthOcclusionNearDistance(float value)
        {
            _OcclusionNearDistance = Mathf.Min(value, _OcclusionFarDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.sharedMaterial.SetFloat("_MinDepth", _OcclusionNearDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.sharedMaterial.SetFloat("_MinDepth", _OcclusionNearDistance);
        }

        private static void SetDepthOcclusionFarDistance(float value)
        {
            _OcclusionFarDistance = Mathf.Max(value, _OcclusionNearDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.sharedMaterial.SetFloat("_MaxDepth", _OcclusionFarDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.sharedMaterial.SetFloat("_MaxDepth", _OcclusionFarDistance);
        }

        

        private static void SetCallbackEnable(bool enable)
        {
            _CallbackMode = enable;
        }
        private void Release() {
            for (int i = 0; i < TextureUndistorted.Length; i++)
            {
                Texture2D.Destroy(TextureUndistorted[i]);
                TextureUndistorted[i] = null;
            }
        }
    }
}
