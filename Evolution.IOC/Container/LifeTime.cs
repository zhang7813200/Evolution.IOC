using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Evolution.IOC
{
    public enum LifeTime
    {
        None = 0,
        [Description("每次都创建对象")]
        Transient = 1,
        [Description("线程级别共享对象")]
        Scoped = 2,
        [Description("进程级别共享对象")]
        Singleton = 4
    }
}
