using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.Attribute
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class PropertyInjectionAttribute : System.Attribute
    {
    }
}
