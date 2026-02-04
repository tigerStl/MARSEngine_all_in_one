using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System.Collections.Generic;
using System.Linq;

namespace Mars.Business
{
    public class B_OBJECT_APPS : V_OBJECT_APPSDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_OBJECT_APPS));
        public List<V_OBJECT_APPSDTO> GetAppObjectsByAppShortName(string strDBIdx,string strAPPName)
        {
            Logger.Info("GetAppObjectsByAppShortName", string.Format("Try to get objects by AppShortName:[{0}]", strAPPName));
            MarsEntities objCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from v_o in objCntx.V_OBJECT_APPS
                        where v_o.APP_SHORT_NAME == strAPPName
                        select v_o;
            return V_OBJECT_APPSAssembler.ToDTOs(query).OrderBy(p => p.OBJECT_TYPE).ThenBy(p => p.OBJECT_HAPPY_NAME).ToList();
        }
    }
}
