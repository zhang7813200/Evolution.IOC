using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Evolution.IOC
{
    [Flags]
    public enum InjectionWay
    {
        [Description("默认构造器注入")]
        None = 0,
        [Description("构造器注入")]
        Constructor = 1,
        [Description("方法注入")]
        Method = 2,
        [Description("属性注入")]
        Property = 4,
        [Description("智能注入:使用此框架者如果不会使用特性标识注入模式, 此枚举会按照 构造器→方法→属性顺序，依次查找需要被注入的接口")]
        Smart = 8,
        [Description("兼容所有注入模式")]
        All = 99999,
    }
}
