using UnityEngine;

namespace Workers {
	public class PCSubReader : BaseSubReader {
        public PCSubReader(string _url, string _streamName, int _streamNumber, int _initialDelay, QueueThreadSafe _outQueue) 
        : base(_url, _streamName, _initialDelay)
        {
            receivers = new ReceiverInfo[]
            {
                new ReceiverInfo() 
                { 
                    outQueue = _outQueue,
                    streamIndexes = new int[] { _streamNumber}
                }
            };
        }
    }
}
