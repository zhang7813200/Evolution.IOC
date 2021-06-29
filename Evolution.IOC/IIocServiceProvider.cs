using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC
{
    public interface IIocServiceProvider
    {
        IRegisterInfo GetService(string interfaceFullName);
        IEnumerable<IRegisterInfo> GetServices();
    }
}
