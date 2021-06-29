using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC
{
    public class XmlServiceSyncWorker : IServiceSyncWorker
    {
        public XmlServiceSyncWorker(string xmlPath)
        { 
        
        }

        public bool HasValue => throw new NotImplementedException();

        IDictionary<string, IService> IServiceSyncWorker.Result => throw new NotImplementedException();
    }
}
