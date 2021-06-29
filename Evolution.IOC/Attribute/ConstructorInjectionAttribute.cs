using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.Attribute
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class ConstructorInjectionAttribute : System.Attribute
    {
    }
}
