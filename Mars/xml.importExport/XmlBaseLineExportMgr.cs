using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars.xml.importExport
{
    public class XmlBaseLineExportImportMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(XmlBaseLineExportImportMgr));
        private string currentStoryboardName;
        public string CurrentStoryboardName { get { return currentStoryboardName; }
            internal set
            { currentStoryboardName = value; }
        }

        public string TestMode { get; set; }

        public long CurrentStoryboardId { get; set; }
        private Dictionary<long, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> currentDataToExp;
        public long? LatestTestAppliationId { get; set; }

        private List<MarsXmlDetailData> ConvertToXmlExport(ref bool isOk, ref string strError)
        {
            if (currentDataToExp==null)
            {
                isOk = false;
                strError = "Data is not fetched";
                return null;
            }

            try
            {
                List<MarsXmlDetailData> lstResult = new List<MarsXmlDetailData>();
                foreach (long lDtlId in currentDataToExp.Keys)
                {
                    Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> dicData = currentDataToExp[lDtlId];
                    if (dicData == null) continue;
                    MarsXmlDetailData crntDtlData = new MarsXmlDetailData();
                    crntDtlData.StoryboardDetailId = lDtlId;

                    crntDtlData.StepsData.Clear();

                    ///actually, only one loop value 
                    foreach (int iLoop in dicData.Keys)
                    {
                        List<V_TEST_DATA_REPORT_SUMMARYDTO> resultDataForLoop = dicData[iLoop];
                        if (resultDataForLoop == null) continue;
                        for (int iData = 0; iData < resultDataForLoop.Count; iData++)
                        {
                            MarsStepTestResultXmlNode objXmlStpData = new MarsStepTestResultXmlNode();
                            objXmlStpData.assignedDBObject = resultDataForLoop[iData];
                            crntDtlData.StepsData.Add(objXmlStpData);
                        }
                    }
                    lstResult.Add(crntDtlData);

                }
                isOk = true;
                return lstResult;
            }
            catch (Exception e)
            {
                Logger.Error("ConvertToXmlExport", strError = string.Format("Exception:[{0}]",e.Message), e);
                isOk = false;
                return null;
            }
        }

        internal bool ExportBaselineDataByStoryBoardIds(List<long> lstStryBrdDtlToExport, string strDesFileName,ref string strError)
        {
            Logger.logBegin("ExportBaselineDataByStoryBoardIds",string.Format("File to import:[{0}]",strDesFileName));
            try
            {
                MarsXmlBaseLineDataFileForStoryboard objBaseLineDataXml = new MarsXmlBaseLineDataFileForStoryboard();
                objBaseLineDataXml.CurrentStoryboardName = this.currentStoryboardName;
                objBaseLineDataXml.StoryboardId = CurrentStoryboardId;
                objBaseLineDataXml.TestMode = TestMode;
                objBaseLineDataXml.ApplicationId = this.LatestTestAppliationId;

                ///将数据库数据转变为xml格式
                /// 
                if (currentDataToExp==null)
                {
                    Logger.Error("ExportBaselineDataByStoryBoardIds", strError = "No data fetched. ");
                    return false;
                }

                bool isOk=false;
                objBaseLineDataXml.DetailExportedData = ConvertToXmlExport(ref isOk, ref strError);

                isOk = objBaseLineDataXml.ExportXmlFileTo(strDesFileName, lstStryBrdDtlToExport, ref strError);
                if (!isOk)
                {
                    Logger.Error("ExportBaselineDataByStoryBoardIds",strError);
                }
                return isOk;
            }
            catch (Exception e)
            {
                Logger.Error("ExportBaselineDataByStoryBoardIds",strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("ExportBaselineDataByStoryBoardIds");
            }
        }

        

        internal bool InitDataFromDataBase(List<long> lstTestStoryboardId,ref string strError)
        {
            Logger.logBegin("InitDataFromDataBase");
            try
            {
                bool isOk = false;
                B_V_TEST_DATA_REPORT_SUMMARY objData = new B_V_TEST_DATA_REPORT_SUMMARY();
                currentDataToExp = null;
                currentDataToExp = objData.getTestStpReportDataByTestStoryBoardIds(MarsMainWindow.CurrentDatabaseIdx, lstTestStoryboardId, 1, ref isOk, ref strError);
                if (!isOk)
                {
                    return false;
                }

                B_TEST_REPORT objRstl = new B_TEST_REPORT();
                LatestTestAppliationId = objRstl.getLatestTestAppIdBy(MarsMainWindow.CurrentDatabaseIdx, this.CurrentStoryboardId, 1, ref isOk, ref strError);

                return isOk;
                /// get All test story board detail's data 
            }catch(Exception e)
            {
                Logger.Error("InitDataFromDataBase",strError= string.Format("Exception:[{0}]", e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("InitDataFromDataBase");
            }
        }

        

        internal bool ImportBaselineDataFromFile(long currentStorybardId,List<long> lstDtlIds, string strFileName, ref string strError)
        {
            Logger.logBegin("ImportBaselineDataFromFile",string.Format("FileName:[{0}]",strFileName));
            try
            {
                bool isOk = false;
                MarsXmlBaseLineDataFileForStoryboard objBaseLineDataXml = MarsXmlBaseLineDataFileForStoryboard.LoadDataFromFile(CurrentStoryboardId,strFileName,ref isOk, ref strError);
                if ((!isOk)||(objBaseLineDataXml==null))
                {
                    return false;
                }

                //load data to database
                ///算法：
                /// 1，判断是否所有的test storyboard detail id存在
                /// 2，创建有个新的testresuslt id，导入数据
                /// 
                List<long> lstDtlIdsFromXml = objBaseLineDataXml.DetailExportedData.Select(p => p.StoryboardDetailId).Distinct().ToList();
                List<long> lstNonDtlId = lstDtlIdsFromXml.Where(p => !lstDtlIds.Any(d => d == p)).ToList();
                if ((lstNonDtlId!=null)&&(lstNonDtlId.Count>0))
                {
                    strError = string.Format("Storyboards [{0}] information are missing incurrent storyboard id.", lstNonDtlId);
                    return false;
                }

                ///2，
                /// 
                
                isOk = objBaseLineDataXml.ImportDataIntoDB( ref strError);
                if (!isOk)
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ImportBaselineDataFromFile",strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("ImportBaselineDataFromFile");
            }
        }
    }

    [Serializable]
    [XmlRoot(ElementName = "MarsBaselineDataExportRoot")]
    public partial class MarsXmlBaseLineDataFileForStoryboard
    {
        #region private
        public static MLogger Logger = MLogger.GetLogger(typeof(MarsXmlBaseLineDataFileForStoryboard));
        #endregion


        #region const defined
        private const string cnst_ExportDate = "ExportDateTime";
        private const string cnst_BaseLine4Detail = "Storyboard_Detail";
        private const string cnst_TestMode = "TestMode";
        private const string cnst_CurrentStoryboardName = "StoryBoardName";
        private const string cnst_CurrentStoryboardId = "StoryBoardId";
        private const string cnst_ApplicationId = "ApplicationId";
        #endregion //const defined


        private DateTime exportDateTime;
        [XmlElement(ElementName = cnst_ExportDate)]
        public DateTime ExportDateTime
        {
            get {
                return exportDateTime;
            }
            set {
                exportDateTime = value;
            }
        }

        [XmlAttribute(AttributeName = cnst_CurrentStoryboardId)]
        public long StoryboardId
        {
            get;
            set;
        }

        [XmlElement(ElementName = cnst_CurrentStoryboardName)]
        public string CurrentStoryboardName { get; set; }

        private List<MarsXmlDetailData> detailExportedData;
        [XmlElement(ElementName= cnst_BaseLine4Detail)]
        public List<MarsXmlDetailData> DetailExportedData
        {
            get {
                return detailExportedData;
            }
            set {
                detailExportedData = value;
            }
        }

        private long? applicationId;
        [XmlElement(ElementName =cnst_ApplicationId)]
        public long? ApplicationId
        {
            get
            {
                return applicationId;
            }
            set
            {
                applicationId = value ;
            }
        }

        public long GetTestModeAsLong()
        {
            return testModeFromDB;
        }


        private long testModeFromDB;
        [XmlElement(ElementName= cnst_TestMode)]
        public string TestMode
        {
            get
            {
                return testModeFromDB == 1 ? "BASELINE" : "NON-BASELINE";
            }
            set
            {
                if (string.Compare("BASELINE", value, true)==0)
                {
                    testModeFromDB = 1;
                }
                else
                {
                    testModeFromDB = 0;
                }
            }
        }

        internal bool ExportXmlFileTo(string strDesFileName, List<long> lstStryBrdDtlToExport, ref string strError)
        {
            Logger.logBegin("ExportXmlFileTo",string.Format("Des FileName:[{0}]",strDesFileName));
            try
            {
                //if (File.Exists(strDesFileName))
                //{
                //    File.Delete(strDesFileName);
                //}
                FileStream fs = new FileStream(strDesFileName, FileMode.OpenOrCreate, FileAccess.Write);
                XmlSerializer objXmlSerializer = new XmlSerializer(typeof(MarsXmlBaseLineDataFileForStoryboard));
                objXmlSerializer.Serialize(fs, this);
                fs.Close();                
                return true;
            }catch(Exception e)
            {
                Logger.Error("ExportXmlFileTo", strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("ExportXmlFileTo");
            }
        }

        static internal MarsXmlBaseLineDataFileForStoryboard LoadDataFromFile(long currentStoryboardId, string strFileName,ref bool isOk,  ref string strError)
        {
            Logger.logBegin("LoadDataFromFile",string.Format("StoryboardId:[{0}] FileName:[{1}]",currentStoryboardId, strFileName ));
            try
            {
                XmlSerializer objXmlSerializer = new XmlSerializer(typeof(MarsXmlBaseLineDataFileForStoryboard));
                var objData = objXmlSerializer.Deserialize(new FileStream(strFileName, FileMode.Open, FileAccess.Read));
                MarsXmlBaseLineDataFileForStoryboard objResult = objData as MarsXmlBaseLineDataFileForStoryboard;
                if (objResult==null)
                {
                    strError = string.Format("No data return for file:[{0}]", strFileName);
                    isOk = false;
                    return null;
                }
                isOk = true;
                return objResult;
            }
            catch (Exception e)
            {
                Logger.Error("LoadDataFromFile",strError=string.Format("Exception:[{0}]",e.Message),e);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("LoadDataFromFile");
            }
        }

        #region Ignore property

        #endregion //Ignore property

    }

    public class MarsXmlDetailData
    {
        #region const
        private const string cnst_storyboard_detail_id = "Storyboard_detail_id";
        private const string cnst_stepsdata = "StepData";
        #endregion //const

        private long storyboardDetailId;
        [XmlElement(ElementName = cnst_storyboard_detail_id)]
        public long StoryboardDetailId
        {
            get
            {
                return storyboardDetailId;
            }

            set
            {
                storyboardDetailId = value;
            }
        }

        private List<MarsStepTestResultXmlNode> stepsData;
        [XmlElement(cnst_stepsdata)]
        public List<MarsStepTestResultXmlNode> StepsData
        {
            get
            {
                return stepsData==null? (stepsData=new List<MarsStepTestResultXmlNode>()):stepsData;
            }

            set
            {
                stepsData = value;
            }
        }
    }

    public class MarsStepTestResultXmlNode
    {
        #region const
        private const string cnst_steps_id = "Step_Id";
        private const string cnst_object_name = "ObjectName";
        private const string cnst_key_word = "KeyWord";
        private const string cnst_value = "DataValue";
        #endregion

        [XmlIgnore]
        public V_TEST_DATA_REPORT_SUMMARYDTO assignedDBObject = null;

        private void AssignedObjCheck()
        {
            if (assignedDBObject == null)
                assignedDBObject = new V_TEST_DATA_REPORT_SUMMARYDTO();
        }

        [XmlAttribute(AttributeName = cnst_steps_id)]
        public long TestStepId
        {
            get
            {
                AssignedObjCheck();
                return assignedDBObject.STEPS_ID;
            }
            set
            {
                AssignedObjCheck();
                assignedDBObject.STEPS_ID = value;
            }
        }
        [XmlAttribute(AttributeName= cnst_key_word)]
        public string Keyword
        {
            get
            {
                AssignedObjCheck();
                return this.assignedDBObject.KEY_WORD_NAME;
            }
            set
            {
                AssignedObjCheck();
                this.assignedDBObject.KEY_WORD_NAME = value;
            }
        }

        [XmlElement(ElementName = cnst_object_name)]
        public string ObjectName
        {
            get
            {
                AssignedObjCheck();
                if ((string.Compare("CAPTUREVALUE",this.assignedDBObject.KEY_WORD_NAME, true)==0)
                    || (string.Compare("CaptureAndCompare", this.assignedDBObject.KEY_WORD_NAME, true) == 0)
                    || (string.Compare("CaptureAndCompareByKey", this.assignedDBObject.KEY_WORD_NAME, true) == 0)                    
                    )
                    
                {
                    return this.assignedDBObject.INPUT_VALUE_SETTING;
                }
                return this.assignedDBObject.OBJECT_HAPPY_NAME;
            }
            set
            {
                AssignedObjCheck();
                if ((string.Compare("CAPTUREVALUE", this.assignedDBObject.KEY_WORD_NAME, true) == 0) 
                    || (string.Compare("CaptureAndCompare", this.assignedDBObject.KEY_WORD_NAME, true) == 0)
                    || (string.Compare("CaptureAndCompareByKey", this.assignedDBObject.KEY_WORD_NAME, true) == 0)
                    )
                {
                    this.assignedDBObject.INPUT_VALUE_SETTING = value ;
                }
                this.assignedDBObject.OBJECT_HAPPY_NAME = value ;
            }
        }

        [XmlElement(ElementName = cnst_value)]
        public string DataValue
        {
            get {
                AssignedObjCheck();
                return this.assignedDBObject.RETURN_VALUES;
            }
            set
            {
                AssignedObjCheck();
                this.assignedDBObject.RETURN_VALUES = value;
            }
        }


    }

    public static partial class XmlImportExtension
    {
        public static bool ImportDataIntoDB(this MarsXmlBaseLineDataFileForStoryboard objInst, ref string strError)
        {
            MarsXmlBaseLineDataFileForStoryboard.Logger.logBegin("ImportDataIntoDB");
            try
            {
                ///算法：
                /// 1，将xmlobject转换成dto
                /// 

                /// 1，将xmlobject转换成dto
                /// 
                Dictionary<long, List<V_TEST_DATA_REPORT_SUMMARYDTO>> lstRptObjs = new Dictionary<long, List<V_TEST_DATA_REPORT_SUMMARYDTO>>();
                foreach (var itm in objInst.DetailExportedData)
                {
                    if (itm == null) continue;
                    var lstAssigned = itm.StepsData.Select(p => p.assignedDBObject);
                    lstRptObjs.Add(itm.StoryboardDetailId, lstAssigned.ToList());
                }

                bool isOk = BoHelper.ImportDataFromXmlObjForTestResult(MarsMainWindow.CurrentDatabaseIdx, 
                    objInst.StoryboardId, objInst.ApplicationId, lstRptObjs, objInst.GetTestModeAsLong(), ref strError);
                if (!isOk)
                {
                    MarsXmlBaseLineDataFileForStoryboard.Logger.Error("ImportDataIntoDB", strError);
                    return false;
                }
                return isOk;
            }
            finally
            {
                MarsXmlBaseLineDataFileForStoryboard.Logger.logEnd("ImportDataIntoDB");
            }

            
        }
    }

}
