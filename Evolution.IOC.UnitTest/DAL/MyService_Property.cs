using Evolution.IOC.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MyService_Property : IMyService
    {

        private Guid _now;
        [PropertyInjection]
        public IMySingleTonService MySingleTonService { get; set; }

        public MyService_Property()
        {
            _now = Guid.NewGuid();
        }
        public void Run()
        {
            Console.WriteLine($"HomeService CreateTime: {_now.ToString()} ,Current thread id is {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine("------------------Property way------------------");
            MySingleTonService.Run();
        }
    }
}
