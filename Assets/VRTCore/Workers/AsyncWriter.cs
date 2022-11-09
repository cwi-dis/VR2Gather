namespace VRT.Core
{
    public abstract class AsyncWriter : AsyncWorker
    {
        public AsyncWriter() : base()
        {
        }

        public virtual SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            return new SyncConfig.ClockCorrespondence();
        }
    }
}