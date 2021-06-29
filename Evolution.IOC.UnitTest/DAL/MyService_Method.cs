using Evolution.IOC.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MyService_Method : IMyService
    {

        private Guid _now;
        public IMySingleTonService _mySingleTonService;

        public MyService_Method()
        {
            _now = Guid.NewGuid();
        }

        [MethodInjection]
        public void MyServiceInjection(IMySingleTonService mySingleTonService)
        {
            _now = Guid.NewGuid();
            this._mySingleTonService = mySingleTonService;
        }

        public void Run()
        {
            Console.WriteLine($"HomeService CreateTime: {_now.ToString()} ,Current thread id is {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine("------------------Method way------------------");
            _mySingleTonService.Run();
        }
    }
}
