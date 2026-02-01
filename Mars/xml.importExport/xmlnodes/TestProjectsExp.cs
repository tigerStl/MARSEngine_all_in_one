using Mars.Business;
using Mars.Dto;
using Mars.ViewModel;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Mars.Model;
using System.Collections.ObjectModel;

namespace Mars.xml.importExport.xmlnodes
{
    [XmlRoot(ElementName = MarsImpExpConst.cnst_project_root)]
    public class MarsprojectInformationToExport:Notify
    {
        
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsprojectInformationToExport));
        [XmlIgnore]
        public long currentProjectId { get; set; }

        private string projectName;
        [XmlElement(ElementName = MarsImpExpConst.cnst_project_name)]
        public string ProjectName {
            get { return projectName; }
            set {
                projectName = value;
                OnPropertyChanged("ProjectName");
            } }

        private string projectDesc;
        [XmlElement(ElementName = MarsImpExpConst.cnst_project_desc)]
        public string ProjectDesc { get { return projectDesc; }
            set
            {
                projectDesc = value;
                OnPropertyChanged("ProjectDesc");
            }
        }
        //系统产生的导出记录，包括导出时间等
        [XmlElement(ElementName =MarsImpExpConst.cnst_project_exp_desc)]
        public string ProjectExportDesc { get; set; }

        private ObservableCollection<MarsProjExpAppInfo> assignedApplications;
        [XmlArray(ElementName = MarsImpExpConst.cnst_project_rel_app)]
        [XmlArrayItem(ElementName = MarsImpExpConst.cnst_project_app)]
        public ObservableCollection<MarsProjExpAppInfo> AssignedApplications
        {
            get {
                return assignedApplications;
            }
            set
            {
                if (assignedApplications!=value)
                {
                    assignedApplications = value;
                    OnPropertyChanged("AssignedApplications");
                }
            }
        }

        private ObservableCollection<MarsProjExpTestSuiteInfo> assignedTestSuites;
        [XmlArray(ElementName = MarsImpExpConst.cnst_project_rel_testsuites)]
        [XmlArrayItem(ElementName = MarsImpExpConst.cnst_project_rel_testsuite)]
        public ObservableCollection<MarsProjExpTestSuiteInfo> AssignedTestSuites { get { return assignedTestSuites; }
            set {
                if (value == null)
                {
                    assignedTestSuites = null;
                }
                else
                {
                    ///keep data unique
                    /// 
                    if (assignedTestSuites == null)
                        assignedTestSuites = new ObservableCollection<MarsProjExpTestSuiteInfo>();
                    foreach (var itm in value)
                    {
                        if (assignedTestSuites.Any(p => p.TestSuiteId == itm.TestSuiteId)) continue;
                        assignedTestSuites.Add(itm);
                    }
                    OnPropertyChanged("AssignedTestSuites");
                }
            }
        }
        private ObservableCollection<MarsProjExpStoryboardInfo> assignedStoryboards;
        [XmlArray(ElementName = MarsImpExpConst.cnst_project_storyboard_root)]
        [XmlArrayItem(ElementName = MarsImpExpConst.cnst_project_storyboard)]
        public ObservableCollection<MarsProjExpStoryboardInfo> AssignedStoryboards
        {
            get {
                return assignedStoryboards;
            }
            set
            {
                assignedStoryboards = value;
                OnPropertyChanged("AssignedStoryboards");
            }
        }

        #region For import
        [XmlIgnore]
        internal B_TEST_PROJECT assignedProject = null;
        #endregion

        public bool InitProjectApplications(long lProjId, ref string strErrorOrHint)
        {
            Logger.logBegin("InitProjectApplications",string.Format("Project Id [{0}]", lProjId));
            try
            {
                B_TEST_PROJECT objProj = new B_TEST_PROJECT();
                T_TEST_PROJECTDTO op = objProj.GetProject(MarsMainWindow.CurrentDatabaseIdx, lProjId);
                if (op == null)
                {
                    Logger.Error("InitProjectApplications", strErrorOrHint = string.Format("Project doesn't exists anymore. projectId :[{0}]", lProjId));
                    return false;
                }

                ProjectName = op.PROJECT_NAME;
                ProjectDesc = op.PROJECT_DESCRIPTION;

                return true;
            }
            finally
            {
                Logger.logEnd("InitProjectApplications");
            }
        }

        public bool InitProjAppInfo(long lProjId, ref string strErrorOrHint)
        {
            Logger.logBegin("InitProjAppInfo", string.Format("Project Id [{0}]", lProjId));
            try
            {
                bool isOk = false;
                List<B_REL_APP_PROJ> lstRelAppProj = B_REL_APP_PROJ.GetRecInfoByProjId(
                    MarsMainWindow.CurrentDatabaseIdx,
                    lProjId, ref strErrorOrHint, ref isOk);
                this.AssignedApplications = new ObservableCollection<MarsProjExpAppInfo>( MarsProjExpAppInfo.ConverFromBussinessObject(lstRelAppProj));
                return isOk;
            }
            finally
            {
                Logger.logEnd("InitProjAppInfo");
            }
        }

        public bool InitSBInfo(long lProjId, ref string strErrorOrHint)
        {
            Logger.logBegin("InitSBInfo", string.Format("Project Id [{0}]", lProjId));
            try
            {
                B_STORYBOARD_SUMMARY objStoryBoardSum = new B_STORYBOARD_SUMMARY();
                bool isOk = false;
                //List<T_STORYBOARD_SUMMARYDTO> lstSBdto = objStoryBoardSum.GetStoryboardSumByProjId(lProjId,ref isOk,ref strErrorOrHint);
                //if (!isOk) return false;
                //if (lstSBdto == null) return true;
                B_V_STORYBOARD_TEST_FULLVISION objStoryBoardVision = new B_V_STORYBOARD_TEST_FULLVISION();
                Dictionary<T_STORYBOARD_SUMMARYDTO, List<V_STORYBOARD_TEST_FULLVISIONDTO>> lstStoryInfo = 
                    objStoryBoardVision.GetStoryboardInfoAndDetailByProjectId(MarsMainWindow.CurrentDatabaseIdx, lProjId,ref isOk,ref strErrorOrHint);
                if (!isOk) return false;

                //convert to list<MarsProjExpStoryboardInfo>
                if (lstStoryInfo == null) return true;
                if (lstStoryInfo.Keys == null) return true;
                AssignedStoryboards = new ObservableCollection<MarsProjExpStoryboardInfo>();
                foreach (T_STORYBOARD_SUMMARYDTO itm in lstStoryInfo.Keys)
                {
                    if (itm == null) continue;
                    MarsProjExpStoryboardInfo objSBInfo = new MarsProjExpStoryboardInfo();
                    objSBInfo.SetAssingedDto(itm);
                    if (lstStoryInfo[itm] == null) continue;
                    lstStoryInfo[itm].ForEach(itmDtl => {
                        if (objSBInfo.StoryboardDetailListForExp == null)
                            objSBInfo.StoryboardDetailListForExp = new ObservableCollection<MarsProjExpStoryDetailInfo>();
                        MarsProjExpStoryDetailInfo objTmpDetailInfo = new MarsProjExpStoryDetailInfo();
                        objTmpDetailInfo.SetAssignedData(B_V_STORYBOARD_TEST_FULLVISION.CopyFromDto(itmDtl));
                        long lDSId = objTmpDetailInfo.DSID;
                        //Logger.Info("InitSBInfo",string.Format("ddd:[{0}]", lDSId));
                        objSBInfo.StoryboardDetailListForExp.Add(objTmpDetailInfo);
                    });
                    objSBInfo.StoryboardDetailListForExp = new ObservableCollection<MarsProjExpStoryDetailInfo>((objSBInfo.StoryboardDetailListForExp.GroupBy(p => p.assignedStoryboardDtlInfo.STORYBOARD_DETAIL_ID).Select(p => p.FirstOrDefault())));//.ToList();
                    AssignedStoryboards.Add(objSBInfo);
                }
                return true;
            }
            catch(Exception e)
            {
                Logger.Error("InitSBInfo",strErrorOrHint= string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("InitSBInfo");
            }

        }

        public bool InitTSInfo(long lProjId, ref string strErrorOrHint)
        {
            Logger.logBegin("InitTSInfo", string.Format("Project Id [{0}]", lProjId));
            try
            {
                List<B_TEST_SUITE> lstTstSuite = (new B_TEST_SUITE()).GetTestSuiteOwneredByProj(MarsMainWindow.CurrentDatabaseIdx, lProjId);
                this.AssignedTestSuites = new ObservableCollection<MarsProjExpTestSuiteInfo>(MarsProjExpTestSuiteInfo.ConvertFromBussinessObject(lstTstSuite));
                return true;
            }catch(Exception e)
            {
                Logger.Error("InitTSInfo",strErrorOrHint = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("InitTSInfo");
            }
        }
        [XmlIgnore]
        public string TargetDirectory { get; set; }
        internal static bool FlushProjectXmlFile<T>(T inst,string strDesDir,ref string strFileName,ref string strError)
        {
            Logger.logBegin("FlushProjectXmlFile",string.Format("TargetDirectory:[{0}]", strDesDir));
            XmlSerializer xmlSubmit = new XmlSerializer(typeof(T));
            try
            {
                using (var xmlWriter = new System.IO.StreamWriter(Path.Combine(strDesDir, strFileName),false, Encoding.UTF8))
                {
                    xmlSubmit.Serialize(xmlWriter, inst);
                    xmlWriter.Flush();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("FlushProjectXmlFile",strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
        }

        internal string CreateProjectXmlExpFileName()
        {
            return string.Format("MarsProj_Exp_{0}.xml", this.currentProjectId);
        }

        internal static MarsprojectInformationToExport LoadFromXml(string strFileName,ref string strError)
        {
            Logger.logBegin("LoadFromXml", string.Format("Try to Load From Xml File:[{0}]", strFileName));
            
            try
            {
                MarsprojectInformationToExport projXmlObj = XmlHelper.XmlDeserializeFromFile<MarsprojectInformationToExport>(strFileName, Encoding.UTF8);
                return projXmlObj;
            }
            catch (Exception e)
            {
                Logger.Error("MarsprojectInformationToExport",strError = string.Format("Exception:[{0}]",e.Message),e);
                return null;
            }
            
        }

        internal long GetTSMappingIDByXmlTSId(long tsIdFromXml, ref bool isOk)
        {
            Logger.logBegin("GetTSMappingIDByXmlTSId", string.Format("Try to find TS mapping ID [{0}] From TS list.", tsIdFromXml));
            try
            {
                MarsProjExpTestSuiteInfo objTs = this.assignedTestSuites.Where(p => p.TestSuiteIdFromXml == tsIdFromXml).FirstOrDefault();
                if (objTs == null)
                {
                    isOk = false;
                    return -1;
                }
                isOk = true;
                return objTs.TestSuiteId;
            }
            finally
            {
                Logger.logEnd("GetTSMappingIDByXmlTSId");
            }
        }

        internal long GetTCMappingIDByXmlTCId(long tcIdFromXml, ref bool isOk)
        {
            Logger.logBegin("GetTCMappingIDByXmlTCId",string.Format("TCId from xml:[{0}]", tcIdFromXml));
            try
            {
                if (this.assignedTestSuites==null)
                {
                    isOk = false;
                    return -1;
                }
                List<TestCaseExportXmlNodes> lstTCases = new List<TestCaseExportXmlNodes>();
                foreach (var p in this.assignedTestSuites)
                {
                    if (p.ChildTestCases!= null)
                    {
                        lstTCases.AddRange(p.ChildTestCases);
                    }
                }
                var itm = lstTCases.Where(p => (p.TestCaseNodeInfo == null ? false : p.TestCaseNodeInfo.TestCaseIdFromImportFileName == tcIdFromXml)).FirstOrDefault();
                if (itm==null)
                {
                    isOk = false;
                    return -1;
                }
                isOk = true;
                return itm.TestCaseNodeInfo.TestCaseId;
            }
            finally
            {
                Logger.logEnd("GetTCMappingIDByXmlTCId");
            }
        }

        internal long GetDataSetMappingIDByXmlDSId(long datasetIdFromXml, ref bool isOk)
        {
            Logger.logBegin("GetDataSetMappingIDByXmlDSId", string.Format("TCId from xml:[{0}]", datasetIdFromXml));
            try
            {
                if (this.assignedTestSuites == null)
                {
                    isOk = false;
                    return -1;
                }
                List<MarsImpExp_Node_TestData> lstData = new List<MarsImpExp_Node_TestData>();
                foreach(var p in this.assignedTestSuites)
                {
                    if (p.ChildTestCases != null)
                    {
                        foreach (var pt in p.ChildTestCases)
                        {
                            if (pt.DataSetWithSettingDataRecords != null)
                            {
                                lstData.AddRange(pt.DataSetWithSettingDataRecords);
                            }
                        }
                    }
                };
                var itm = lstData.Where(pd => pd.DataSetId == datasetIdFromXml).FirstOrDefault();
                if (itm == null)
                {
                    isOk = false;
                    return -1;
                }
                isOk = true;
                return itm.NewDataSetId;
            }
            finally
            {
                Logger.logEnd("GetDataSetMappingIDByXmlDSId");
            }
        }
    }

    public class TestProjectsExp
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestProjectsExp));

        public string TargetExpProjDir = "";
        internal bool ExportProjectInfo(long lProjId, string strTargetDir,ref string strErrorOrHint, ref string strTargetFile)
        {
            Logger.logBegin("ExportProjectInfo",string.Format("Proj Id [{0}], TargetDir:[{1}]", lProjId,strTargetDir));

            MarsprojectInformationToExport objProjXmlOp = new MarsprojectInformationToExport();
            objProjXmlOp.currentProjectId = lProjId;
            bool isOk = objProjXmlOp.InitProjectApplications(lProjId, ref strErrorOrHint);
            if (!isOk) return false;

            isOk = objProjXmlOp.InitProjAppInfo(lProjId,ref strErrorOrHint);
            if (!isOk) return false;

            isOk = objProjXmlOp.InitTSInfo(lProjId, ref strErrorOrHint);
            if (!isOk) return false;

            isOk = objProjXmlOp.InitSBInfo(lProjId,ref strErrorOrHint);
            if (!isOk) return false;

            ///创建project的xml文件
            string strProjXmlDir = strTargetDir;
            try
            {
                if (!Directory.Exists(strProjXmlDir))
                {
                    Directory.CreateDirectory(strProjXmlDir);
                }
            }
            catch (Exception e)
            {
                Logger.Error("ExportProjectInfo",strErrorOrHint=string.Format("Can't create directory :[{0}] with exception:\r\n[{1}]", strProjXmlDir,e.Message),e);
                return false;
            }
            TargetExpProjDir = objProjXmlOp.TargetDirectory = strProjXmlDir;
            string strProjectFileName = objProjXmlOp.CreateProjectXmlExpFileName();
            isOk = MarsprojectInformationToExport.FlushProjectXmlFile(objProjXmlOp, objProjXmlOp.TargetDirectory,ref strProjectFileName,
                ref strErrorOrHint);
            if (!isOk) return false;
            /// 
            /// 创建TS的主目录
            /// 
            string strDirectoryTSRoot = "";
            isOk = CreateTSDirectories(strProjXmlDir,ref strDirectoryTSRoot, ref strErrorOrHint);
            string strDirTS = Path.Combine(strProjXmlDir, "TS\\");
            /// 循环创建TS目录
            /// 
            if (objProjXmlOp.AssignedTestSuites == null) return true;
            try
            {
                foreach (MarsProjExpTestSuiteInfo itm in objProjXmlOp.AssignedTestSuites)
                {
                    string strTmpTSdir = "";
                    if (itm != null)
                    {
                        strTmpTSdir = Path.Combine(strDirectoryTSRoot, string.Format("TS_ID_{0}", itm.TestSuiteId));
                        if (!Directory.Exists(strTmpTSdir))
                        {
                            Directory.CreateDirectory(strTmpTSdir);
                        }
                        itm.TSDirectory = strTmpTSdir;
                        isOk = itm.ExpTCsToXml(ref strErrorOrHint);
                        if (!isOk)
                        {
                            Logger.Error("ExportProjectInfo",strErrorOrHint);
                            return false;
                        }
                        ///Write TS
                    }
                };
            }
            catch (Exception e)
            {
                Logger.Error("ExportProjectInfo",strErrorOrHint = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }

            /// 循环创建TC的信息
            return true;
        }

        private bool CreateTSDirectories(string strProjXmlDir, ref string strDirectoryTSRoot, ref string strErrorOrHint)
        {
            string strTmpRoot = Path.Combine(strProjXmlDir, "TS\\");
            try
            {
                if (!Directory.Exists(strTmpRoot))
                    Directory.CreateDirectory(strTmpRoot);
                strDirectoryTSRoot = strTmpRoot;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateTSDirectories",strErrorOrHint =string.Format("No such directory exists [{0}], exception when Create [{1}]", strTmpRoot, e.Message),e);
                return false;
            }

        }



    }


    #region sub structure of export projects
    public class MarsProjExpTestSuiteInfo:Notify
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsProjExpTestSuiteInfo));
        [XmlIgnore]
        public B_TEST_SUITE assignedSuite { get; set; }
        
        
        [XmlElement(ElementName = MarsImpExpConst.cnst_project_test_suite_name)]
        public string TestSuiteName
        {
            get
            {
                if (assignedSuite == null) return "";
                return assignedSuite.TEST_SUITE_NAME;
            }
            set
            {
                if (assignedSuite == null)
                    assignedSuite = new B_TEST_SUITE();
                assignedSuite.TEST_SUITE_NAME = value;
                OnPropertyChanged("TestSuiteName");
            }
        }

        [XmlIgnore]
        public long TestSuiteIdFromXml=long.MinValue;

        [XmlAttribute(AttributeName = MarsImpExpConst.cnst_project_test_suite_Id)]
        public long TestSuiteId
        {
            get { if (assignedSuite == null) return -1;return assignedSuite.TEST_SUITE_ID; }
            set {
                if (assignedSuite == null) assignedSuite = new B_TEST_SUITE();
                assignedSuite.TEST_SUITE_ID = value;
                if (TestSuiteIdFromXml == long.MinValue)
                    TestSuiteIdFromXml = value;
                OnPropertyChanged("TestSuiteId");
            }
        }
        [XmlElement(ElementName = MarsImpExpConst.cnst_project_test_suite_desc)]
        public string TestSuiteDescription
        {
            get { if (assignedSuite == null) return "";return assignedSuite.TEST_SUITE_DESCRIPTION; }
            set {
                if (assignedSuite == null) assignedSuite = new B_TEST_SUITE();
                assignedSuite.TEST_SUITE_DESCRIPTION = value;
                OnPropertyChanged("TestSuiteDescription");
            }
        }

        internal static List<MarsProjExpTestSuiteInfo> ConvertFromBussinessObject(List<B_TEST_SUITE> lstTstSuite)
        {
            if (lstTstSuite == null) return null;
            List<MarsProjExpTestSuiteInfo> lstResult = new List<MarsProjExpTestSuiteInfo>();
            lstTstSuite.ForEach(itm=> {
                if (itm!=null)
                {
                    MarsProjExpTestSuiteInfo objTestSuit = new MarsProjExpTestSuiteInfo();
                    objTestSuit.assignedSuite = itm;
                    lstResult.Add(objTestSuit);
                }
            });
            return lstResult;
        }

        internal bool ExpTCsToXml(ref string strErrorOrHint)
        {
            /// Steps:
            /// 1, 获得所有的TCIds
            /// 
            B_TEST_CASE bTC = new B_TEST_CASE();
            List<B_TEST_CASE> lstTC = bTC.GetTestCasesBelong2Ts(MarsMainWindow.CurrentDatabaseIdx, this.TestSuiteId);
            if (lstTC == null) return true;
            if (lstTC.Count <= 0) return true;
            foreach (B_TEST_CASE itm in lstTC) {
                TestCaseWithObjectsExp objTCExp2Xml = new TestCaseWithObjectsExp();
                objTCExp2Xml.TargetDirectory = TSDirectory;
                objTCExp2Xml.TargetFileName = string.Format("MarsExpProj_TC_{0}.xml",itm.TEST_CASE_ID);
                Logger.Info("ExpTCsToXml",string.Format("Try to Export File:[{0}]", objTCExp2Xml.TargetFileName));
                if (!objTCExp2Xml.ExportTestCaseWithObjectByTestCaseId(itm.TEST_CASE_ID, ref strErrorOrHint))
                {
                    Logger.Error("ExpTCsToXml",strErrorOrHint);
                    return false;
                }
            }
            return true;
        }
        [XmlIgnore]
        public string TSDirectory { get; set; }
               
        public static B_TEST_SUITE ConvertToTmpBObj(MarsProjExpTestSuiteInfo objSrc)
        {
            if (objSrc == null) return null;
            return new B_TEST_SUITE() { TEST_SUITE_NAME=objSrc.TestSuiteName, TEST_SUITE_DESCRIPTION = objSrc.TestSuiteDescription, SRC_MAPPING_TESTSUITEID = objSrc.TestSuiteId };
        }

        public void CreateAssignedBObject()
        {
            this.assignedSuite = ConvertToTmpBObj(this);
        }

        internal bool CreateTSObject2DB(MarsEntities dbCntx, ref string strError)
        {
            try
            {
                Logger.logBegin("CreateTSObject2DB", string.Format("Name:[{0}]", this.TestSuiteName));
                return this.assignedSuite.AddXmlObj(MarsMainWindow.CurrentDatabaseIdx, dbCntx, ref strError);
            }catch(Exception e)
            {
                Logger.Error("CreateTSObject2DB",strError = string.Format("Exception:[{0}] stackTrace:\r\n[{1}]",e.Message,e.StackTrace),e);
                return false;
            }
            finally
            {
                Logger.logEnd("CreateTSObject2DB");
            }
            
        }
        
        private ObservableCollection<TestCaseExportXmlNodes> childTestCases=null;
        [XmlIgnore]
        public ObservableCollection<TestCaseExportXmlNodes> ChildTestCases
        {
            get
            {
                if (childTestCases == null)
                    return childTestCases = new ObservableCollection<TestCaseExportXmlNodes>();
                return childTestCases;
            }
            set
            {
                childTestCases = value;
                OnPropertyChanged("ChildTestCases");
            }
        }
    }

    public class MarsProjExpAppInfo:Notify
    {
        [XmlIgnore]
        public B_REL_APP_PROJ assignedRelAppProj { get; set; }
        [XmlElement(ElementName = MarsImpExpConst.cnst_project_app_id)]
        public long ApplicationId { get
            {
                return assignedRelAppProj == null ? -1 : assignedRelAppProj.APPLICATION_ID??-1;
            }
            set
            {
                if (assignedRelAppProj == null)
                    assignedRelAppProj = new B_REL_APP_PROJ();
                assignedRelAppProj.APPLICATION_ID = value;
            }
                 }
        [XmlElement(ElementName = MarsImpExpConst.cnst_project_app_name)]
        public string ApplicationName {
            get
            {
                return assignedRelAppProj == null ? null : assignedRelAppProj.ApplicationName;
            } set {
                if (assignedRelAppProj == null)
                    assignedRelAppProj = new B_REL_APP_PROJ();
                assignedRelAppProj.ApplicationName = value;
                OnPropertyChanged("ApplicationName");
            } }

        public static List<MarsProjExpAppInfo> ConverFromBussinessObject(List<B_REL_APP_PROJ> lstSrc)
        {
            if (lstSrc == null) return null;
            List<MarsProjExpAppInfo> lstResult = new List<MarsProjExpAppInfo>();
            lstSrc.ForEach(itm=> {
                if ((itm!=null))
                {
                    MarsProjExpAppInfo objProjExpApp = new MarsProjExpAppInfo();
                    objProjExpApp.assignedRelAppProj = itm;
                    lstResult.Add(objProjExpApp);
                }
            });

            return lstResult;
        }
    }

    public class MarsProjExpStoryboardInfo:Notify
    {
        [XmlIgnore]
        private T_STORYBOARD_SUMMARYDTO StoryBoardInfo = null;
        public void SetAssingedDto(T_STORYBOARD_SUMMARYDTO dto)
        {
            StoryBoardInfo = dto;
        }
        [XmlElement(ElementName = MarsImpExpConst.cnst_project_storyboardname)]
        public string StoryboardName
        {
            get { if (StoryBoardInfo == null) return "N/A";
                return StoryBoardInfo.STORYBOARD_NAME;
            }
            set {
                if (StoryBoardInfo == null)
                    StoryBoardInfo = new B_STORYBOARD_SUMMARY();
                StoryBoardInfo.STORYBOARD_NAME = value;
                OnPropertyChanged("StoryboardName");
            }
        }

        [XmlElement(ElementName = MarsImpExpConst.cnst_project_storyboard_desc)]
        public string StoryboardDesc
        {
            get
            {
                if (StoryBoardInfo == null)
                    return "N/A";
                return StoryBoardInfo.DESCRIPTION;
            }
            set
            {
                if (StoryBoardInfo == null)
                    StoryBoardInfo = new B_STORYBOARD_SUMMARY();
                StoryBoardInfo.DESCRIPTION = value;
                OnPropertyChanged("StoryboardDesc");
            }
        }

        private ObservableCollection<MarsProjExpStoryDetailInfo> storyboardDetailListForExp;
        [XmlArray(ElementName = MarsImpExpConst.cnst_project_storyboard_Details)]
        [XmlArrayItem(ElementName = MarsImpExpConst.cnst_project_storyboard_Detail)]
        public ObservableCollection<MarsProjExpStoryDetailInfo> StoryboardDetailListForExp
        {
            get
            {
                return storyboardDetailListForExp;
            }
            set
            {
                storyboardDetailListForExp = value;
                OnPropertyChanged("StoryboardDetailListForExp");
            }
        }
        
    }


    public partial class MarsProjExpStoryDetailInfo:Notify
    {
        [XmlIgnore]
        public B_V_STORYBOARD_TEST_FULLVISION assignedStoryboardDtlInfo;
        public void SetAssignedData(V_STORYBOARD_TEST_FULLVISIONDTO itm)
        {
            assignedStoryboardDtlInfo = B_V_STORYBOARD_TEST_FULLVISION.CopyFromDto(itm);
        }
        [XmlElement(ElementName = MarsImpExpConst.cnst_sb_dtl_Action)]
        public string Action
        {
            get {
                if (assignedStoryboardDtlInfo == null)
                    return "";
                return assignedStoryboardDtlInfo.DISPLAY_NAME;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.DISPLAY_NAME = value;
                assignedStoryboardDtlInfo.SetRunValueByString(assignedStoryboardDtlInfo.DISPLAY_NAME);
                OnPropertyChanged("Action");
            }
        }
        [XmlAttribute(AttributeName = MarsImpExpConst.cnst_sb_dtl_RunOrd)]
        public int RunOrder
        {
            get { if (assignedStoryboardDtlInfo == null) return -1;
                return (int)assignedStoryboardDtlInfo.RUN_ORDER;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.RUN_ORDER = value;
                OnPropertyChanged("RunOrder");
            }
        }
        private long tsIDFromXml = -1;
        [XmlIgnore]
        public long TSIDFromXml
        {
            get {
                return tsIDFromXml;
            }
            set {
                tsIDFromXml = value;
                OnPropertyChanged("TSIDFromXml");
            }
        }

        [XmlAttribute(AttributeName =MarsImpExpConst.cnst_sb_dtl_TestSuiteId)]
        public long TSID
        {
            get
            {
                if (assignedStoryboardDtlInfo == null) return -1;
                return assignedStoryboardDtlInfo.TEST_SUITE_ID??-1;
            }
            set
            {
                if(assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.TEST_SUITE_ID = value;
                if (tsIDFromXml==-1)
                {
                    tsIDFromXml = value;
                }
                OnPropertyChanged("TSID");
            }
        }

        [XmlElement(ElementName = MarsImpExpConst.cnst_sb_dtl_TestSuiteName)]
        public string TS_Name
        {
            get
            {
                if (assignedStoryboardDtlInfo == null) return null;
                return assignedStoryboardDtlInfo.TEST_SUITE_NAME;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.TEST_SUITE_NAME = value;
                OnPropertyChanged("TS_Name");
            }
        }

        private long tcIDFromXml=-1;
        [XmlIgnore]
        public long TCIDFromXml
        {
            get { return tcIDFromXml; }
            set {
                tcIDFromXml = value;
                OnPropertyChanged("TCIDFromXml");
            }
        }


        [XmlAttribute(AttributeName = MarsImpExpConst.cnst_sb_dtl_TestCaseId)]
        public long TCID
        {
            get
            {
                if (assignedStoryboardDtlInfo == null) return -1;
                return assignedStoryboardDtlInfo.TEST_CASE_ID;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.TEST_CASE_ID = value;
                if (tcIDFromXml == -1)
                    tcIDFromXml = value;
                OnPropertyChanged("TCID");
            }
        }

        [XmlElement(ElementName = MarsImpExpConst.cnst_sb_dtl_AliasName)]
        public string AliaseName
        {
            get {
                if (assignedStoryboardDtlInfo == null) return null;
                return assignedStoryboardDtlInfo.ALIAS_NAME;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.ALIAS_NAME = value;
                OnPropertyChanged("AliaseName");
            }
        }

        [XmlElement(ElementName = MarsImpExpConst.cnst_sb_dtl_TestCaseName)]
        public string TCName
        {
            get
            {
                if (assignedStoryboardDtlInfo == null) return null;
                return assignedStoryboardDtlInfo.TEST_CASE_NAME;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.TEST_CASE_NAME = value;
                OnPropertyChanged("TCName");
            }
        }

        private long datasetIdFromXml = -1;
        [XmlIgnore]
        public long DatasetIdFromXml
        {
            get {
                return datasetIdFromXml;
            }
            set
            {
                datasetIdFromXml = value;
                OnPropertyChanged("DatasetIdFromXml");
            }
        }

        [XmlAttribute(AttributeName = MarsImpExpConst.cnst_sb_dtl_DS_Id)]
        public long DSID
        {
            get
            {
                if (assignedStoryboardDtlInfo == null) return -1;
                return assignedStoryboardDtlInfo.DATA_SUMMARY_ID??-1;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.DATA_SUMMARY_ID = value;
                if (datasetIdFromXml==-1)
                {
                    datasetIdFromXml = value;
                }
                OnPropertyChanged("DSID"); 
            }
        }

        [XmlElement(ElementName = MarsImpExpConst.cnst_sb_dtl_DS_Name)]
        public string DSName
        {
            get
            {
                if (assignedStoryboardDtlInfo == null) return null;
                return assignedStoryboardDtlInfo.DATA_SET_ALIAS_NAME;
            }
            set
            {
                if (assignedStoryboardDtlInfo == null)
                    assignedStoryboardDtlInfo = new B_V_STORYBOARD_TEST_FULLVISION();
                assignedStoryboardDtlInfo.DATA_SET_ALIAS_NAME = value;
                OnPropertyChanged("DSName");
            }
        }
    }
    #endregion
}
