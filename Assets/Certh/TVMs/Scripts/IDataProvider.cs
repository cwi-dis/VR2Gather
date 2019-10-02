using System;
using Utils;

namespace DataProviders
{
    public interface IDataProvider
    {
        event EventHandler<EventArgs<byte[]>> OnNewData;
    }
}
