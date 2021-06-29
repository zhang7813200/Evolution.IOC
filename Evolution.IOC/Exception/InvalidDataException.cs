using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Evolution.IOC.Exception
{
    public class InvalidDataException : System.Exception
    {
        public InvalidDataException():base()
        {
        }

        public InvalidDataException(string message) : base(message)
        {
        }

        public InvalidDataException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
