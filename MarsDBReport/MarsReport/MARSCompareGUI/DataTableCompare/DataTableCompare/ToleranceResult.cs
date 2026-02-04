using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class ToleranceResult
    {
        public bool Result { get; set; }

        public string ResultComment { get; set; }

        public double diff { get; set; }

        public string ToString()
        {
            string result = null; ;

            result = "Result = " + Result + " | " + "ResultComment " + ResultComment + " | " + " diff " + diff;

            return result;
        }
    }
}
