using RabbitMQ.Utils;
using System;

namespace DataProviders
{
    public interface IDataProvider {
        event EventHandler<EventArgs<byte[]>> OnNewData;
    }
}
