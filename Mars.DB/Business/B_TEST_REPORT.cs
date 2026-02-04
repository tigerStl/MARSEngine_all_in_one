using Mars.message.DataLayer;

using Mars.message.Dto;
using Mars.Model;

using System;
using System.Collections.Generic;
using System.Linq;

#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
using Mars.message.DataLayer.Generic;
#endif
#if !_noEntities
using System.Data.Entity;
using System.Data.Entity.Validation;
#endif
#if _marsLog
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    public class B_TEST_REPORT : T_TEST_REPORTDTO
    {
#if !_marsLog
        private MLogger Logger = MLogger.GetLogger(typeof(B_TEST_REPORT));
#endif
        /// <summary>
        /// Long of the dictionary is step id
        /// </summary>
        private Dictionary<long, B_TEST_REPORT_STEPS> subTestStepsRpt = new Dictionary<long, B_TEST_REPORT_STEPS>();
        public int Create2Entities(ref string strError, string strDBIdx ) //= MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("Save2Entities");
            /// Steps 
            /// 1, get new Id
            /// 2, save to DB
            ///             
#if !_forWebClient
            return (new BoHelper()).CreateTestReportLog(this, ref strError,strDBIdx);
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            bool isOk = false;
            string strAdv = "";
            string strStack = "";
            
            int iCnt = clnt.testReport_CreateTestReportLog(this, ref isOk, ref strError, ref strAdv, ref strStack);
            if (!isOk)
            {
                Logger.Error("Create2Entities", $"testReport_CreateTestReportLog return's Error:{strError}, \r\n\t{strStack}");
                if (string.IsNullOrEmpty(strError))
                    strError = "Can't create test report, and no error returns";
                return -1;
            }
            return iCnt;
#endif

        }
#if !_noEntities
        public int update(string strDBIdx) //= MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("update");
            MarsDataAccessLayer<T_TEST_REPORT> objDBAcss = new MarsDataAccessLayer<T_TEST_REPORT>(strDBIdx);
            return objDBAcss.AddSingle(this.ToEntity());
        }
#endif
        public void AddOrUpdateTestSteptsRpt(B_TEST_REPORT_STEPS objStp)
        {
            if (objStp.STEPS_ID == null)
            {
                Logger.Warnning("AddOrUpdateTestSteptsRpt", "STEPS_ID IS null, FOR current Step Info");
                return;
            }
            subTestStepsRpt.Add((long)objStp.STEPS_ID, objStp);
        }

        public B_TEST_REPORT_STEPS GetTestRptStepsByTestStepId(long stepId)
        {
            B_TEST_REPORT_STEPS objRslt = null;
            if (subTestStepsRpt.TryGetValue(stepId, out objRslt))
            {
                return objRslt;
            }
            return null;
        }

        public int updateById(ref string strError, string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
        {
#if !_forWebClient
            MarsDataAccessLayer<T_TEST_REPORT> objDBAcss = new MarsDataAccessLayer<T_TEST_REPORT>(strDBIdx);
            objDBAcss.updateCurrentSingle += cloneContentTo;
            int iReulst = objDBAcss.UpdateSingle(p => p.TEST_REPORT_ID == this.TEST_REPORT_ID, ref strError);

            //T_TEST_REPORT objRpt = objDBAcss.GetSingle(p => p.TEST_REPORT_ID == this.TEST_REPORT_ID);
            //if (objRpt==null)
            //{
            //    Logger.Error("updateById", strError = string.Format("No such Id:[{0}] returns from T_TEST_REPORT", this.TEST_REPORT_ID));
            //    return -1;
            //}
            //cloneContentTo(objRpt);
            //objDBAcss.UpdateSingle();

            return iReulst;
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            bool isOk = false;
            int iCnt = clnt.testReport_updateById(this, ref isOk, ref strError);
            if (!isOk)
            {
                if (string.IsNullOrEmpty(strError))
                    strError = "Can't save test report, and no error returns";
                return -1;
            }
            return iCnt;
#endif
        }

        private int cloneContentTo(T_TEST_REPORT objRpt)
        {
            Logger.logBegin("cloneContentTo", string.Format("time:[{0}] TEST_CASE_ID-[{1}]", this.BEGIN_TIME, this.TEST_CASE_ID));
            try
            {

                if (objRpt == null) return -1;
                objRpt.APPLICATION_ID = this.APPLICATION_ID;
                objRpt.BEGIN_TIME = this.BEGIN_TIME;
                objRpt.END_TIME = this.END_TIME;
                objRpt.HIST_ID = this.HIST_ID;
                objRpt.LOOP_ID = this.LOOP_ID;
                objRpt.RETURN_VALUES = this.RETURN_VALUES;
                objRpt.RUNNING_RESULT = this.RUNNING_RESULT;
                objRpt.RUNNING_RESULT_INFO = this.RUNNING_RESULT_INFO;
                objRpt.TEST_CASE_ID = this.TEST_CASE_ID;
                objRpt.TEST_MODE = this.TEST_MODE;
                return 1;
            }
            finally
            {
                Logger.logEnd("cloneContentTo");
            }

        }
        public B_TEST_REPORT CreateFrom(T_TEST_REPORTDTO rptDto)
        {
            if (rptDto == null) return null;
            B_TEST_REPORT rslt = new B_TEST_REPORT();
            rslt.APPLICATION_ID = rptDto.APPLICATION_ID;
            rslt.BEGIN_TIME = rptDto.BEGIN_TIME;
            rslt.END_TIME = rptDto.END_TIME;
            rslt.HIST_ID = rptDto.HIST_ID;
            rslt.LOOP_ID = rptDto.LOOP_ID;
            rslt.RETURN_VALUES = rptDto.RETURN_VALUES;
            rslt.RUNNING_RESULT = rptDto.RUNNING_RESULT;
            rslt.RUNNING_RESULT_INFO = rptDto.RUNNING_RESULT_INFO;
            rslt.TEST_CASE_ID = rptDto.TEST_CASE_ID;
            rslt.TEST_MODE = rptDto.TEST_MODE;
            rslt.TEST_REPORT_ID = rptDto.TEST_REPORT_ID;

            return rslt;
        }

        public void CloneFrom(T_TEST_REPORTDTO rptDto)
        {
            if (rptDto == null) return;
            this.APPLICATION_ID = rptDto.APPLICATION_ID;
            this.BEGIN_TIME = rptDto.BEGIN_TIME;
            this.END_TIME = rptDto.END_TIME;
            this.HIST_ID = rptDto.HIST_ID;
            this.LOOP_ID = rptDto.LOOP_ID;
            this.RETURN_VALUES = rptDto.RETURN_VALUES;
            this.RUNNING_RESULT = rptDto.RUNNING_RESULT;
            this.RUNNING_RESULT_INFO = rptDto.RUNNING_RESULT_INFO;
            this.TEST_CASE_ID = rptDto.TEST_CASE_ID;
            this.TEST_MODE = rptDto.TEST_MODE;
            this.TEST_REPORT_ID = rptDto.TEST_REPORT_ID;
        }
#if !_noEntities
        public long? getLatestTestAppIdBy(string strDBIdx, long stryBrdId, int iTestMode, ref bool isOk, ref string strError)
        {
            Logger.logBegin("getLatestTestAppIdBy", string.Format("StoryboardId:[{0}], TestMode:[{1}]", stryBrdId, iTestMode));
            try
            {
                MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var q = from strybrd in objDBCntx.T_PROJ_TC_MGR
                        from tstRslt in objDBCntx.T_PROJ_TEST_RESULT
                        from rpt in objDBCntx.T_TEST_REPORT
                        where strybrd.STORYBOARD_ID == stryBrdId
                        && strybrd.STORYBOARD_DETAIL_ID == tstRslt.STORYBOARD_DETAIL_ID
                        && tstRslt.TEST_MODE == iTestMode
                        && tstRslt.HIST_ID == rpt.HIST_ID
                        select rpt.APPLICATION_ID
                        ;
                List<long?> lstData = q.ToList();
                long? lrslt = lstData.FirstOrDefault();
                isOk = true;
                return lrslt;// lrslt.HasValue?lrslt.Value:-1;
            }
            catch (Exception e)
            {
                Logger.Error("getLatestTestAppIdBy", strError = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return -1;
            }
            finally
            {
                Logger.logEnd("getLatestTestAppIdBy");
            }
        }
#endif
    }
}
