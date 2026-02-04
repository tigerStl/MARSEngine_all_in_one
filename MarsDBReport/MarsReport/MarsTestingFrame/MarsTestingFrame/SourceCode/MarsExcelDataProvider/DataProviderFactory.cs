using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsExcelDataProvider
{
    public class DataProviderFactory
    {
        static DataProvider dataProvider = null;
        public  DataProvider GetDataProvider()
        {
            if (DataProviderFactory.dataProvider == null)
            {
                dataProvider = new DataProvider();
                dataProvider.init();
            }
                
            return DataProviderFactory.dataProvider;
        }
    }
}
