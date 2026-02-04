using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlCompareLib
{
    public class XmlFileCompareException : Exception
{
    public XmlFileCompareException()
    {
    }

    public XmlFileCompareException(string message)
        : base(message)
    {
    }

    public XmlFileCompareException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
}
