using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare
{
    public class DataCompareError
    {
        public bool Status = true;
        public string Message = ""; 
        public string refFileNameWithPath { get; set; }
    }
}
