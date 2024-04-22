using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Cwipc
{
    /// <summary>
    /// Play a sequence of prerecorded pointclouds (think: volumetric video)
    /// </summary>
    public class PointCloudPlayback : MonoBehaviour
    {
        [Tooltip("Point cloud reader prefab")]
        public PrerecordedPointCloudReader reader_prefab;
        [Tooltip("Point cloud renderer prefab")]
        public PointCloudRenderer renderer_prefab;
        [Tooltip("If true start playback on Start")]
        public bool playOnStart = false;
        [Tooltip("If true attempt to preload point cloud files into operating system cache")]
        public bool preload = false;
        [Tooltip("Number of times point cloud stream is looped (zero: forever)")]
        public int loopCount = 0;
        [Tooltip("If nonzero: fade in point clouds for this many seconds")]
        public float fadeIn = 0f;
        [Tooltip("If nonzero: fade out point clouds (after natural duration) for this many seconds")]
        public float fadeOut = 0f;
        [Tooltip("Directory with point cloud files")]
        public string dirName = "";
        [Tooltip("Invoked when playback starts")]
        public UnityEvent started;
        [Tooltip("Invoked when playback finishes")]
        public UnityEvent finished;
        [Tooltip("(introspection) point cloud reader")]
        public PrerecordedPointCloudReader cur_reader;
        [Tooltip("(introspection) point cloud renderer")]
        public PointCloudRenderer cur_renderer;

        public string Name()
        {
            return $"{GetType().Name}";
        }


        // Start is called before the first frame update
        void Start()
        {
            if (playOnStart)
            {
                Play(dirName);
            }
        }

        public void Play(string _dirName)
        {
            if (cur_reader != null || cur_renderer != null)
            {
                Debug.LogError($"{Name()}: Play() called while playing");
                return;
            }
            cur_reader = Instantiate(reader_prefab, transform);
            cur_renderer = Instantiate(renderer_prefab, transform);
            cur_renderer.pointcloudSource = cur_reader;
            Debug.Log($"{Name()}: Play({dirName})");
            dirName = _dirName;
            StartCoroutine(startPlay());
        }

        private IEnumerator startPlay()
        {
            if (preload)
            {
                Debug.Log($"{Name()}: start preload");
                Thread th = new Thread(preloadThread);
                th.Start();
                while (th.IsAlive)
                {
                    yield return null;
                }
                Debug.Log($"{Name()}: preload done");
            }
            else
            {
                yield return null;
            }
            cur_reader.dirName = dirName;
            cur_reader.loopCount = loopCount;
            cur_reader.gameObject.SetActive(true);
            cur_renderer.gameObject.SetActive(true);
            if (fadeIn > 0)
            {
                float elapsedTime = 0f;
                while (elapsedTime < fadeIn)
                {
                    elapsedTime += Time.deltaTime;
                    cur_renderer.pointSizeFactor = elapsedTime / fadeIn;
                    yield return null;
                }
            }
        }

        private void preloadThread()
        {
            string[] filenames = System.IO.Directory.GetFileSystemEntries(dirName);
            foreach(var filename in filenames)
            {
                byte[] dummy = System.IO.File.ReadAllBytes(filename);
            }
        }

        private IEnumerator stopPlay()
        {
            yield return null;
            cur_reader.Stop();
            finished.Invoke(); // xxxjack or should this be done after the fade out?
            if (fadeOut > 0)
            {
                float elapsedTime = 0f;
                while (elapsedTime < fadeOut)
                {
                    elapsedTime += Time.deltaTime;
                    cur_renderer.pointSizeFactor = (1 - elapsedTime / fadeOut);
                    yield return null;
                }
            }
            Destroy(cur_reader.gameObject);
            Destroy(cur_renderer.gameObject);
            cur_reader = null;
            cur_renderer = null;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void RendererStarted()
        {
            Debug.Log($"{Name()}: Renderer started");
            started.Invoke();
        }

        public void RendererFinished()
        {
            Debug.Log($"{Name()}: Renderer finished");
            StartCoroutine(stopPlay());
        }

        public void Stop()
        {
            if (cur_reader != null || cur_renderer != null)
            {
                Debug.Log($"{Name()}: Stop");
                StartCoroutine(stopPlay());
            }
        }
    }
}
