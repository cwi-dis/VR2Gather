namespace VRT.Core
{
    public class BaseWriter : BaseWorker
    {
        public BaseWriter(WorkerType _type = WorkerType.Run) : base(_type)
        {
        }

        public virtual SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            return new SyncConfig.ClockCorrespondence();
        }
    }
}