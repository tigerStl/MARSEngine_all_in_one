using Mars.Business;
using Mars.DataLayer;
using Mars.xml.importExport;
using Mars.xml.importExport.xmlnodes;

using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mars.Utility;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Windows;

namespace Mars.ViewModel.xml
{
    public class XmlProjImportDataModel: ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(XmlProjImportDataModel));

        private ObservableCollection<string> currentLog=new ObservableCollection<string>();
        public ObservableCollection<string> CurrentLog
        {
            get
            {
                if (currentLog == null)
                    currentLog = new ObservableCollection<string>();
                return currentLog;
            }
            set
            {
                if (currentLog != value)
                {
                    currentLog = value;
                    OnPropertyChanged("CurrentLog");
                }
            }
        }

        private string lastLoadLog;
        public string LastLoadLog
        {
            get { return lastLoadLog; }
            set
            {
                lastLoadLog = value;
                OnPropertyChanged("LastLoadLog");
            }
        }

        private System.Windows.Controls.ListBox assignedLogElement = null;
        public XmlProjImportDataModel(System.Windows.Controls.ListBox objAssignedLogElement ):base()
        {
            assignedLogElement = objAssignedLogElement;
            InitCommands();
        }

        private string currentDirectoryOfProjectFiles = null;
        public string CurrentDirectoryOfProjectFiles
        {
            get { return currentDirectoryOfProjectFiles; }
            set {
                currentDirectoryOfProjectFiles = value;
                RaisePropertyChanged("CurrentDirectoryOfProjectFiles");
            }
        }

        private string currentProjectImportFileName = null;
        public string CurrentProjectImportFileName
        {
            get { return currentProjectImportFileName; }
            set
            {
                currentProjectImportFileName = value;
                RaisePropertyChanged("CurrentProjectImportFileName");
            }
        }


        private MarsprojectInformationToExport projectXmlFile;
        public MarsprojectInformationToExport ProjectXmlFile
        {
            get { return projectXmlFile; }
            set {
                projectXmlFile = value;
                RaisePropertyChanged("ProjectXmlFile");
            }
        }

        private ICommand loadProjectsFilesClickCommand = null;
        public ICommand LoadProjectsFilesClickCommand
        {
            get { return loadProjectsFilesClickCommand; }
            set {
                loadProjectsFilesClickCommand = value;
                RaisePropertyChanged("LoadProjectsFilesClickCommand");
            }
        }

        //private ICommand importProjectFileToDBCommand = null;
        public ICommand ImportProjectFileToDBCommand
        {
            get
            {
                return (new DelegateCommand(() => { SaveProjectFileToDB(); })); 
            }            
        }

        #region methods
        private void InitCommands()
        {
            loadProjectsFilesClickCommand = new DelegateCommand(() =>{ SelectAndLoadProjectFiles(); });
        }

        private string defaultImportExportDir
        {
            get {
                string strPath = SystemCommonUtil.GetCurrentPathDir();
                return strPath;// Path.Combine(strPath, @"..\");
            }
        }
        private static Action EmptyDelegate = delegate () { };
        private void InsertLog(string strInfo)
        {
            if (currentLog.Count>1000)
            {
                currentLog.Clear();
            }
            
            CurrentLog.Add(strInfo);
            LastLoadLog = strInfo;
            if (assignedLogElement != null)
            {
                if (this.assignedLogElement.Items.Count > 0)
                {
                    this.assignedLogElement.SelectedIndex = this.assignedLogElement.Items.Count - 1;
                    this.assignedLogElement.ScrollIntoView(this.assignedLogElement.SelectedItem);                    
                }
                this.assignedLogElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                //Dispatcher.CurrentDispatcher.InvokeAsync(new Action(() => { assignedLogElement.InvalidateVisual(); }));
            }
        }

        private void SaveProjectFileToDB()
        {
            /// 步骤：
            /// 1， 在导入project后，导入TS，同时记录新的TS的ids
            /// 2， 导入TC，同时导入新的TC的ids
            /// 3， 加入构建新的TSTC的关系表
            /// 4， 构建project
            /// 5， 构建
            /// 
            Logger.logBegin("SaveProjectFileToDB");
            bool isOk = false;
            string strError = "";
            MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);

            try
            {
                TransactionOptions o = new TransactionOptions();
                o.Timeout = new TimeSpan(1, 0, 0);
                using (var scope = new TransactionScope(TransactionScopeOption.Required, o))
                {
                    InsertLog("Load Project Xml File");

                    projectXmlFile.assignedProject = new Business.B_TEST_PROJECT();
                    projectXmlFile.assignedProject.PROJECT_NAME = projectXmlFile.ProjectName;
                    projectXmlFile.assignedProject.PROJECT_DESCRIPTION = projectXmlFile.ProjectDesc;
                    isOk = InsertProject(projectXmlFile.assignedProject, objTrans, ref strError);
                    //objTrans.CurrentDBContext.SaveChanges();
                    if (!isOk) return;
                    projectXmlFile.currentProjectId = projectXmlFile.assignedProject.PROJECT_ID;

                    InsertLog("Import TS...");
                    //导入TS，object appliation
                    isOk = ImportTS(objTrans, projectXmlFile.currentProjectId, true,ref strError);
                    //objTrans.CurrentDBContext.SaveChanges();
                    if (!isOk)
                    {
                        HintByMessageBox(strError, "ERROR");
                        return;
                    }
                    ///create projects and TS
                    isOk = SetProjectAndTSRel(objTrans, ref strError);
                    objTrans.CurrentDBContext.SaveChanges();
                    if (!isOk)
                    {
                        HintByMessageBox(strError, "ERROR");
                        return;
                    }
                    ///set the ts and applications
                    /// 


                    //创建storyboard
                    isOk = ImportStoryboard(objTrans, ref strError);
                    if (!isOk)
                    {
                        HintByMessageBox(strError, "ERROR");
                        return;
                    }
                    objTrans.CurrentDBContext.SaveChanges();
                    scope.Complete();
                }

                /// add to left tree
                /// 
                MarsTreeView.InsertProjectsToTree(projectXmlFile.currentProjectId);
                HintByMessageBox(string.Format("Project [{0}] is already imported.", projectXmlFile.ProjectName));
                //if (isOk)
            }
            catch (Exception edb)
            {
                Logger.Error("SelectAndLoadProjectFiles", strError = string.Format("Exception:[{0}] stacktrace:[{1}]", edb.Message, edb.StackTrace), edb);
                isOk = false;
                return;
            }
            Logger.logEnd("SaveProjectFileToDB");
        }

        private void SelectAndLoadProjectFiles()
        {
            Logger.logBegin("SelectAndLoadProjectFiles");
            string strError = "";
            bool isOk = true;
            try
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        CommonOpenFileDialog objFolderDialog = new CommonOpenFileDialog()
                        {
                            IsFolderPicker = true,
                            EnsurePathExists = true,
                            EnsureFileExists = false,
                            AllowNonFileSystemItems = false,
                            DefaultDirectory = defaultImportExportDir,
                            //DefaultFileName = "Select PImport Folder",
                            Title = "Select The Folder To Import"
                        };

                        //objFolderDialog.Description = "Select the folder where stores MARS projects export Files.";
                        //objFolderDialog.RootFolder = Environment.SpecialFolder.UserProfile;// typeof(XmlProjImportDataModel).Assembly.Location;
                        //objFolderDialog.ShowNewFolderButton = true;
                        //objFolderDialog.SelectedPath = typeof(XmlProjImportDataModel).Assembly.Location;
                        if (objFolderDialog.ShowDialog() != CommonFileDialogResult.Ok) return;
                        //CurrentDirectoryOfProjectFiles = objFolderDialog.SelectedPath;
                        CurrentDirectoryOfProjectFiles = objFolderDialog.FileName;

                        ///判断 是否存在合法的xml file
                        /// 
                        DirectoryInfo d = new DirectoryInfo(currentDirectoryOfProjectFiles);
                        FileInfo[] arrFiles = d.GetFiles("*.xml");
                        if ((arrFiles == null) || (arrFiles.Length != 1))
                        {
                            Logger.Error("SelectAndLoadProjectFiles", strError = string.Format("Only one xml File should exists under this folder:[{0}]", currentDirectoryOfProjectFiles));
                            ViewModelBase.HintByMessageBox(strError, "Error");
                            return;
                        }
                        CurrentProjectImportFileName = arrFiles[0].FullName;

                        ///导入数据
                        /// 
                        try
                        {
                            ProjectXmlFile = MarsprojectInformationToExport.LoadFromXml(CurrentProjectImportFileName, ref strError);
                            if (projectXmlFile == null)
                            {
                                HintByMessageBox("Can't Load xml file, please check the format and make sure the File is from MARS project export system.\r\n[{0}]", strError);
                                return;
                            }
                            isOk = ImportTS(null, -1, false, ref strError);
                            if (!isOk)
                            {
                                HintByMessageBox("Can't Load Test suite or Test case xml file(s), please check the format and make sure the File(s) is from MARS project export system.\r\n[{0}]", strError);
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error("SelectAndLoadProjectFiles", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                            isOk = false;
                            return;
                        }
                    }),null
                 );
            }
            finally
            {
                if (!isOk)
                    HintByMessageBox(string.Format("Can't create project object with error:\r\n[{0}]", strError), "ERROR");
                Logger.logEnd("SelectAndLoadProjectFiles");
            }
        }

        private bool ImportStoryboard(MarsTransactionMgr objTrans, ref string strError)
        {
            Logger.logBegin("ImportStoryboard");
            try
            {
                if (objTrans == null) return true;
                if (this.projectXmlFile.AssignedStoryboards == null) return true;

                int iPreRunOrd = -1;
                B_PROJ_TC_MGR objStoryboardDtl = new B_PROJ_TC_MGR();
                B_STORYBOARD_SUMMARY objStoryboard = new B_STORYBOARD_SUMMARY();
                bool isOk = false;
                foreach (var stybrd in this.projectXmlFile.AssignedStoryboards)
                {
                    if (stybrd == null) continue;
                    var lstStyDetail = stybrd.StoryboardDetailListForExp.OrderBy(p => p.RunOrder);
                    if (lstStyDetail == null) continue;

                    //create story board record
                    B_STORYBOARD_SUMMARY objCurrntStrybrd = stybrd.ConvertoStoryboardObj(projectXmlFile.currentProjectId);
                    isOk= objCurrntStrybrd.Insert2DB(objTrans.CurrentDBContext,ref strError);
                    //bool isOk = objStoryboard.

                    if (!isOk)
                    {
                        Logger.Error("ImportStoryboard", strError);
                        return false;
                    }

                    iPreRunOrd = -1;
                    foreach(var styDtlItm in lstStyDetail)
                    {
                        if (styDtlItm == null) continue;
                        if (styDtlItm.RunOrder == iPreRunOrd) continue;//有时候有些脏数据， dirty data exists sometimes

                        styDtlItm.TSID = projectXmlFile.GetTSMappingIDByXmlTSId(styDtlItm.TSIDFromXml, ref isOk);
                        if (!isOk)
                        {
                            strError = string.Format("Can't Find TSID [{0}] From storyBoard Detail list.", styDtlItm.TSID);
                            return false;
                        }
                        styDtlItm.TCID = projectXmlFile.GetTCMappingIDByXmlTCId(styDtlItm.TCIDFromXml, ref isOk);
                        if (!isOk)
                        {
                            strError = string.Format("Can't Find Test case ID [{0}] From storyBoard Detail list.", styDtlItm.TCID);
                            return false;
                        }
                        styDtlItm.DSID = projectXmlFile.GetDataSetMappingIDByXmlDSId(styDtlItm.DatasetIdFromXml, ref isOk);
                        if (!isOk)
                        {
                            strError = string.Format("Can't Find Dataset ID [{0}] From storyBoard Detail list.", styDtlItm.TCID);
                            return false;
                        }

                        B_PROJ_TC_MGR objStryBrd = styDtlItm.ConvertFromMarsProjExpStoryDetailInfo(projectXmlFile.currentProjectId, objCurrntStrybrd.STORYBOARD_ID);
                        if (objStryBrd == null) continue;
                        ///save to db
                        /// 
                        isOk = objStryBrd.Insert2DB(objTrans.CurrentDBContext, ref strError);
                        if (!isOk)
                        {
                            Logger.Error("ImportStoryboard",strError);
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ImportStoryboard", strError=string.Format("ImportStoryboard-Exception:[{0}]",e.Message),e);
                return false;
            }finally
            {
                Logger.logEnd("ImportStoryboard");
            }
        }

        private bool SetProjectAndTSRel(MarsTransactionMgr objTrans, ref string strError)
        {
            Logger.logBegin("SetProjectAndTSRel");
            try
            {
                if (this.projectXmlFile == null) return true;
                if (this.projectXmlFile.AssignedTestSuites == null) return true;
                if (objTrans == null) return true;
                List<long> lstTsIds = this.projectXmlFile.AssignedTestSuites.Select(p => p.TestSuiteId).Distinct().ToList();
                B_REL_TEST_SUIT_PROJECT objRelTSProj = new B_REL_TEST_SUIT_PROJECT();
                return objRelTSProj.InsertProjAndTSIds(MarsMainWindow.CurrentDatabaseIdx,
                    objTrans.CurrentDBContext, 
                    projectXmlFile.currentProjectId, 
                    lstTsIds, 
                    ref strError);
            }
            finally
            {
                Logger.logEnd("SetProjectAndTSRel");
            }
            
        }

        private string getExpTCFileNameByTCId(long iId)
        {
            return string.Format("MarsTestCaseExport_{0}.xml", iId);
        }

        private bool ImportTS(MarsTransactionMgr objTrans, long lProjId, bool isWithImportDB, ref string strError)
        {
            Logger.logBegin("ImportTS");
            string strTSRootDir = Path.Combine(CurrentDirectoryOfProjectFiles, "TS");
            if (!Directory.Exists(strTSRootDir))
            {
                Logger.Error("ImportTS", strError = string.Format("No such Directory:[{0}] for Test Suites information.", strTSRootDir));
                return false;
            }
            ///获得test case 数据
            /// 
            List<long> lstApplications = new List<long>();
            bool isOk = false;
            for (int i = 0; i < (projectXmlFile.AssignedTestSuites == null ? -1 : projectXmlFile.AssignedTestSuites.Count); i++)
            {
                MarsProjExpTestSuiteInfo objTSInfo = projectXmlFile.AssignedTestSuites[i];
                if (objTSInfo == null) continue;
                ///判断是否存在相应的TS目录
                /// 
                string strTSDir = Path.Combine(this.currentDirectoryOfProjectFiles, string.Format("TS\\TS_ID_{0}", objTSInfo.TestSuiteId));
                if (!Directory.Exists(strTSDir))
                {
                    Logger.Error("ImportTS", strError = string.Format("No such directory [{0}] exists for Test Suite", strTSDir));
                    isOk = false;
                    return isOk;
                }
                ///获得该目录下的文件
                /// 
                string[] arrFiles = Directory.GetFiles(strTSDir, "*.xml");
                ///获得storyboard中的TS和tc信息
                /// 
                List<long> lstStoryboardTCIds = new List<long>();
                foreach (var itm in projectXmlFile.AssignedStoryboards)
                {

                    if (itm.StoryboardDetailListForExp != null)
                    {
                        lstStoryboardTCIds.AddRange(itm.StoryboardDetailListForExp.Where(p => p.TSID == objTSInfo.TestSuiteId).Select(p => p.TCID).ToList());
                    }
                }

                isOk = true;
                string strErrorTmp = "";
                List<string> TestCaseFileNameWithPth = new List<string>();
                //获得TS目录下的所有TC文件
                //
                
                string[] arrTCFiles = Directory.GetFiles(strTSDir, "MarsTestCaseExport_*.xml");
                TestCaseFileNameWithPth.AddRange(arrTCFiles);

                //lstStoryboardTCIds.Distinct().ToList().ForEach(itm => {
                //    string strFileName = null;
                //    isOk =isOk && File.Exists(strFileName=Path.Combine(strTSDir, getExpTCFileNameByTCId(itm)));

                //    InsertLog(string.Format("\tFind TS file:[{0}]", strFileName));

                //    TestCaseFileNameWithPth.Add(strFileName);
                //    if (!isOk)
                //    {
                //        strErrorTmp = string.IsNullOrEmpty(strErrorTmp) ? string.Format("File [{0}] desn't exists", strFileName) : string.Format("{0}\r\n{1}", strErrorTmp, strFileName);
                //    }
                //});
                //if (!isOk)
                //{
                //    Logger.Error("ImportTS",strError= strErrorTmp);
                //    return false;
                //}

                InsertLog("\tCreate TS Object to Database...");
                if (isWithImportDB)
                {                    
                    /// create TS information
                    /// 
                    isOk = objTSInfo.CreateTSObject2DB(objTrans.CurrentDBContext, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("ImportTS", strError);
                        return false;
                    }
                }

                objTSInfo.ChildTestCases.Clear();

                ///创建TCs
                /// 
                for (int itc = 0; itc < TestCaseFileNameWithPth.Count; itc++)
                {
                    InsertLog(string.Format("\tFind Test case file:[{0}]", TestCaseFileNameWithPth[itc]));

                    TestCaseWithObjectsImp xmlImp = new TestCaseWithObjectsImp();
                    TestCaseExportXmlNodes objTCNodes = xmlImp.LoadXmlToNodes(TestCaseFileNameWithPth[itc], ref strError, ref isOk);
                    if (objTCNodes!=null)
                    {
                        InsertLog("\tInsert Test case inforamtion");
                        /// create test cases and objects into database
                        /// 
                        if (isWithImportDB)
                        {
                            isOk = objTCNodes.ImportTCXmlFileObjectsToDB(false, ref strError, objTrans, this.CurrentLog);
                            if (!isOk)
                            {
                                //HintByMessageBox(strError, "ERROR");
                                return false;
                            }
                            
                        }
                        objTSInfo.ChildTestCases.Add(objTCNodes);
                    }
                }

                if (!isWithImportDB)
                {
                    continue;
                    //return true;
                }

                isOk = SetTSIdTCIdsRelations(objTrans, objTSInfo.TestSuiteId, objTSInfo.ChildTestCases.Select(p=>p.TestCaseNodeInfo.TestCaseId),ref strError);
                if (!isOk)
                {
                    Logger.Error("ImportTS",strError);
                    return false;
                }

                isOk = SetTSRelApps(objTrans, objTSInfo, lstApplications, ref strError);
                if (!isOk)
                {
                    //Logger.Error("ImportTS", strError);
                    return false;
                }
                //filter ,and keep unique
                //objTSInfo.CreateAssignedBObject();

                //isOk = objTSInfo.CreateTSObject2DB(objTrans.CurrentDBContext,ref strError);
                //if (!isOk) return false;
            }
            ///Create app-project relations
            ///
            B_REL_APP_PROJ objRelAppPrj = new B_REL_APP_PROJ();
            isOk = objRelAppPrj.CreateRelations(MarsMainWindow.CurrentDatabaseIdx, 
                lProjId, 
                lstApplications.Distinct(), 
                objTrans.CurrentDBContext, 
                ref strError);
            if (!isOk)
            {
                Logger.Error("ImportTS", string.Format("Error when call B_REL_APP_PROJ.CreateRelations. [{0}]", strError) );
                return false;
            }
            ///create app-test suite relations
            /// 

            /// create story board information
            /// 
            // isOk = objT
            return true;
        }

        private bool SetTSRelApps(MarsTransactionMgr objTrans, MarsProjExpTestSuiteInfo objTSInfo, List<long> lstApplications,ref string strError)
        {
            Logger.logBegin("SetTSRelApps", string.Format("objTs Name:[{0}]", objTSInfo==null?"NULL":objTSInfo.TestSuiteName));
            try
            {
                if ((objTSInfo == null) || (objTrans == null))
                {
                    return true;
                }
                List<long> appIds = new List<long>();
                if (objTSInfo.ChildTestCases == null)
                {
                    return true;
                }
                foreach (var tc in objTSInfo.ChildTestCases)
                {
                    if (tc == null) continue;
                    if (tc.MarsApps == null) continue;
                    try
                    {
                        appIds.AddRange(tc.MarsApps.Select(p => p.APPLICATION_ID));
                    }
                    catch (Exception)
                    {

                    }
                }
                appIds = appIds.Distinct().ToList();
                B_REL_APP_TESTSUITE objTSApp = new B_REL_APP_TESTSUITE();
                bool isOk = objTSApp.SetTSAndApps(MarsMainWindow.CurrentDatabaseIdx, 
                    objTrans.CurrentDBContext, 
                    objTSInfo.TestSuiteId, 
                    appIds, 
                    ref strError);
                if (isOk)
                {
                    lstApplications.AddRange(appIds);
                }
                return isOk;
            }
            finally
            {
                Logger.logEnd("SetTSRelApps");
            }
        }

        

        private bool SetTSIdTCIdsRelations(MarsTransactionMgr objTrans, long tsId, IEnumerable<long> tcIds, ref string strError)
        {
            Logger.logBegin("SetTSIdTCIdsRelations",string.Format("TsId from xml:[{0}]", tsId));
            try
            {
                if (objTrans == null)
                {
                    Logger.Error("SetTSIdTCIdsRelations", strError = "DbContext is Null.");
                    return false;
                }
                
                B_REL_TEST_CASE_TEST_SUITE objTCTS = new B_REL_TEST_CASE_TEST_SUITE();
                bool isOk = objTCTS.BuildTSTCRelationsByIds(MarsMainWindow.CurrentDatabaseIdx, objTrans.CurrentDBContext, tsId, tcIds, ref strError);
                if (!isOk)
                {
                    Logger.Error("SetTSIdTCIdsRelations", strError);
                    return false;
                }
                return true;
            }
            catch (Exception e) 
            {
                Logger.Error("SetTSIdTCIdsRelations", strError = string.Format("Exception for SetTSIdTCIdsRelations:[{0}]", e.Message),e);
                return false;
            }
        }

        private bool InsertProject(B_TEST_PROJECT objPrjInfo, MarsTransactionMgr dbTrans, ref string strError)
        {
            Logger.logBegin("InsertProject",string.Format("Project Name:[{0}]  Desc:[{1}]", objPrjInfo==null?"":objPrjInfo.PROJECT_NAME, objPrjInfo == null ? "" : objPrjInfo.PROJECT_DESCRIPTION));
            try
            {
                if ((objPrjInfo == null)||(dbTrans==null))
                {
                    Logger.Error("InsertProject", strError = "Source project object is null");
                    return false;
                }
                if (string.IsNullOrEmpty(objPrjInfo.PROJECT_NAME))
                {
                    Logger.Error("InsertProject", strError = "Project name is null");
                    return false;
                }
                
                if (objPrjInfo.ProjectExists(MarsMainWindow.CurrentDatabaseIdx, objPrjInfo.PROJECT_NAME, dbTrans.CurrentDBContext))
                {
                    objPrjInfo.PROJECT_NAME += "_imp";
                }
                if (!objPrjInfo.AddNewObject2Database(MarsMainWindow.CurrentDatabaseIdx, dbTrans.CurrentDBContext,ref strError))
                {
                    return false;
                }
                        
                return true;
            }
            finally
            {
                Logger.logEnd("InsertProject");
            }
        }
        #endregion //methods

    }

    public static partial class ExtensionForXmlObject2DbObj
    {
        public static B_STORYBOARD_SUMMARY ConvertoStoryboardObj(this MarsProjExpStoryboardInfo objFromExp, long lProjId)
        {
            B_STORYBOARD_SUMMARY objStoryboard = new B_STORYBOARD_SUMMARY();
            objStoryboard.ASSIGNED_PROJECT_ID = lProjId;
            objStoryboard.DESCRIPTION = objFromExp.StoryboardDesc;
            objStoryboard.LATEST_VERISON = 0;
            objStoryboard.STORYBOARD_ID = -1;
            objStoryboard.STORYBOARD_NAME = objFromExp.StoryboardName;
            return objStoryboard;
        }

        public static B_PROJ_TC_MGR ConvertFromMarsProjExpStoryDetailInfo(this MarsProjExpStoryDetailInfo objSrc,long lProjId, long lStrybrdId)
        {
            B_PROJ_TC_MGR objResult = new B_PROJ_TC_MGR();
            objResult.ALIAS_NAME = objSrc.AliaseName;
            //objResult.DEPENDS_ON = objSrc.
            objResult.LATEST_TEST_MARK_ID = 0;
            objResult.PROJECT_ID = lProjId;
            objResult.RECORD_VERSION = 0;
            objResult.RUN_ORDER = objSrc.RunOrder;
            objResult.RUN_TYPE = B_PROJ_TC_MGR.Action2RunType(objSrc.Action);
            objResult.STORYBOARD_DETAIL_ID = -1;
            objResult.STORYBOARD_ID = lStrybrdId;
            objResult.TEST_CASE_ID = objSrc.TCID;
            objResult.TEST_SUITE_ID = objSrc.TSID;
            return objResult;
        }
    }

}
