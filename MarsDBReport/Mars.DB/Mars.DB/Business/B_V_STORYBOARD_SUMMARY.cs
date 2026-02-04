using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Linq;

namespace Mars.Business
{
    public class B_V_TESTSTORYBOARD_SUMMARY : V_TESTSTORYBOARD_SUMMARYDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_V_TESTSTORYBOARD_SUMMARY));

        public V_TESTSTORYBOARD_SUMMARYDTO getSummaryInfoByStoryBoardId(string strDBIdx, long lStoryBoardId)
        {
            Logger.Info("getSummaryInfoByStoryBoardId", string.Format("Try to get Summary info ByID:[{0}]", lStoryBoardId));
            try
            {
                MarsEntities objDBContext = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var query = from storySum in objDBContext.V_TESTSTORYBOARD_SUMMARY
                            where storySum.STORYBOARD_ID == lStoryBoardId
                            select storySum;
                var objResult = query.FirstOrDefault();
                if (objResult == null) return null;
                return V_TESTSTORYBOARD_SUMMARYAssembler.ToDTO(objResult);
            }
            catch (Exception e)
            {
                Logger.Error("getSummaryInfoByStoryBoardId", string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return null;
            }

        }
    }
}
