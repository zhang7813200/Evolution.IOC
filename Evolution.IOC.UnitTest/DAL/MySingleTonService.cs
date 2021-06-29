using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MySingleTonService : IMySingleTonService
    {
        private Guid _now;

        public MySingleTonService()
        {
            _now = Guid.NewGuid();
        }

        public void Run()
        {
            Console.WriteLine($"MySingleTonService CreateTime: {_now.ToString()} ,Current thread id is {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        }
    }
}
