using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsExcelDataProvider
{
    public class ExcelDataHandlerFactory
    {
        public  ExcelDataHandler GetExcelDataHandler(string path, out string reason)
        {
            ExcelDataHandler excelDataHandler = new ExcelDataHandler(path,  out reason);
            return excelDataHandler;

            
        }
    }
}
