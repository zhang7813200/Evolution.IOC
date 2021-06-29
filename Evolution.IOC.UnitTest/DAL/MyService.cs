using Evolution.IOC.Attribute;
using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MyService : IMyService
    {

        private Guid _now;

        public MyService()
        {
            _now = Guid.NewGuid();
        }

        public void Run()
        {
            Console.WriteLine($"HomeService CreateTime: {_now.ToString()} ,Current thread id is {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            System.Threading.Thread.Sleep(1000);
        }
    }
}
