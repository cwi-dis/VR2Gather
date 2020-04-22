using System;
using Utils;

namespace DataProviders
{
    public interface IDataProvider
    {
        event EventHandler<EventArgs<byte[]>> OnNewPCLData;
        event EventHandler<EventArgs<byte[]>> OnNewMetaData;
    }
}
