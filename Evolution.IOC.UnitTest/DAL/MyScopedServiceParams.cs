using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC.UnitTest
{
    public class MyScopedServiceParams : IMyScopedServiceParams
    {
        private int _a;
        private string _b;
        public MyScopedServiceParams(int a, string b)
        {
            this._a = a;
            this._b = b;
        }

        public void Run()
        {
            throw new NotImplementedException();
        }
    }
}
