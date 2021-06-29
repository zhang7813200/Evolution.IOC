using Evolution.IOC.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MyService_Constructor : IMyService
    {

        private Guid _now;
        public IMySingleTonService _mySingleTonService;

        [ConstructorInjection]
        public MyService_Constructor(IMySingleTonService mySingleTonService)
        {
            _now = Guid.NewGuid();
            _mySingleTonService = mySingleTonService;
        }
        public void Run()
        {
            Console.WriteLine($"HomeService CreateTime: {_now.ToString()} ,Current thread id is {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine("------------------Constructor way------------------");
            _mySingleTonService.Run();
        }
    }
}
