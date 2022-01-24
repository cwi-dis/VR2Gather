namespace VRT.Core
{
    public class BaseWriter : BaseWorker
    {
        public BaseWriter() : base()
        {
        }

        public virtual SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            return new SyncConfig.ClockCorrespondence();
        }
    }
}