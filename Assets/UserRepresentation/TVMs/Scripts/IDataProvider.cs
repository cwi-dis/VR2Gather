using System;
using VRT.Transport.RabbitMQ.Utils;

namespace VRT.UserRepresentation.TVM.DataProviders
{
    public interface IDataProvider
    {
        event EventHandler<EventArgs<byte[]>> OnNewData;
    }
}
