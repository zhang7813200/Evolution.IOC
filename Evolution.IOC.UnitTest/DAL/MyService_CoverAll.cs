using Evolution.IOC.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MyService_CoverAll : IMyService
    {

        private Guid _now;
        
        private IMySingleTonService _mySingleTonService1;
        private IMySingleTonService _mySingleTonService2;
        [PropertyInjection]
        public IMySingleTonService IMySingleTonService { get; set; }

        [ConstructorInjection]
        public MyService_CoverAll(IMySingleTonService mySingleTonService)
        {
            _now = Guid.NewGuid();
            this._mySingleTonService1 = mySingleTonService;
        }

        [MethodInjection]
        public void CoverAllMethod(IMySingleTonService mySingleTonService)
        {
            this._mySingleTonService2 = mySingleTonService;
        }

        public void Run()
        {
            Console.WriteLine($"MyService_CoverAll CreateTime: {_now.ToString()} ,Current thread id is {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine("------------------Constructor way------------------");
            this._mySingleTonService1.Run();
            Console.WriteLine("------------------Method way------------------");
            this._mySingleTonService2.Run();
            Console.WriteLine("------------------Property way------------------");
            IMySingleTonService.Run();
        }
    }
}
