using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC
{
    public interface IServiceSyncWorker
    {
        bool HasValue { get; }
        IDictionary<string,IService> Result { get; }
    }
}
