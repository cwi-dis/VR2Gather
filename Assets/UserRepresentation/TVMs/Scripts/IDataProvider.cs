using System;
using VRT.Transport.RabbitMQ.Utils;

namespace DataProviders
{
    public interface IDataProvider {
        event EventHandler<EventArgs<byte[]>> OnNewData;
    }
}
