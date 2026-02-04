using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class CompareErrorLineItem
    {

        private static int LastId = 1;


        public int LineId { get; set; }

        public enum Status { Equal, NotEqual, Left, Right};
        public int LineNumber { get; set; }

        public Status ErrorStatus { get; set; }

        public List<int> ErrorColumns = new List<int>();

        public string ErrorMessage = "";

        public CompareErrorLineItem()
        {
            ErrorStatus = Status.Equal;
            LineId = LastId++;
        }

        public System.Data.DataRow dr1 { get; set; }

        public System.Data.DataRow dr2 { get; set; }
    }
}
