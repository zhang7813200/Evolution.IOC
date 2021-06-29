using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.Attribute
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class MethodInjectionAttribute : System.Attribute
    {
    }
}
