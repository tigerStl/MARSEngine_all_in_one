using Mars.DataLayer;
using Mars.DataLayer.Generic;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.Business
{
    internal class B_V_STB_PROJ_APP_FULLVISION
    {
        private static MLogger logger = MLogger.GetLogger(typeof(B_V_STB_PROJ_APP_FULLVISION));

        public static IList<V_STB_PROJ_APP_FULLVISIONDTO> GetAllByIds(Int64? iId, string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
        {
            logger.logBegin("GetAllByIds");
            IList<V_STB_PROJ_APP_FULLVISION> lstRslt;
            MarsDataAccessLayer<V_STB_PROJ_APP_FULLVISION> objMarsData = new MarsDataAccessLayer<V_STB_PROJ_APP_FULLVISION>(strDBIdx);
            MarsEntities objEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (iId == null)
            {
                var a = from q in objEntities.V_STB_PROJ_APP_FULLVISION
                        orderby new { q.STORYBOARD_ID, q.APP_SHORT_NAME }
                        select q;

                lstRslt = a.ToList();// (IList<V_STB_PROJ_APP_FULLVISION>)(objMarsData.GetAll().OrderBy(p => p.PROJECT_ID).ThenBy(p => p.STORYBOARD_ID).ThenBy(p => p.APP_SHORT_NAME).ToList<V_STB_PROJ_APP_FULLVISION>());
            }
            else
            {
                var l = from q in objEntities.V_STB_PROJ_APP_FULLVISION
                        where q.STORYBOARD_ID == iId
                        orderby new { q.PROJECT_ID, q.STORYBOARD_ID, q.APP_SHORT_NAME }
                        select q;
                lstRslt = l.ToList();// (IList<V_STB_PROJ_APP_FULLVISION>)(objMarsData.GetList(p => p.STORYBOARD_ID == iId, null).OrderBy(p => p.PROJECT_ID).ThenBy(p => p.STORYBOARD_ID).ThenBy(p => p.APP_SHORT_NAME).ToList<V_STB_PROJ_APP_FULLVISION>());

            }

            logger.logEnd("GetAllByIds");
            return V_STB_PROJ_APP_FULLVISIONAssembler.ToDTOs(lstRslt);
        }


    }
}
