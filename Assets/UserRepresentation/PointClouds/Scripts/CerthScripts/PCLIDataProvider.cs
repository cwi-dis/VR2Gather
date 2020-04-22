using System;
using Utils;

namespace PCLDataProviders
{
    public interface PCLIdataProvider
    {
        event EventHandler<EventArgs<byte[]>> OnNewPCLData;
        event EventHandler<EventArgs<byte[]>> OnNewMetaData;
    }
}
