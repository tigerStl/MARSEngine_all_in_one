using Mars.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Business
{
    public class B_T_DATA_SOURCE
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_T_DATA_SOURCE));

        private static Dictionary<short?, List<T_DATA_SOURCEDTO>> cachedDataSource=null;

        public Dictionary<short?, List<T_DATA_SOURCEDTO>> getDataSource(string strDBIdx, ref bool isOk ,ref string strError, ref string strStack, bool isRefresh = false)
        {
            Logger.logBegin("getDataSource", $"dbidx:{strDBIdx}");
            try
            {
                if ((isRefresh) || (cachedDataSource == null))
                {
                    try
                    {
                        Model.MarsEntities marsEntities = DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                        var d = from p in marsEntities.T_DATA_SOURCE
                                where p.DATA_SOURCE_TYPE!=null
                                select p;
                        var l = d.ToList();
                        var z = d.GroupBy(p => p.DATA_SOURCE_TYPE, x => x)
                            .ToDictionary(p => p.Key, x => x.OrderBy(zx => zx.DATA_SOURCE_NAME).ToDTOs());
                        cachedDataSource = z;
                        isOk = true;
                        return cachedDataSource;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("getDataSource", strError = e.Message, strStack = e.StackTrace);
                        isOk = false;
                        return null;
                    }
                }
                else
                {
                    isOk = true;
                    return cachedDataSource;
                }
            }
            finally
            {
                Logger.logEnd("getDataSource");
            }
        }
    }
}
