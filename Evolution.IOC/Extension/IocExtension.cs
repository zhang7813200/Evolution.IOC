using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Evolution.IOC
{
    public static class IOCExtension
    {
        
    }

    public static class ServiceExtension
    {
        public static T UseInjectionWay<T>(this T service, Func<InjectionWay> getInjectionWay) where T:IRegisterInfo
        {
            if (service == null || getInjectionWay == null)
            {
                throw new ArgumentNullException();
            }
            service.InjectionWay = getInjectionWay.Invoke();
            return service;
        }

        public static T GetDefault<T>(this T t)
        {
            return default(T);
        }
    }
}
