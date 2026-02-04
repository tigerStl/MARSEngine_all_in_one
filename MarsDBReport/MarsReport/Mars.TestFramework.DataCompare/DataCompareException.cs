using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare
{
    class DataCompareException : Exception
    {
        public DataCompareException(string message) : base(message)
        {
        }
    }
}
