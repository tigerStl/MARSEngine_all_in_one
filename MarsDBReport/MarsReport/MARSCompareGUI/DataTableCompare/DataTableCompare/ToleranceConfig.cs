using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class ToleranceConfig
    {

        public string FieldName { get; set; }

        public string CompareType { get; set; }

        public double ToleranceValue { get; set; }
    }
}
