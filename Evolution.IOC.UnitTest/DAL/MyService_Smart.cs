using Evolution.IOC.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MyService_Smart : IMyService
    {

        private Guid _now;
        
        private IMySingleTonService _mySingleTonService1;
        private IMySingleTonService _mySingleTonService2;
        public IMySingleTonService IMySingleTonService { get; set; }

        public MyService_Smart(IMySingleTonService mySingleTonService)
        {
            _now = Guid.NewGuid();
            this._mySingleTonService1 = mySingleTonService;
        }

        public void SmartMethod(IMySingleTonService mySingleTonService)
        {
            this._mySingleTonService2 = mySingleTonService;
        }

        public void Run()
        {
            Console.WriteLine($"MyService_Smart CreateTime: {_now.ToString()} ,Current thread id is {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine("------------------Smart Constructor way------------------");
            this._mySingleTonService1.Run();
            Console.WriteLine("------------------Smart Method way------------------");
            this._mySingleTonService2.Run();
            Console.WriteLine("------------------Smart Property way------------------");
            IMySingleTonService.Run();
        }
    }
}
