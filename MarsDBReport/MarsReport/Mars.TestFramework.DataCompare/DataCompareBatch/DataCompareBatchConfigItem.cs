using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare.DataCompareBatch
{
    public class DataCompareBatchConfigItem
    {
        public string Name { get; set; }

        public string Action { get; set; }

        public string CompareConfigID { get; set; }

        public string File1 { get; set; }

        public string File2 { get; set; }

        public string OutputFile { get; set; }

        public string Status { get; set; }

        public string Comment { get; set; }

        public string OutputFileLink { get; set; }
        public bool isEmpty { get; internal set; }
    }
}
