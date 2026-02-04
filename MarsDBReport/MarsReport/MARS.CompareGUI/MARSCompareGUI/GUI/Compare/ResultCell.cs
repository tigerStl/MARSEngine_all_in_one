using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.TestFramework.DataCompare;

namespace MARS.TEMP
{
    class ResultCell
    {
 
        ErrorDescriptor errDescr;
        private string dataItem;

        public ResultCell(string dataItem)
        {
            this.dataItem = dataItem;
        }

        internal void SetErrorDescriptor(ErrorDescriptor errDescr)
        {
            this.errDescr = errDescr;
        }

        internal string GetData()
        {
            return dataItem;
        }

        internal ErrorDescriptor GetErrorDescr()
        {
            return errDescr;
        }
    }
}
