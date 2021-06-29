using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Evolution.IOC.Exception
{
    public class NullResultException : System.Exception
    {
        public NullResultException():base()
        {
        }

        public NullResultException(string message) : base(message)
        {
        }

        public NullResultException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        protected NullResultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
