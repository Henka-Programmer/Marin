using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework.Exceptions
{
    public class UserErrorException : Exception
    {
        public UserErrorException()
        {
        }

        public UserErrorException(string message) : base(message)
        {
        }
    }
}
