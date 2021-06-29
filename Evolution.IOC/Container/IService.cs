using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC
{
    public interface IService: IRegisterInfo
    {
        Object Instance { get; set; }
    }

    public interface IRegisterInfo
    {
        InjectionWay InjectionWay { get; set; }
        LifeTime LifeTime { get; set; }
        Type ImplementClassType { get; set; }
        Type InterfaceType { get; set; }
        object[] ParameterValues { get; set; }
    }
}
