namespace Cwipc
{
    public abstract class AsyncWriter : AsyncWorker
    {
        /// <summary>
        /// Base class for object implementing asynchronous writer, through AsyncWorker.
        /// Usually subclasses of this class are network protocol transmitters.
        /// </summary>
        public AsyncWriter() : base()
        {
        }
        /// <summary>
        /// Asks this outgoing stream for its correspondence between local system time and stream time.
        /// The intention is that the caller converts local system time to some shared time reference (possibly NTP time)
        /// and then transfers this information to the receiver. The receiver will then be able to re-map the timestamps
        /// of the incoming stream to its local system time.
        /// </summary>
        /// <param name="_clockCorrespondence">Mapping between timestamps</param>
        public virtual SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            return new SyncConfig.ClockCorrespondence();
        }
    }
}