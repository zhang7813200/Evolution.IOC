using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC
{
    public class ImplementInstanceInfo: IService
    {
        public Object Instance { get; set; }
        public LifeTime LifeTime { get; set; }
        public InjectionWay InjectionWay { get; set; }
        public Type ImplementClassType { get; set; }
        public object[] ParameterValues { get; set; }
        public Type InterfaceType { get; set; }
    }
}
