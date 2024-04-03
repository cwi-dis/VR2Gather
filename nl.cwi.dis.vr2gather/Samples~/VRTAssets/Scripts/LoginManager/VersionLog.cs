using System.IO;
using UnityEngine;
namespace VRT.Pilots.LoginManager
{

    public class VersionLog : MonoBehaviour
    {

        private static VersionLog _instance;

        public static VersionLog Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = FindObjectOfType<VersionLog>();
                }
                return _instance;
            }
        }

        private static string nativeClient;
        private static string player;
        private static string visualStudio;
        private static string libjpeg;
        private static string realSense;
        private static string pcl;
        private static string cwipc_util;
        private static string cwipc_rs2;
        private static string cwipc_codec;
        private static string sub;
        private static string b2d;

        public string NativeClient { get { return nativeClient; } }

        public string versionFilePath = "VRTsetup.ver";


        void Awake()
        {
            ReadVersion(versionFilePath);
            Debug.Log("Application Name: " + Application.productName);
            Debug.Log("Application Version: " + Application.version);
            LogVersion(versionFilePath);
        }

        static void ReadVersion(string path)
        {
            //Read the text from directly from the test.txt file
            StreamReader reader = new StreamReader(path);
            nativeClient = reader.ReadLine();
            player = reader.ReadLine();
            visualStudio = reader.ReadLine();
            libjpeg = reader.ReadLine();
            realSense = reader.ReadLine();
            pcl = reader.ReadLine();
            cwipc_util = reader.ReadLine();
            cwipc_rs2 = reader.ReadLine();
            cwipc_codec = reader.ReadLine();
            sub = reader.ReadLine();
            b2d = reader.ReadLine();
            reader.Close();
        }

        static void LogVersion(string path)
        {
            //Read the text from directly from the test.txt file
            StreamReader reader = new StreamReader(path);
            Debug.Log(reader.ReadToEnd());
            reader.Close();
        }
    }

}
