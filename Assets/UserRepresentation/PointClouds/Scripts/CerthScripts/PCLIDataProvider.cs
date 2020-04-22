using System;
using Utils;

namespace DataProviders
{
    public interface PCLIdataProvider
    {
        event EventHandler<EventArgs<byte[]>> OnNewPCLData;
        event EventHandler<EventArgs<byte[]>> OnNewMetaData;
    }
}
