namespace VRTLivePresenter
{
    public class MessageType
    {

        public const string START = "START";            // Start the experience and load the scene
        public const string READY = "READY";            // Send the ready state to the master
        public const string LIVESTREAM = "LIVESTREAM";  // Start the livepresenter livestream
        public const string PLAY = "PLAY";              // Play the selected video
        public const string PAUSE = "PAUSE";            // Pause the selected video
        public const string PING = "PING";              // Ping message to the master

    }
}