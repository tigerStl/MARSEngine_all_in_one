
using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
#endif
using System;
using System.Collections.Generic;
using System.Linq;

#if _pythonInterface
using logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    public class B_V_TEST_STEPS_FULLVISIONDTO : V_TEST_STEPS_FULLVISIONDTO
    {
#if !_pythonInterface
        private static MLogger logger = MLogger.GetLogger(typeof(B_V_TEST_STEPS_FULLVISIONDTO));
#endif

#if !v_useNameId
#if _forWebSvc
        internal static IList<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseID(long iTestCaseID)
        {
            logger.logBegin("GetTestStepsByTestCaseID");
            
            MarsDataAccessLayer<V_TEST_STEPS_FULLVISION> objMarData = new MarsDataAccessLayer<V_TEST_STEPS_FULLVISION>();
            List<V_TEST_STEPS_FULLVISION> lstRslt = objMarData.GetList(p => p.TEST_CASE_ID == iTestCaseID).OrderBy(p=>p.RUN_ORDER).ToList<V_TEST_STEPS_FULLVISION>();
            logger.Info("GetTestStepsByTestCaseID", string.Format("Get steps from testcase id[{0}], numbers:[{1}]",iTestCaseID, lstRslt==null?0:lstRslt.Count));
            
            logger.logEnd("GetTestStepsByTestCaseID");
            return V_TEST_STEPS_FULLVISIONAssembler.ToDTOs(lstRslt);
        }
#endif
#else
#if _forWebSvc
        public List<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseID(long iTestCaseID, long lTargetAppId,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        public static List<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseID(long iTestCaseID, long lTargetAppId,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            //logger.logBegin("GetTestStepsByTestCaseID");
            logger.Info("GetTestStepsByTestCaseID", string.Format("iTestCaseId:[{0}], targetAppId:[{1}]", iTestCaseID, lTargetAppId));

            MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);// new MarsEntities();
            var vt = (from q in dbCntx.V_TEST_STEPS_FULLVISION
                      where q.TEST_CASE_ID == iTestCaseID
                      && ((q.APPLICATION_ID == -1) || (q.APPLICATION_ID == lTargetAppId))
                      select q).OrderBy(p => p.RUN_ORDER);


            //MarsDataAccessLayer < V_TEST_STEPS_FULLVISION > objMarData = new MarsDataAccessLayer<V_TEST_STEPS_FULLVISION>();
            //List<V_TEST_STEPS_FULLVISION> lstRslt = objMarData.GetList(p => (p.TEST_CASE_ID == iTestCaseID) && ((p.APPLICATION_ID==-1)||(p.APPLICATION_ID==lTargetAppId))).OrderBy(p => p.RUN_ORDER).ToList<V_TEST_STEPS_FULLVISION>();
            List<V_TEST_STEPS_FULLVISION> lstRslt = vt.ToList();
            logger.Info("GetTestStepsByTestCaseID", string.Format("Get steps from testcase id[{0}], numbers:[{1}]", iTestCaseID, lstRslt == null ? 0 : lstRslt.Count));

            List<V_TEST_STEPS_FULLVISIONDTO> lstRsltDto = V_TEST_STEPS_FULLVISIONAssembler.ToDTOs(lstRslt);
            logger.Info("GetTestStepsByTestCaseID", string.Format("Total Steps' DTO:[{0}]", lstRsltDto.Count));
            logger.logEnd("GetTestStepsByTestCaseID");
            return lstRsltDto;
        }

        public bool GetTestStepsByTestId(
            string strDBIdx, 
            long lTestCaseId, 
            ref List<V_TEST_STEPS_FULLVISIONDTO> lstStps, 
            ref string strError, 
            List<long> applicationId = null)
        {
            logger.Info("GetTestStepsByTestId", string.Format("Test Case Id:[{0}]", lTestCaseId));
            try
            {
                lstStps = null;
                MarsEntities objDBCnt = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if (applicationId == null)
                {
                    var stp = from s in objDBCnt.V_TEST_STEPS_FULLVISION
                              where s.TEST_CASE_ID == lTestCaseId
                              orderby s.RUN_ORDER, s.APPLICATION_ID
                              select s;
                    if (stp == null) return true;
                    lstStps = V_TEST_STEPS_FULLVISIONAssembler.ToDTOs(stp.ToList());
                }
                else
                {
                    var stp = from s in objDBCnt.V_TEST_STEPS_FULLVISION
                              where s.TEST_CASE_ID == lTestCaseId
                              && ((s.APPLICATION_ID == -1) || (applicationId.Contains(s.APPLICATION_ID ?? -1)))
                              orderby s.RUN_ORDER, s.APPLICATION_ID
                              select s;
                    if (stp == null) return true;
                    lstStps = V_TEST_STEPS_FULLVISIONAssembler.ToDTOs(stp.ToList());
                }
                return true;

            }
            catch (Exception e)
            {
                logger.Error("GetTestStepsByTestId", strError = string.Format("Exception when get Test Step data.\r\n{0}", e.Message), e);
                return false;
            }
        }

        public static List<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseName(
            string strDBIdx, 
            string testCaseName, 
            ref string strError, 
            ref bool isOk)
        {
            logger.logBegin("GetTestStepsByTestCaseName", string.Format("Test case required:[{0}]", testCaseName));

            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var q = (from p in dbCntx.V_TEST_STEPS_FULLVISION
                         where p.TEST_CASE_NAME == testCaseName
                         select p).OrderBy(p => p.RUN_ORDER).ToDTOs();
                isOk = true;
                return q;
            }
            catch (Exception e)
            {
                logger.Error("GetTestStepsByTestCaseName", strError = string.Format("Exception:[{0}] \r\n{1}", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
            finally
            {
                logger.logEnd("GetTestStepsByTestCaseName");
            }
        }
#endif
    }
}
