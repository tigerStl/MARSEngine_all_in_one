using Mars.DataLayer;
using Mars.Model;
using System.Collections.Generic;
using System.Linq;

namespace Mars.Business
{
    public class B_TEST_ENUM : T_TEST_ENUM
    {



        public static List<string> GetDistinctEnumList(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            //var data = from d in marsEntities.T_TEST_ENUM
            List<string> data = marsEntities.T_TEST_ENUM.Select(d => d.ENUM_VALUE).Distinct().ToList();
            data.Sort();
            return data;
        }
    }
}
