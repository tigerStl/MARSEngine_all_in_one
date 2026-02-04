
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class ErrorReport
    {
        public List<CompareErrorLineItem> errorList = new List<CompareErrorLineItem>();

        public DataTable ResultDataTable = new DataTable();

        //public void GenetateLineItem()
        //{
        //    CompareErrorLineItem item = new CompareErrorLineItem();
        //}

        internal void GenetateLineItem(CompareErrorLineItem lineItem)
        {
            errorList.Add(lineItem);
        }
    }
}
