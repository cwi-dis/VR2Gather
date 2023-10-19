using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRT.Core
{
    public interface ErrorManagerSink
    {
        void FillError(string title, string message);
    }

    public class ErrorManager : MonoBehaviour
    {

        private static ErrorManager instance;

        public static ErrorManager Instance { get { return instance; } }

        [Tooltip("Prefab of dialog to use, if no errorManagerSink registered")]
        public GameObject myPrefab;

        [Tooltip("GameObject where dialogs are instantiated (if no errorManagerSink registered), should contain overlay canvas")]
        public GameObject myCanvas;

        [Tooltip("Hide BestHTTP errors")]
        public bool hideBestHTTPErrors = false;

        ErrorManagerSink mySink = null;

        List<string[]> queue = new List<string[]>();
        private object thisLock = new object();

        public void RegisterSink(ErrorManagerSink sink)
        {
            mySink = sink;
        }
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (myCanvas == null)
            {
                myCanvas = gameObject;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Application.logMessageReceived += Application_logMessageReceived;
        }

        private void Update()
        {
            lock (thisLock)
            {
                if (queue.Count > 0)
                {
                    foreach (string[] error in queue)
                    {
                        if (mySink != null)
                        {
                            mySink.FillError(error[0], error[1]);
                        }
                        else
                        {
                            // Show  dialog
                            bool instantiate = true;
                            ErrorPopup[] prevErrors = gameObject.GetComponentsInChildren<ErrorPopup>();
                            foreach (ErrorPopup item in prevErrors)
                            {
                                if (item.ErrorMessage == error[1])
                                    instantiate = false;
                            }
                            if (instantiate)
                            {
                                myCanvas.SetActive(true);
                                GameObject popup = Instantiate(myPrefab, myCanvas.transform);
                                popup.GetComponent<ErrorPopup>().FillError(error[0], error[1]);
                            }

                        }
                    }
                    queue.Clear();
                }
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= Application_logMessageReceived;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            string[] error = { "", "" };
            error[1] = condition;
            if (type == LogType.Exception)
            {
                error[0] = "Exception";
                lock (thisLock)
                {
                    queue.Add(error);
                }
            }
            else if (type == LogType.Error)
            {
                error[0] = "Error";
                // Don't show a popup for BestHTTP error messages
                if (hideBestHTTPErrors && stackTrace.Contains("BestHTTP")) return;
                // Don't show a popup for Quest virtual keyboard not enabled.
                if (condition.Contains("overlay keyboard is disabled")) return;
                lock (thisLock)
                {
                    queue.Add(error);
                }
            }
        }

        public void EnqueueOrchestratorError(int code, string message)
        {
            string[] error = { "Orchestrator Error", code.ToString() + ": " + message };
            queue.Add(error);
        }
    }
}