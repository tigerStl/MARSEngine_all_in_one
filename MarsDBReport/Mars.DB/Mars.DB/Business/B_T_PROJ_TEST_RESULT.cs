

using Mars.Dto;
using Mars.Model;
using System.Collections.Generic;
using System.Linq;

namespace Mars.Business
{
    public class B_T_PROJ_TEST_RESULT : T_PROJ_TEST_RESULTDTO
    {
        public List<T_PROJ_TEST_RESULTDTO> GetProjTestResult(string strDBIdx)
        {
            MarsEntities objDBCntx = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var result = (from res in objDBCntx.T_PROJ_TEST_RESULT
                          select res).ToDTOs().ToList();

            return result;

        }
    }
}
