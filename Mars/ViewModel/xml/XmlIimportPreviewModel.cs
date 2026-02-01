using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Mars.xml.importExport;
using Mars.xml.importExport.xmlnodes;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Input;
using System.Xml.Serialization;

namespace Mars.ViewModel.xml
{


    internal class SimpleMarsLog
    {
        private string _Log = "";
        internal string Log
        {
            get { return _Log; }
            private set { _Log = value; }
        }
        internal SimpleMarsLog(string strLog)
        {
            Log = strLog;
        }
    }

    public class XmlImportEnvelopApp: ViewModelBase
    {
        [XmlIgnore]
        private string applicationNameFromXml;
        [XmlElement(ElementName = "Application_Name")]
        public string ApplicationNameFromXml
        {
            get
            {
                return applicationNameFromXml;
            }
            set
            {
                applicationNameFromXml = value;
                RaisePropertyChanged("ApplicationNameFromXml");
            }
        }
        [XmlIgnore]
        private string mapTo;
        [XmlElement(ElementName = "MapTo")]
        public string MapTo
        {
            get
            {
                return mapTo;
            }
            set
            {
                mapTo = value;
                RaisePropertyChanged("MapTo");
            }
        }
        [XmlIgnore]
        private bool isSkipImport;
        [XmlElement(ElementName ="IsSkipImport")]
        public bool IsSkipImport
        {
            get
            {
                return isSkipImport;
            }
            set
            {
                isSkipImport = value;
                RaisePropertyChanged("IsSkipImport");
            }
        }
    }

    [XmlRoot(ElementName ="ImportEnvelop")]
    public class XmlImportEnvelopInfo: ViewModelBase
    {
        [XmlIgnore]
        private static MLogger Logger = MLogger.GetLogger(typeof(XmlImportEnvelopInfo));
        

        [XmlIgnore]
        const string cnst_EnvelopName = "MARS_IMPORT_ENV.XML";
        [XmlElement(ElementName = "AppInfo")]
        public XmlImportEnvelopApp[] AppInfos = new XmlImportEnvelopApp[1];

        
        public static XmlImportEnvelopInfo Init(string strImpPth,ref bool isOk, ref string strError)
        {
            string strFile = null;
            if (File.Exists(strFile = Path.Combine(strImpPth, cnst_EnvelopName)))
            {
                return LoadFrom(strFile, ref isOk, ref strError);                
            }

            return new XmlImportEnvelopInfo();
        }

        private static XmlImportEnvelopInfo LoadFrom(string strFileName,ref bool isOk, ref string strError)
        {
            Logger.logBegin("LoadFrom, strFileName");
            try
            {
                isOk = true;
                return XmlHelper.XmlDeserializeFromFile<XmlImportEnvelopInfo>(strFileName, Encoding.UTF8);
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("LoadFrom", strError = e.Message ,e);
                return null;
            }
            finally
            {
                Logger.logEnd("LoadFrom");
            }
            
            
        }

        internal static string GetEnvelopPath(string strPath)
        {
            return Path.Combine(strPath, cnst_EnvelopName);
        }

        internal ObservableCollection<XmlImportEnvelopApp> GetObservableList()
        {
            return  new ObservableCollection<XmlImportEnvelopApp>(AppInfos) ;

        }

        internal void SetObservableList(ObservableCollection<XmlImportEnvelopApp> envelop_applications)
        {
            AppInfos = envelop_applications.ToArray();
        }
    }

    internal class XmlImportPreviewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(XmlImportPreviewModel));
        public XmlImportPreviewModel()
        {
            InitCommand();
            InitApplications();
            InitialEnvelop();
        }

        private void InitCommand()
        {
            cmdBrowseXmlFile = new DelegateCommand(FindXmlFile);
            cmdImportXmlToDB = new DelegateCommand(ImportXmlToDBImpl);

        }
        private void InitApplications()
        {
            
            var tmpApps = new ObservableCollection<string>();
                ObservableCollection<B_REGISTERED_APPS> allApps = B_REGISTERED_APPS.GetCacheApps(MarsMainWindow.CurrentDatabaseIdx);
                foreach (var itm in allApps)
                {
                    if (itm == null) continue;
                tmpApps.Add(itm.APP_SHORT_NAME);
                }

            AllApplications = tmpApps;
            
        }

        public ICommand SaveEnvelopCommand
        {
            get
            {
                return new DelegateCommand(()=>
                {
                    if (ImportEnvelop==null)
                    {
                        HintByMessageBox("No Envelop Information!");
                        return;
                    }
                    XmlHelper.XmlSerializeToFile(ImportEnvelop, XmlImportEnvelopInfo.GetEnvelopPath(AppConfigReader.GetXmlImportDir()), Encoding.UTF8);
                });
            }
        }

        public ICommand ConverterToNewApplication
        {
            get
            {
                return new DelegateCommand(()=> {
                    if (string.IsNullOrEmpty(SelectedConvertToApplication))
                    {
                        HintByMessageBox("Please select a target application name. ");
                        return;
                    }
                    if (CurrentApplication==null)
                    {
                        HintByMessageBox("Please Select an application first.");
                        return;
                    }
                    if (!QuestionByMessageBox(string.Format("Do you want to convertion application name from :[{0}] to [{1}]", CurrentApplication.APP_SHORT_NAME, SelectedConvertToApplication),"MESSAGE"))
                    {
                        return;
                    }
                    CurrentApplication.APP_SHORT_NAME = SelectedConvertToApplication;

                });
            }
        }

        private ICommand cmdBrowseXmlFile = null;
        public ICommand BrowseXmlFile
        {
            get { return cmdBrowseXmlFile; }
        }
        private ICommand cmdImportXmlToDB;
        public ICommand ImportXmlToDB
        {
            get { return cmdImportXmlToDB; }
        }

        private int currentTabItmIdx = 0;
        public int CurrentTabItmIdx
        {
            get { return currentTabItmIdx; }
            set { currentTabItmIdx = value; RaisePropertyChanged("CurrentTabItmIdx"); }
        }

        private string currentFileName;
        public string CurrentFileName
        {
            get { return currentFileName; }
            set { currentFileName = value; RaisePropertyChanged("CurrentFileName"); }
        }

        private ObservableCollection<string> allApplications=null;
        public ObservableCollection<string> AllApplications
        {
            get {
                
                return allApplications;
            }

            set
            {
                allApplications = value;
                RaisePropertyChanged("AllApplications");
            }

        }

        private string selectedConvertToApplication;
        public string SelectedConvertToApplication
        {
            get
            {
                return selectedConvertToApplication;
            }
            set
            {
                selectedConvertToApplication = value;
                RaisePropertyChanged("SelectedConvertToApplication");
            }
        }

        private XmlImportEnvelopInfo importEnvelop;
        public XmlImportEnvelopInfo ImportEnvelop
        {
            get
            {
                return importEnvelop;
            }
            set
            {
                importEnvelop = value;
                RaisePropertyChanged("ImportEnvelop");
            }
        }

        private void InitialEnvelop()
        {
            string strImpPath = AppConfigReader.GetXmlImportDir();
            bool isOk = false;
            string strError = "";
            ImportEnvelop = XmlImportEnvelopInfo.Init(strImpPath, ref isOk, ref strError);
            if (!isOk)
            {
                Logger.Error("InitialEnvelop", strError);
            }
        }

        private ObservableCollection<XmlImportEnvelopApp> envelop_applications;
        public ObservableCollection<XmlImportEnvelopApp> Envelop_applications
        {
            get
            {
                if (ImportEnvelop == null) return null;
                if (envelop_applications==null)
                    return envelop_applications = ImportEnvelop.GetObservableList();
                return envelop_applications;
            }

            set
            {
                envelop_applications = value;
                RaisePropertyChanged("Envelop_applications");

                ImportEnvelop.SetObservableList(envelop_applications);
            }
        }


        private ObservableCollection<SimpleMarsLog> importLogList = new ObservableCollection<SimpleMarsLog>();
        private ObservableCollection<SimpleMarsLog> ImportLogList
        {
            get { return importLogList; }
            set { importLogList = value; RaisePropertyChanged("ImportLogList"); }
        }

        public bool ImportTCXmlFileObjectsToDBByPureDBCnn(ref string strError)
        {
            if (currentXmlTestCaseInfo == null)
            {
                strError = "No Test case information imported";
                return false;
            }
            bool isRollBackRequired = true;
            TargetApplications targetApps = new TargetApplications();
            bool isOk = targetApps.AssignTargetInfo(currentXmlTestCaseInfo.MarsApps,ref strError);
            List<MarsImpExp_Node_Object> lstAllObectsFromXml = new List<MarsImpExp_Node_Object>();
            if (!isOk) return false;

            //all name id
            List<T_OBJECT_NAMEINFODTO> lstAllObjectNames = B_OBJECT_NAMEINFO.GetAllObjectNameInfo(ref strError, ref isOk, MarsMainWindow.CurrentDatabaseIdx);
            List<T_TEST_DATA_SUMMARYDTO> lstAllDataSet = B_T_TEST_DATA_SUMMARYDTO.GetAllDataSetSummary(MarsMainWindow.CurrentDatabaseIdx);

            //获得目标testcase对象
            currentXmlTestCaseInfo.targetTestCase = B_TEST_CASE.GetTestCaseInfoByName(MarsMainWindow.CurrentDatabaseIdx, 
                currentXmlTestCaseInfo.TestCaseNodeInfo.TestCaseName, 
                ref isOk, ref strError);
            if (!isOk)
            {
                return false;
            }
            

            //初始化对象
            foreach (var itmPeg in CurrentXmlTestCaseInfo.ParentObjects)
            {
                if (itmPeg == null) continue;
                B_REGISTED_OBJECT objOp = new B_REGISTED_OBJECT();

                var parentObjApp = currentXmlTestCaseInfo.MarsApps.Where(p => p.APPLICATION_ID == itmPeg.ApplicationId).FirstOrDefault();
                if (parentObjApp==null)
                {
                    strError = string.Format("No such application [{1}] exists for pegwindow object:[{0}]", itmPeg.ObjectName, itmPeg.ApplicationId);
                    return false;
                }
                itmPeg.assignedApp = parentObjApp;

                //将所有的对象放到一个list中，以便查询
                lstAllObectsFromXml.Add(itmPeg);
                lstAllObectsFromXml.AddRange(itmPeg.ChildObjects);

                List<B_REGISTED_OBJECT> lstObjePeg = itmPeg.assignedApp.targetApplication==null?new List<B_REGISTED_OBJECT>():
                    objOp.GetRegisterdObjectsByObjectParentFromCache(MarsMainWindow.CurrentDatabaseIdx, 
                    itmPeg.ObjectName, new List<long> { itmPeg.assignedApp.targetApplication.APPLICATION_ID });
                if (lstObjePeg==null)
                {
                    continue;//no bojects
                }

                var pegObj = lstObjePeg.Where(p => string.Compare(p.OBJECT_HAPPY_NAME,itmPeg.ObjectName)==0).FirstOrDefault();
                itmPeg.targetObject = pegObj;
                foreach (var itmChildUnderPegs in itmPeg.ChildObjects)
                {
                    var objInfo = lstObjePeg.Where(p=>string.Compare(p.OBJECT_HAPPY_NAME, itmChildUnderPegs.ObjectName)==0).FirstOrDefault();
                    itmChildUnderPegs.targetObject = objInfo;
                    itmChildUnderPegs.assignedApp = parentObjApp;
                }
            }


            //将对象装佩到test step中
            B_KEYWORD objKeywordDB = new B_KEYWORD();
            foreach (var stp in currentXmlTestCaseInfo.TestSteps)
            {
                //装配keyword
                T_KEYWORDDTO keywrd = objKeywordDB.GetKeywordByName(MarsMainWindow.CurrentDatabaseIdx, stp.Keyword, null, ref isOk, ref strError);
                if (!isOk)
                {
                    return false;
                }
                stp.targetKeyword = keywrd;

                // get object 
                var objForStp = lstAllObectsFromXml.Where(p => p.ObjectId == stp.ObjectId).Select(p => p.ObjectName).ToList();
                if (!((objForStp == null) || (objForStp.Count <= 0)))
                {

                    var objForStpWithSameHappyName = lstAllObectsFromXml.Where(px => objForStp.Contains(px.ObjectName)).ToList();
                    foreach (var objItmForStpSameHappyName in objForStpWithSameHappyName)
                    {
                        if (objItmForStpSameHappyName == null) continue;

                        if (stp.AssignedObjects.ContainsKey(objItmForStpSameHappyName.assignedApp))
                        {
                            stp.AssignedObjects[objItmForStpSameHappyName.assignedApp] = objItmForStpSameHappyName;
                        }
                        else
                        {
                            stp.AssignedObjects.Add(objItmForStpSameHappyName.assignedApp, objItmForStpSameHappyName);
                        }
                    }
                }

                //装配数据
                foreach(var datasetXml in currentXmlTestCaseInfo.DataSetWithSettingDataRecords)
                {
                    if (datasetXml == null) continue;
                    var data4Step = datasetXml.StepData.Where(p => p.Step_id == stp.TestStepId).FirstOrDefault();
                    if (data4Step == null) continue;

                    var tmpTargetDataSet = lstAllDataSet.Where(q => string.Compare(q.ALIAS_NAME, datasetXml.DataSetName) == 0)
                        .FirstOrDefault();

                    datasetXml.targetDataSet = tmpTargetDataSet;

                    if (stp.AssignedDataAndDataSet.ContainsKey(datasetXml))
                    {
                        stp.AssignedDataAndDataSet[datasetXml] = data4Step;
                    }
                    else
                    {
                        stp.AssignedDataAndDataSet.Add(datasetXml, data4Step);
                    }
                }                              
            }


            //获得目标数据库中的test case 对象和其step信息
            var teststpsFromTargetDB = B_TEST_STEPS.GetTestStepsByTestCaseName(MarsMainWindow.CurrentDatabaseIdx, 
                currentXmlTestCaseInfo.TestCaseNodeInfo.TestCaseName, ref strError, ref isOk);
            //B_V_TEST_STEPS_FULLVISIONDTO.GetTestStepsByTestCaseName(currentXmlTestCaseInfo.TestCaseNodeInfo.TestCaseName, ref strError, ref isOk);
            if (!isOk)
            {
                return false;
            }

            Dictionary<MarsImpExp_Node_TestData, Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>>> dictDataSetDBCatche = new Dictionary<MarsImpExp_Node_TestData, Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>>>();
            DbConnection dbCnn = null;
            DbTransaction dbTrans = null;
            List<T_TEST_DATA_SUMMARYDTO> lstNewDataSetCreated = new List<T_TEST_DATA_SUMMARYDTO>();
            try
            {
                dbCnn = BoHelper.GetDBConnectionFromEntityFramework(MarsMainWindow.CurrentDatabaseIdx, 
                    ref strError, ref isOk);
                if (!isOk) return false;
                dbTrans = dbCnn.BeginTransaction();
                DbCommand dbCmmd = dbCnn.CreateCommand();

                ((Oracle.ManagedDataAccess.Client.OracleCommand)dbCmmd).BindByName = true;
                bool isNewTestcase = false;
                if (currentXmlTestCaseInfo.targetTestCase == null)
                {
                    //create a new test case
                    currentXmlTestCaseInfo.targetTestCase = B_TEST_CASE.CreateNewTestCase(currentXmlTestCaseInfo.TestCaseNodeInfo.TestCaseName,
                        currentXmlTestCaseInfo.TestCaseNodeInfo.TestcaseDesc, 
                        dbCmmd,
                        ref strError, ref isOk );
                    if (!isOk) return false;
                    isNewTestcase = true;
                }

                // delete all test step data
                isOk = B_TEST_DATA_SETTING.deleteReordsByTestCaseId(currentXmlTestCaseInfo.targetTestCase.TEST_CASE_ID, dbCmmd, ref strError);
                if (!isOk) return false;
                ///application
                ///
                foreach (var a in currentXmlTestCaseInfo.MarsApps)
                {
                    if (a == null) continue;
                    if (a.targetApplication==null)
                    {
                        #region create new application
                        long lId = BoHelper.GetBussinessSeq("T_REGISTERED_APPS_SEQ", dbCmmd, ref strError, ref isOk);
                        if (!isOk)
                        {
                            return false;
                        }
                       
                        //需要创建一个新的application //T_REGISTERED_APPS_SEQ.NEXTVAL
                        string strSqlInsertNew = @"INSERT INTO T_REGISTERED_APPS(APPLICATION_ID, APP_SHOT_NAME, APPLCIATION_TYPE_ID,PROCESS_IDENTIFIER, VERSION, EXTRAREQUIREMENT, COMMENT) 
                                                                          VALUES(:APPLICATION_ID, :APP_SHOT_NAME, :APPLCIATION_TYPE_ID,:PROCESS_IDENTIFIER, :VERSION, :EXTRAREQUIREMENT, :COMMENT)";
                        dbCmmd.CommandText = strSqlInsertNew;
                        DbParameter APPLICATION_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        APPLICATION_ID.ParameterName = "APPLICATION_ID";
                        APPLICATION_ID.Value = lId;
                        DbParameter APP_SHOT_NAME = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        APP_SHOT_NAME.ParameterName = "APP_SHOT_NAME";
                        APP_SHOT_NAME.Value = a.APP_SHORT_NAME;
                        DbParameter APPLCIATION_TYPE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        APPLCIATION_TYPE_ID.ParameterName = "APPLCIATION_TYPE_ID";
                        APPLCIATION_TYPE_ID.Value = a.Application_Type_id_fromName;
                        DbParameter PROCESS_IDENTIFIER = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        PROCESS_IDENTIFIER.ParameterName = "PROCESS_IDENTIFIER";
                        PROCESS_IDENTIFIER.Value = a.PROCESS_IDENTIFIER;
                        DbParameter VERSION = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        VERSION.ParameterName = "VERSION";
                        VERSION.Value = a.VERSION;
                        DbParameter EXTRAREQUIREMENT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        EXTRAREQUIREMENT.ParameterName = "EXTRAREQUIREMENT";                                                          
                        EXTRAREQUIREMENT.Value =a.EXTRAREQUIREMENT;
                        DbParameter COMMENT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        EXTRAREQUIREMENT.ParameterName = "COMMENT";
                        EXTRAREQUIREMENT.Value = null;
                        dbCmmd.Parameters.Clear();
                        dbCmmd.Parameters.Add(APPLICATION_ID);
                        dbCmmd.Parameters.Add(APP_SHOT_NAME);
                        dbCmmd.Parameters.Add(APPLCIATION_TYPE_ID);
                        dbCmmd.Parameters.Add(PROCESS_IDENTIFIER);
                        dbCmmd.Parameters.Add(VERSION);
                        dbCmmd.Parameters.Add(EXTRAREQUIREMENT);
                        dbCmmd.Parameters.Add(COMMENT);

                        dbCmmd.ExecuteNonQuery();
                        #endregion

                        a.targetApplication = new T_REGISTERED_APPSDTO() {
                            APPLICATION_ID = lId,
                            APP_SHORT_NAME = a.APP_SHORT_NAME,
                            VERSION = a.VERSION
                        };                        
                    }
                    if (isNewTestcase)
                    {
                        ///build test case and application relation
                        ///
                        isOk = B_REL_APP_TESTCASE.CreateRelation(currentXmlTestCaseInfo.targetTestCase.TEST_CASE_ID, a.targetApplication.APPLICATION_ID, dbCmmd, ref strError);
                        if (!isOk)
                            return false;
                    }
                }
                                         

                //获取dataset信息，如果为空，就创建一个新的
                foreach (var d in currentXmlTestCaseInfo.DataSetWithSettingDataRecords)
                {
                    if (d == null) continue;
                    #region dataset 
                    if (d.targetDataSet==null)
                    {
                        // create new Data set
                        isOk = B_T_TEST_DATA_SUMMARYDTO.AddNewDataSetAndTestCaseRelByCmd(d.DataSetName, "FROM import", currentXmlTestCaseInfo.targetTestCase.TEST_CASE_ID,dbCmmd, ref strError, out d.targetDataSet);
                        if (!isOk) return false;
                        if (d.targetDataSet!=null)
                            lstNewDataSetCreated.Add(d.targetDataSet);
                        isOk = BoHelper.CreateRelTCDataSummary(d.targetDataSet.DATA_SUMMARY_ID, currentXmlTestCaseInfo.targetTestCase.TEST_CASE_ID, dbCmmd, ref strError);
                        if (!isOk)
                            return false;
                    }
                    else
                    {
                        if (isNewTestcase)
                        {
                            isOk = BoHelper.CreateRelTCDataSummary(d.targetDataSet.DATA_SUMMARY_ID, currentXmlTestCaseInfo.targetTestCase.TEST_CASE_ID, dbCmmd, ref strError);
                            if (!isOk)
                                return false;
                        }
                    }
                    if (!dictDataSetDBCatche.ContainsKey(d))
                    {
                        Dictionary<string,List<T_SHARED_OBJECT_POOLDTO>> lstPoolData = B_SHARED_OBJECT_POOL.GetSharePoolInfoByDataSetId(d.targetDataSet.DATA_SUMMARY_ID, dbCmmd, ref isOk, ref strError);
                        if (!isOk) return false;
                        dictDataSetDBCatche.Add(d, lstPoolData);
                    }
                    #endregion

                }

                Dictionary<MarImpExp_Node_AppItem, List<B_REGISTED_OBJECT>> tmpNewAppObjectsList = new Dictionary<MarImpExp_Node_AppItem, List<B_REGISTED_OBJECT>>();
                //逐步处理所有的对象
                #region object by steps and applications
                for (int i = 0; i < CurrentXmlTestCaseInfo.TestSteps.Count; i++)
                {
                    long lTargetNameId = -1;
                    long lobjectIdForTarget = -1;
                    if (CurrentXmlTestCaseInfo.TestSteps[i].ObjectId == -1) continue;

                    T_OBJECT_NAMEINFODTO objNameObjId = null;
                    //获得一个有效的对象
                    var objFromXml = CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects.Values.Where(p => p != null).FirstOrDefault();//[CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects.Keys[0]) ;
                    if (objFromXml == null) continue;
                    objNameObjId = lstAllObjectNames.Where(p => string.Compare(p.OBJECT_HAPPY_NAME, objFromXml.ObjectName) == 0).FirstOrDefault();
                    //是否存在name id
                    if (objNameObjId == null)
                    {
                        //不存在object Name
                        long newObjNameId = BoHelper.GetBussinessSeq(B_REGISTED_OBJECT.SEQ_MARS_OBJECT_ID, dbCmmd, ref strError, ref isOk);
                        if (!isOk) return false;
                        B_OBJECT_NAMEINFO.CreateNewNameObjectByDBCnn(newObjNameId, objFromXml.ObjectName, "from Imprt, Auto", dbCmmd, ref strError, ref isOk);
                        if (!isOk) return false;
                        //吧新创建的name id对象添加到list中                                
                        lstAllObjectNames.Add(objNameObjId = new T_OBJECT_NAMEINFODTO()
                        {
                            OBJECT_NAME_ID = newObjNameId,
                            OBJECT_HAPPY_NAME = objFromXml.ObjectName,
                            OBJNAME_DESCRIPTION = "IMPORTED"
                        });
                        CurrentXmlTestCaseInfo.TestSteps[i].TargetObjectName = objNameObjId;
                        lTargetNameId = objNameObjId.OBJECT_NAME_ID;
                    }
                    CurrentXmlTestCaseInfo.TestSteps[i].TargetObjectName = objNameObjId;

                    foreach (var appInXml in CurrentXmlTestCaseInfo.MarsApps)
                    {
                        MarsImpExp_Node_Object objForStp = CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml];
                        if (objForStp == null) //针对某个application的对象没有,应该是从原系统导出时候存在差异，
                                               //故而，不做处理
                        {
                            continue;
                        }                        

                        if (objForStp.targetObject == null)//缺乏目标系统的对象映射 //因此需要判断是否缺失对象名称以及对象
                        {
                            List<B_REGISTED_OBJECT> tmpNewObjectsList = null;
                            //说明xml中存在对象，但是目标数据库中没有对象，需要创建。在创建后，需要更新同appid下 所有的相同ID的对象 target
                            //先从tmpNewAppObjectsList 中找，如果没有找到再创建新的 否则从该hash表中取
                            if (tmpNewAppObjectsList.ContainsKey(appInXml))
                            {
                                tmpNewObjectsList = tmpNewAppObjectsList[appInXml];
                                if (tmpNewObjectsList == null)
                                {
                                    tmpNewObjectsList = new List<B_REGISTED_OBJECT>();
                                    tmpNewAppObjectsList[appInXml] = tmpNewObjectsList;
                                }
                                var tmpObjNew = tmpNewObjectsList.Where(p =>
                                    string.Compare(p.OBJECT_HAPPY_NAME, CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectName) == 0).FirstOrDefault();
                                if (tmpObjNew != null)
                                {
                                    CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].targetObject = tmpObjNew;
                                    continue;
                                }
                            }     
                            else
                            {
                                tmpNewAppObjectsList.Add(appInXml, tmpNewObjectsList = new List<B_REGISTED_OBJECT>());
                            }
                            B_REGISTED_OBJECT objBussines = new B_REGISTED_OBJECT();
                            int objTypeId = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeIDByName(MarsMainWindow.CurrentDatabaseIdx, 
                                CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectTestType,
                                ref isOk, ref strError);
                            if (!isOk)
                            {
                                return false;
                            }
                            B_REGISTED_OBJECT objNewObjInstance = objBussines.CreateNewObjectByCommand(CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectName, CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].QuickAcess,
                                objNameObjId.OBJECT_NAME_ID, appInXml.targetApplication.APPLICATION_ID, CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectType,
                                objTypeId, "IMPORTED", null, 0,
                                dbCmmd, ref isOk, ref strError);
                            if (!isOk) return false;
                            CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].targetObject = objNewObjInstance;
                            lobjectIdForTarget = objNewObjInstance.OBJECT_ID;
                            tmpNewObjectsList.Add(objNewObjInstance);
                            
                        }
                        else
                        {
                            if (string.Compare(objForStp.targetObject.QUICK_ACCESS, objForStp.QuickAcess, true) != 0)
                            {
                                //update the object inforamtion
                                isOk = B_REGISTED_OBJECT.UpdateObject(objForStp.targetObject.OBJECT_ID, objForStp.QuickAcess, dbCmmd, ref strError);
                                if (!isOk) return false;
                            }
                            //update Test steps with new name id
                            lTargetNameId = objForStp.targetObject.OBJECT_NAME_ID ?? -1;
                            lobjectIdForTarget = objForStp.targetObject.OBJECT_ID;
                            
                        }
                    }
                }
                #endregion
                bool isTestStepUpdated;
                isTestStepUpdated = false;
                for (int i=0;i<CurrentXmlTestCaseInfo.TestSteps.Count;i++)
                {
                    if (i >= teststpsFromTargetDB.Count) break;

                    //object 是否需要更新或者添加？ 每个mapping对象已经好设置。可能有两种情况，一直是存在mapping，一种不存在
                    //Assigned object是xml文件中的对象。必定存在。
                    if (CurrentXmlTestCaseInfo.TestSteps[i].ObjectId!=-1)
                    {
                        //AssignedObjects object should be null. otherwise, it means that step requires an object but can't find
                        if ((CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects == null))
                        {
                            strError = string.Format("no object defined in xml file for step:[id-{0},keyword:{1}, runorder:{2}]", CurrentXmlTestCaseInfo.TestSteps[i].TestStepId,
                                CurrentXmlTestCaseInfo.TestSteps[i].Keyword, CurrentXmlTestCaseInfo.TestSteps[i].RunOrder);
                            Logger.Error("ImportTCXmlFileObjectsToDBByPureDBCnn",strError);
                            return isOk = false;
                        }
                        long lTargetNameId = -1;
                        long lobjectIdForTarget = -1;
                        if ((CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects!=null))                            
                        {
                            lobjectIdForTarget = -1;
                            foreach (var appInXml in CurrentXmlTestCaseInfo.MarsApps)
                            {
                                MarsImpExp_Node_Object objForStp = CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml];
                                if (objForStp==null) //针对某个application的对象没有,应该是从原系统导出时候存在差异，
                                    //故而，不做处理
                                {
                                    continue;
                                }
                                else
                                {
                                    if (objForStp.targetObject == null)//缺乏目标系统的对象映射
                                                                       //因此需要判断是否缺失对象名称以及对象
                                    {
                                        T_OBJECT_NAMEINFODTO objNameObjId = null;
                                        //获得一个有效的对象
                                        var objFromXml = CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects.Values.Where(p => p != null).FirstOrDefault();//[CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects.Keys[0]) ;
                                        if (objFromXml == null) continue;
                                        objNameObjId = lstAllObjectNames.Where(p => string.Compare(p.OBJECT_HAPPY_NAME, objFromXml.ObjectName) == 0).FirstOrDefault();
                                        //是否存在name id
                                        if (objNameObjId == null)
                                        {
                                            //不存在object Name
                                            long newObjNameId = BoHelper.GetBussinessSeq(B_REGISTED_OBJECT.SEQ_MARS_OBJECT_ID, dbCmmd, ref strError, ref isOk);
                                            if (!isOk) return false;
                                            B_OBJECT_NAMEINFO.CreateNewNameObjectByDBCnn(newObjNameId, objFromXml.ObjectName, "from Imprt, Auto", dbCmmd, ref strError, ref isOk);
                                            if (!isOk) return false;
                                            //吧新创建的name id对象添加到list中                                
                                            lstAllObjectNames.Add(objNameObjId = new T_OBJECT_NAMEINFODTO()
                                            {
                                                OBJECT_NAME_ID = newObjNameId,
                                                OBJECT_HAPPY_NAME = objFromXml.ObjectName,
                                                OBJNAME_DESCRIPTION = "IMPORTED"
                                            });
                                            CurrentXmlTestCaseInfo.TestSteps[i].TargetObjectName = objNameObjId;
                                            lTargetNameId = objNameObjId.OBJECT_NAME_ID;
                                        }

                                        List<B_REGISTED_OBJECT> tmpNewObjectsList = null;
                                        //说明xml中存在对象，但是目标数据库中没有对象，需要创建。在创建后，需要更新同appid下 所有的相同ID的对象 target
                                        //先从tmpNewAppObjectsList 中找，如果没有找到再创建新的 否则从该hash表中取
                                        if (tmpNewAppObjectsList.ContainsKey(appInXml))
                                        {
                                            tmpNewObjectsList = tmpNewAppObjectsList[appInXml];
                                            if (tmpNewObjectsList == null)
                                            {
                                                tmpNewObjectsList = new List<B_REGISTED_OBJECT>();
                                                tmpNewAppObjectsList[appInXml] = tmpNewObjectsList;
                                            }
                                            var tmpObjNew = tmpNewObjectsList.Where(p => 
                                                string.Compare(p.OBJECT_HAPPY_NAME, CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectName) == 0).FirstOrDefault();
                                            if (tmpObjNew != null)
                                            {
                                                CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].targetObject = tmpObjNew;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            tmpNewAppObjectsList.Add(appInXml, tmpNewObjectsList=new List<B_REGISTED_OBJECT>());
                                        }
                                        B_REGISTED_OBJECT objBussines = new B_REGISTED_OBJECT();
                                        int objTypeId = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeIDByName(MarsMainWindow.CurrentDatabaseIdx, 
                                            CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectTestType,
                                            ref isOk, ref strError);
                                        if (!isOk)
                                        {
                                            return false;
                                        }
                                        B_REGISTED_OBJECT objNewObjInstance = objBussines.CreateNewObjectByCommand(CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectName, CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].QuickAcess,
                                            objNameObjId.OBJECT_NAME_ID, appInXml.APPLICATION_ID, CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].ObjectType,
                                            objTypeId, "IMPORTED", null, 0,
                                            dbCmmd, ref isOk, ref strError);
                                        if (!isOk) return false;
                                        CurrentXmlTestCaseInfo.TestSteps[i].AssignedObjects[appInXml].targetObject = objNewObjInstance;
                                        lobjectIdForTarget = objNewObjInstance.OBJECT_ID;
                                        tmpNewObjectsList.Add(objNewObjInstance);
                                    }
                                    else
                                    {
                                        if (string.Compare(objForStp.targetObject.QUICK_ACCESS, objForStp.QuickAcess, true) != 0)
                                        {
                                            //update the object inforamtion
                                            isOk = B_REGISTED_OBJECT.UpdateObject(objForStp.targetObject.OBJECT_ID, objForStp.QuickAcess, dbCmmd, ref strError);
                                            if (!isOk) return false;
                                        }
                                        //update Test steps with new name id
                                        lTargetNameId = objForStp.targetObject.OBJECT_NAME_ID ?? -1;
                                        lobjectIdForTarget = objForStp.targetObject.OBJECT_ID;
                                    }
                                }
                            }
                            
                        }
                      

                        #region update test steps
                        if (!isTestStepUpdated)
                        {   //更新test step 只需要更新1次
                            //isOk = B_TEST_STEPS.UpdateStepWithNewObjectNameId( CurrentXmlTestCaseInfo.TestSteps[i].TestStepId, dbCmmd, lTargetNameId, ref strError);
                            isOk = B_TEST_STEPS.UpdateStepWithData(teststpsFromTargetDB[i].STEPS_ID,
                                CurrentXmlTestCaseInfo.TestSteps[i].RunOrder,
                                CurrentXmlTestCaseInfo.TestSteps[i].targetKeyword.KEY_WORD_ID,
                                teststpsFromTargetDB[i].TEST_CASE_ID??-1,
                                lobjectIdForTarget,
                                CurrentXmlTestCaseInfo.TestSteps[i].StepsParamenter,
                                "", //value_setting_data
                                "", //
                                true,
                                lTargetNameId,
                                dbCmmd,
                                ref strError
                                );
                            if (!isOk)
                            {
                                return false;
                            }
                            isTestStepUpdated = true;
                        }
                        #endregion

                        #region update or insert data set
                        long poolid = -1;
                        foreach (var dtset  in dictDataSetDBCatche.Keys)
                        {
                            Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> dicData = dictDataSetDBCatche[dtset];
                            string strDataObjectName = string.IsNullOrEmpty(CurrentXmlTestCaseInfo.TestSteps[i].ObjectName) ? CurrentXmlTestCaseInfo.TestSteps[i].Keyword : CurrentXmlTestCaseInfo.TestSteps[i].ObjectName;
                            if (!CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet.ContainsKey(dtset)) continue;

                            if (!dicData.ContainsKey(strDataObjectName))
                            {
                                dicData.Add(strDataObjectName,new List<T_SHARED_OBJECT_POOLDTO>());
                            }
                            
                            List<T_SHARED_OBJECT_POOLDTO> lstData = dicData[strDataObjectName];
                            if ((lstData==null)||(lstData.Count==0))
                            {
                                //Create new shared data pool
                                poolid = (new B_SHARED_OBJECT_POOL()).CreateNewRecorder(dtset.targetDataSet.DATA_SUMMARY_ID,strDataObjectName, 
                                    lstData.Count+1, 1, CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue,dbCmmd, ref isOk, ref strError );
                                if (!isOk) return false;
                                if (lstData==null)
                                {
                                    dicData[strDataObjectName] = lstData = new List<T_SHARED_OBJECT_POOLDTO>();
                                }
                                lstData.Add(new T_SHARED_OBJECT_POOLDTO() {
                                    OBJECT_POOL_ID = poolid ,
                                    OBJECT_NAME = strDataObjectName,
                                    DATA_VALUE = CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue,
                                    DATA_SUMMARY_ID = dtset.targetDataSet.DATA_SUMMARY_ID
                                });
                            }
                            else
                            {
                                poolid = lstData[0].OBJECT_POOL_ID;                                
                            }
                            //update 
                            TEST_DATA_SETTINGDTO dataSetting = B_TEST_DATA_SETTING.CreateNewRecord(teststpsFromTargetDB[i].STEPS_ID, 
                                1,
                                CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue,
                                0,
                                "from imported",
                                dtset.targetDataSet.DATA_SUMMARY_ID,
                                CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataDirection,
                                poolid,
                                dbCmmd,
                                ref isOk,ref strError
                                );

                            if (!isOk) return false;
                        }
                        #endregion
                    }
                    else
                    {
                        // non object required
                        isOk = B_TEST_STEPS.UpdateStepWithData(teststpsFromTargetDB[i].STEPS_ID,
                                CurrentXmlTestCaseInfo.TestSteps[i].RunOrder,
                                CurrentXmlTestCaseInfo.TestSteps[i].targetKeyword.KEY_WORD_ID,
                                teststpsFromTargetDB[i].TEST_CASE_ID ?? -1,
                                -1,
                                CurrentXmlTestCaseInfo.TestSteps[i].StepsParamenter,
                                "", //value_setting_data
                                "", //
                                true,
                                -1,
                                dbCmmd,
                                ref strError
                                );
                        if (!isOk)
                        {
                            return false;
                        }
                        #region update or insert data set
                        long poolid = -1;
                        foreach (var dtset in dictDataSetDBCatche.Keys)
                        {
                            Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> dicData = dictDataSetDBCatche[dtset];
                            string strDataObjectName = string.IsNullOrEmpty(CurrentXmlTestCaseInfo.TestSteps[i].ObjectName) ? CurrentXmlTestCaseInfo.TestSteps[i].Keyword : CurrentXmlTestCaseInfo.TestSteps[i].ObjectName;
                            if (!CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet.ContainsKey(dtset)) continue;

                            if (!dicData.ContainsKey(strDataObjectName))
                            {
                                dicData.Add(strDataObjectName, new List<T_SHARED_OBJECT_POOLDTO>());
                            }

                            List<T_SHARED_OBJECT_POOLDTO> lstData = dicData[strDataObjectName];
                            if ((lstData == null) || (lstData.Count == 0))
                            {
                                //Create new shared data pool
                                poolid = (new B_SHARED_OBJECT_POOL()).CreateNewRecorder(dtset.targetDataSet.DATA_SUMMARY_ID, strDataObjectName,
                                    lstData.Count + 1, 1, CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue, dbCmmd, ref isOk, ref strError);
                                if (!isOk) return false;
                                if (lstData == null)
                                {
                                    dicData[strDataObjectName] = lstData = new List<T_SHARED_OBJECT_POOLDTO>();
                                }
                                lstData.Add(new T_SHARED_OBJECT_POOLDTO()
                                {
                                    OBJECT_POOL_ID = poolid,
                                    OBJECT_NAME = strDataObjectName,
                                    DATA_VALUE = CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue,
                                    DATA_SUMMARY_ID = dtset.targetDataSet.DATA_SUMMARY_ID
                                });
                            }
                            else
                            {
                                poolid = lstData[0].OBJECT_POOL_ID;
                            }
                            //update 
                            TEST_DATA_SETTINGDTO dataSetting = B_TEST_DATA_SETTING.CreateNewRecord(teststpsFromTargetDB[i].STEPS_ID,
                                1,
                                CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue,
                                0,
                                "from imported",
                                dtset.targetDataSet.DATA_SUMMARY_ID,
                                CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataDirection,
                                poolid,
                                dbCmmd,
                                ref isOk, ref strError
                                );

                            if (!isOk) return false;
                        }
                        #endregion
                    }

                }

                #region //构建test case 和新dataset的关系
                isOk = BoHelper.CreateTestCaseDataSetRelation(lstNewDataSetCreated, currentXmlTestCaseInfo.targetTestCase.TEST_CASE_ID, dbCmmd, ref strError);
                if (!isOk) return false;
                #endregion


                //delete, 数据库的数据比新import的长
                List<long> stepIdsTobeDeleted = teststpsFromTargetDB.Where(p => p.RUN_ORDER > CurrentXmlTestCaseInfo.TestSteps.Count).Select(p => p.STEPS_ID).ToList();
                if (stepIdsTobeDeleted.Count > 0)
                {
                    isOk = BoHelper.deleteTestSteps(stepIdsTobeDeleted, dbCmmd, ref strError);
                    if (!isOk) return false;
                }
                
                for (int i= teststpsFromTargetDB.Count;i< CurrentXmlTestCaseInfo.TestSteps.Count;i++)
                {
                    long lObjeNameId = -1;
                    if (CurrentXmlTestCaseInfo.TestSteps[i].ObjectId!=-1)
                    {
                        if (CurrentXmlTestCaseInfo.TestSteps[i].TargetObjectName==null)
                        {
                            strError = string.Format("Object_name_info for #[{0}] is null", CurrentXmlTestCaseInfo.TestSteps[i].RunOrder);
                            Logger.Error("ImportTCXmlFileObjectsToDBByPureDBCnn", strError);
                            return false;
                        }
                        lObjeNameId = CurrentXmlTestCaseInfo.TestSteps[i].TargetObjectName.OBJECT_NAME_ID;
                    }
                    //insert test steps
                    T_TEST_STEPSDTO newTestStep = B_TEST_STEPS.CreateTestStep(CurrentXmlTestCaseInfo.TestSteps[i].targetKeyword.KEY_WORD_ID,
                            lObjeNameId,
                            CurrentXmlTestCaseInfo.TestSteps[i].StepsParamenter,
                            CurrentXmlTestCaseInfo.targetTestCase.TEST_CASE_ID,
                            CurrentXmlTestCaseInfo.TestSteps[i].RunOrder,
                            "For import",
                            dbCmmd,
                            ref isOk,
                            ref strError
                        );
                    if (!isOk)
                    {
                        return false;
                    }

                    #region update or insert data set
                    long poolid = -1;
                    foreach (var dtset in dictDataSetDBCatche.Keys)
                    {
                        Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> dicData = dictDataSetDBCatche[dtset];
                        string strDataObjectName = string.IsNullOrEmpty(CurrentXmlTestCaseInfo.TestSteps[i].ObjectName) ? CurrentXmlTestCaseInfo.TestSteps[i].Keyword : CurrentXmlTestCaseInfo.TestSteps[i].ObjectName;
                        if (!dicData.ContainsKey(strDataObjectName))
                        {
                            dicData.Add(strDataObjectName, new List<T_SHARED_OBJECT_POOLDTO>());
                        }
                        if (!CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet.ContainsKey(dtset)) continue;

                        List<T_SHARED_OBJECT_POOLDTO> lstData = dicData[strDataObjectName];
                        if ((lstData == null) || (lstData.Count == 0))
                        {
                            //Create new shared data pool
                            poolid = (new B_SHARED_OBJECT_POOL()).CreateNewRecorder(dtset.targetDataSet.DATA_SUMMARY_ID, strDataObjectName,
                                lstData.Count + 1, 1, CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue, dbCmmd, ref isOk, ref strError);
                            if (!isOk) return false;
                            if (lstData == null)
                            {
                                dicData[strDataObjectName] = lstData = new List<T_SHARED_OBJECT_POOLDTO>();
                            }
                            lstData.Add(new T_SHARED_OBJECT_POOLDTO()
                            {
                                OBJECT_POOL_ID = poolid,
                                OBJECT_NAME = strDataObjectName,
                                DATA_VALUE = CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue,
                                DATA_SUMMARY_ID = dtset.targetDataSet.DATA_SUMMARY_ID
                            });
                        }
                        else
                        {
                            poolid = lstData[0].OBJECT_POOL_ID;
                        }
                        //update 
                        TEST_DATA_SETTINGDTO dataSetting = B_TEST_DATA_SETTING.CreateNewRecord(newTestStep.STEPS_ID,
                            1,
                            CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataValue,
                            0,
                            "from imported",
                            dtset.targetDataSet.DATA_SUMMARY_ID,
                            CurrentXmlTestCaseInfo.TestSteps[i].AssignedDataAndDataSet[dtset].DataDirection,
                            poolid,
                            dbCmmd,
                            ref isOk, ref strError
                            );

                        if (!isOk) return false;
                    }
                    #endregion
                }

                isRollBackRequired = false;
                dbTrans.Commit();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ImportTCXmlFileObjectsToDBByPureDBCnn", strError = string.Format("Exception:[{0}] stackTrace:{1}",e.Message,e.StackTrace), e);
                if (dbTrans != null)
                {
                    try
                    {
                        dbTrans.Rollback();
                        isRollBackRequired = false;
                    }
                    catch (Exception)
                    {
                        
                    }
                }
                return false;
            }
            finally
            {
                if (isRollBackRequired)
                {
                    try
                    {
                        dbTrans.Rollback();
                    }
                    catch (Exception)
                    {                       
                    }
                }
            }
            
        }

        class TargetApplications
        {
            private static MLogger Logger = MLogger.GetLogger(typeof(TargetApplications));
            internal bool AssignTargetInfo(ObservableCollection<MarImpExp_Node_AppItem> marsApps, ref string strError)
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
                bool isOk = false;
                try
                {
                    ObservableCollection<B_REGISTERED_APPS>  lstTargetSysApps = B_REGISTERED_APPS.GetCacheApps(MarsMainWindow.CurrentDatabaseIdx);
                    if (lstTargetSysApps==null)
                    {
                        strError = "No application information in System";
                        return false;
                    }
                    foreach (var itm in marsApps)
                    {
                        if (itm == null) continue;
                        var targetApp = lstTargetSysApps.Where(p=>string.Compare(p.APP_SHORT_NAME, itm.APP_SHORT_NAME,true)==0).FirstOrDefault();
                        if (targetApp == null)
                        {
                            //Create new Test application infor
                            B_REGISTERED_APPS newApp = B_REGISTERED_APPS.NewApplication(MarsMainWindow.CurrentDatabaseIdx, 
                                itm.APP_SHORT_NAME, (int)itm.Application_Type_id_fromName, itm.COMMENT, 
                                itm.EXTRAREQUIREMENT, itm.PROCESS_IDENTIFIER, itm.VERSION, ref strError, ref isOk);
                            if (!isOk)
                            {
                                return false;
                            }
                        }
                        itm.targetApplication = targetApp;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error("AssignTargetInfo",strError = string.Format("Exception:[{0}]",e.Message), e);
                    return false;
                }
            }
        }


        private ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> currentObjTypeList = null;
        public bool ImportTCXmlFileObjectsToDB(string strDBIdx, ref string strError, bool isOverride = false)
        {
            //this.CurrentTabItmIdx = 2;
            /// 准备数据
            /// 
            currentObjTypeList = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeListEx(MarsMainWindow.CurrentDatabaseIdx);
            ImportLogList.Clear();

            MarsTransactionMgr objTrans = new MarsTransactionMgr(strDBIdx,true);
            int iCnt = -1;
            //objTrans.CurrentDBContext;
            //using (objTrans.CurrentDBContext)
            {
                try
                {
                    TransactionOptions options = new TransactionOptions();
                    options.Timeout = new TimeSpan(0, 30, 0);
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, options))
                    {
                        bool isOk = false;

                        /// 1，添加application，返回applicationID
                        ///  1.1 获得application的mapping，因为不同的system具有不同的application id 
                        /// 
                        Logger.Info("ImportXmlToDBImpl", "To Deal with applications...");
                        B_TEST_CASE objTestCaseBo = new B_TEST_CASE();

                        List<B_REGISTERED_APPS> lstApps = ConvertFromImportAppInfo(CurrentXmlTestCaseInfo.MarsApps);
                        //ImportLogList.Add(new SimpleMarsLog("Begin to deal applications..."));
                        List<KeyValuePair<long, T_REGISTERED_APPSDTO>> lstMappedApp = B_REGISTERED_APPS.CreateAppInfoIdMapping(MarsMainWindow.CurrentDatabaseIdx, 
                            lstApps, ref isOk, ref strError, objTrans);
                        if (!isOk)
                        {
                            HintByMessageBox(strError = string.Format("Can't import testcase with Error from Create new Application:\r\n[{0}]", strError));
                            Logger.Error("ImportXmlToDBImpl", strError);
                            //ImportLogList.Add(new SimpleMarsLog(strError));
                            return false;
                        }
                        Logger.Info("ImportXmlToDBImpl", string.Format("Applications are ok with [{0}] mapped applications", lstMappedApp.Count));
                        //isOk = objTestCaseBo.ImportApplications(objTrans,lstApps,ref strError);

                        /// 2，添加objects，返回objects的mapping
                        /// objects的mapping信息在B_REGISTED_OBJECTS中newpeg, new objects
                        B_REGISTED_OBJECT objB = new B_REGISTED_OBJECT();
                        Logger.Info("ImportXmlToDBImpl", "Begin to deal Objects...");
                        //ImportLogList.Add(new SimpleMarsLog("Begin to deal Objects..."));

                        Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstObjectFromImportXml = ConverFromImportObjectsInfo(CurrentXmlTestCaseInfo.ParentObjects, ref isOk, ref strError);
                        if (!isOk)
                        {
                            HintByMessageBox(string.Format("Error:{0}", strError), "Error");
                            return false;
                        }
                        objB.CreateObjectMappingForImport(lstMappedApp, lstObjectFromImportXml, ref isOk, ref strError, objTrans,strDBIdx:strDBIdx, true);
                        if (!isOk)
                        {
                            Logger.Error("ImportXmlToDBImpl", strError = string.Format("Can't import data with error:[{0}]", strError));
                            HintByMessageBox(string.Format("Error:{0}", strError), "Error");
                            return false;
                        }                       

                        #region///3, Teststeps的处理
                        //ImportLogList.Add(new SimpleMarsLog("Begin to deal test steps..."));
                        isOk = InsertAndMappingTestSteps(currentXmlTestCaseInfo.TestSteps, currentXmlTestCaseInfo.TestCaseNodeInfo, lstObjectFromImportXml,
                            lstMappedApp.Select(p => p.Key).ToList(), currentXmlTestCaseInfo.TestCaseNodeInfo.TestCaseName,
                            objTrans, ref strError,true);
                        if (!isOk)
                        {
                            Logger.Error("ImportXmlToDBImpl", strError = string.Format("Error when Insert Test steps\r\n{0}", strError));
                            HintByMessageBox(strError, "Error");
                            return false;
                        }
                        #endregion //3, Teststeps的处理                   

                        #region ///4, Dataset 的处理
                        if (isOverride)
                        {
                            isOk = InsertAndMappingDataSetOverrideMode(currentXmlTestCaseInfo.DataSetWithSettingDataRecords, currentXmlTestCaseInfo.TestCaseNodeInfo, lstObjectFromImportXml, currentXmlTestCaseInfo.TestSteps, objTrans, ref strError);
                        }
                        else {
                            isOk = InsertAndMappingDataSet(currentXmlTestCaseInfo.DataSetWithSettingDataRecords, currentXmlTestCaseInfo.TestCaseNodeInfo, lstObjectFromImportXml, currentXmlTestCaseInfo.TestSteps, objTrans, ref strError);
                        }
                        if (!isOk)
                        {
                            Logger.Error("ImportXmlToDBImpl", strError = string.Format("Error when call InsertAndMappingDataSet\r\n{0}", strError));
                            HintByMessageBox(strError, "Error");
                            return false;
                        }

                        #endregion
                        iCnt= objTrans.CurrentDBContext.SaveChanges();
                        scope.Complete();
                    }

                    //update cache
                    MarsDBGlobe_Cache.UpdateObjectsCache();
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error("ImportXmlToDBImpl", strError = string.Format("Exception:{0}\r\nstackTrace:{1}\r\nInnerException:{2}",
                        e.Message,
                        e.StackTrace,

                        e.InnerException == null ? "N/A" : (e.InnerException.InnerException == null ? e.InnerException.Message : e.InnerException.InnerException.Message)));
                    HintByMessageBox(strError, "ERROR");
                    return false;
                }
                finally
                {
                    HintByMessageBox(string.Format("Finished import, total [{0}] records are imported", iCnt), "Hint");
                    objTrans.CurrentDBContext = null;
                }
            }
        }
        private void ImportXmlToDBImpl()
        {
            ///步骤：
            /// 1，添加application，返回applicationID
            /// 2，添加objects，返回objects的mapping
            /// 3，修改teststep的objects 的ids
            /// 4，添加dataset，返回dataset的mapping
            /// 5，插入steps，返回steps的mapping ids
            /// 6，插入数据
            /// strError
            /// 
            string strError = "";
            Logger.logBegin("ImportXmlToDBImpl");


            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ImportTCXmlFileObjectsToDBByPureDBCnn(ref strError))
                {
                    ViewModelBase.HintByMessageBox(string.Format("import {0} finished!", CurrentXmlTestCaseInfo.TestCaseNodeInfo.TestCaseName));
                    //update cache
                    MarsDBGlobe_Cache.UpdateAppTestCaseCache();
                    
                }
                else
                {
                    ViewModelBase.HintByMessageBox(string.Format("import {0} Failed with error:\r\n{1}!", CurrentXmlTestCaseInfo.TestCaseNodeInfo.TestCaseName, strError));
                }
                //ImportTCXmlFileObjectsToDB(ref strError,true);
            }));
        }

        
        private bool InsertAndMappingDataSetOverrideMode(ObservableCollection<MarsImpExp_Node_TestData> dataSetWithSettingDataRecords,
            MarsImpExp_Node_TestCaseInfo testCaseNodeInfo,
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstObjectFromImportXml,
            ObservableCollection<MarsImpExp_Node_Test> testSteps,
            MarsTransactionMgr objTrans,
            ref string strError)
        {
            Logger.logBegin("InsertAndMappingDataSetOverrideMode", string.Format("TestCaseName:[{0}],TestCaseDesc:[{1}] DatasetCount:[{2}]", testCaseNodeInfo == null ? "N/A" : testCaseNodeInfo.TestCaseName, testCaseNodeInfo == null ? "N/A" : testCaseNodeInfo.TestcaseDesc, dataSetWithSettingDataRecords == null ? 0 : dataSetWithSettingDataRecords.Count));
            if (dataSetWithSettingDataRecords == null || dataSetWithSettingDataRecords.Count == 0) return true;

            //ImportLogList.Add(new SimpleMarsLog("Begin to deal dataset....")); 

            foreach (MarsImpExp_Node_TestData dataItm in dataSetWithSettingDataRecords)
            {
                if (dataItm == null) continue;
                ///算法：
                /// 1，判断是否存在同名的data set name，如果存在，就覆盖 超出现有test step的已经删除了
                /// 该步可以省略，因为不同的dataset会依附不同的testcase
                /// 2，创建dataset
                /// 3，assign dataset 和testcase
                /// 4，添加数据到 sharepool中
                /// 5，创建Test_data_setting表中
                /// 

                /// 算法：
                /// 1，原数据库的sharedata数据
                /// 2，循环找import的数据dataset
                /// 3，是否存在相同名字的，如果没有，创建有个新的，否则获得已经存在的
                /// 4，从当前的
                bool isOk = false;

                ///need install data to test step first
                ///
                if (dataItm.StepData == null) continue;
                for (int istp = 0;istp< testSteps.Count;istp++)
                {
                    if (testSteps[istp] == null) continue;
                    testSteps[istp].CurrentData = null;
                }

                foreach(var oneStepDataFromXml in dataItm.StepData)
                {
                    if (oneStepDataFromXml == null) continue;
                    var oneStep = testSteps.Where(p => p.TestStepId == oneStepDataFromXml.Step_id).FirstOrDefault();
                    if (oneStep == null) continue;
                    oneStep.CurrentData = oneStepDataFromXml.DataValue;
                }

                B_T_TEST_DATA_SUMMARYDTO objDataSetSum = new B_T_TEST_DATA_SUMMARYDTO();
                T_TEST_DATA_SUMMARYDTO currentDataSet = B_T_TEST_DATA_SUMMARYDTO.GetDataSetFromName(dataItm.DataSetName, dataItm.Description, ref isOk, ref strError, objTrans.CurrentDBContext);
                long lNewDataSetId = -1;
                if (!isOk)
                {
                    Logger.Error("InsertAndMappingDataSetOverrideMode", string.Format("Error when call B_T_TEST_DATA_SUMMARYDTO.CreateDataSet. \r\n{0}", strError));
                    return false;
                }
                if ((currentDataSet == null))
                {
                    lNewDataSetId = objDataSetSum.CreateDataSet(dataItm.DataSetName, dataItm.Description, ref isOk, ref strError, objTrans.CurrentDBContext);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingDataSetOverrideMode", string.Format("Error when call B_T_TEST_DATA_SUMMARYDTO.CreateDataSet. \r\n{0}", strError));
                        return false;
                    }
                    dataItm.NewDataSetId = lNewDataSetId;
                    /// 3，assign dataset 和testcase
                    /// 
                    long lNewRelDat_Tc = objDataSetSum.AssignDataSetToTestCase(testCaseNodeInfo.TestCaseId, lNewDataSetId, objTrans.CurrentDBContext, ref isOk, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingDataSetOverrideMode", string.Format("Error when call B_T_TEST_DATA_SUMMARYDTO.AssignDataSetToTestCase. \r\n{0}", strError));
                        return false;
                    }
                    /// 4，添加数据到 sharepool和test——data——setting中
                    /// 
                    Dictionary<string, List<MarsImpExp_Node_Test>> dicTestStpObjects = testSteps.GroupBy(p => p.ObjectId == -1 ? p.Keyword : p.ObjectName)
                        .ToDictionary(p => p.Key, v => v.ToList());
                    /// 说明 因为是覆盖模式，需要就sharepool数据进行调整
                    /// 

                    isOk = ModifySharePoolAndTestDataRecord(lNewDataSetId, testSteps, dataItm, dicTestStpObjects, objTrans.CurrentDBContext, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingDataSetOverrideMode", string.Format("Error when call CreateSharePool \r\n{0}", strError));
                        return false;
                    }

                    isOk = CreateSharePoolAndTestDataRecord(lNewDataSetId, testSteps, dataItm, dicTestStpObjects, objTrans.CurrentDBContext, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingDataSetOverrideMode", string.Format("Error when call CreateSharePool \r\n{0}", strError));
                        return false;
                    }
                    Thread.Sleep(5);
                }
                else
                {
                    /// for testing
                    /// 

                    #region for testing comment
                    /// it requires to rebuild the Dataset and test case relation
                    lNewDataSetId = currentDataSet.DATA_SUMMARY_ID;
                    long lNewRelDat_Tc = objDataSetSum.AssignDataSetToTestCase(testCaseNodeInfo.TestCaseId, lNewDataSetId, objTrans.CurrentDBContext, ref isOk, ref strError,false);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingDataSetOverrideMode", string.Format("Error when call B_T_TEST_DATA_SUMMARYDTO.AssignDataSetToTestCase for existsing dataset. \r\n{0}", strError));
                        return false;
                    }

                    /// 更新或者添加数据
                    /// 
                    Dictionary<string, List<MarsImpExp_Node_Test>> dicTestStpObjects = testSteps.GroupBy(p => p.ObjectId == -1 ? p.Keyword : p.ObjectName)
                        .ToDictionary(p => p.Key, v => v.ToList());

                    //isOk = CreateOrModifySharePoolAndTestDataRecord(lNewDataSetId, testSteps, dataItm, dicTestStpObjects, objTrans.CurrentDBContext, ref strError);
                    isOk = ModifySharePoolAndTestDataRecord(lNewDataSetId, testSteps, dataItm, dicTestStpObjects, objTrans.CurrentDBContext, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingDataSetOverrideMode", string.Format("Error when call CreateOrModifySharePoolAndTestDataRecord \r\n{0}", strError));
                        return false;
                    }
                    #endregion
                }

                //ImportLogList.Add(new SimpleMarsLog(string.Format("create one dataset [{0}] with id [{1}]--uncommitted.",dataItm.DataSetName,lNewDataSetId)));

            }
            return true;
        }
        private bool InsertAndMappingDataSet(ObservableCollection<MarsImpExp_Node_TestData> dataSetWithSettingDataRecords,
            MarsImpExp_Node_TestCaseInfo testCaseNodeInfo,
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstObjectFromImportXml,
            ObservableCollection<MarsImpExp_Node_Test> testSteps,
            MarsTransactionMgr objTrans,
            ref string strError)
        {
            Logger.logBegin("InsertAndMappingDataSet", string.Format("TestCaseName:[{0}],TestCaseDesc:[{1}] DatasetCount:[{2}]", testCaseNodeInfo == null ? "N/A" : testCaseNodeInfo.TestCaseName, testCaseNodeInfo == null ? "N/A" : testCaseNodeInfo.TestcaseDesc, dataSetWithSettingDataRecords == null ? 0 : dataSetWithSettingDataRecords.Count));
            if (dataSetWithSettingDataRecords == null || dataSetWithSettingDataRecords.Count == 0) return true;

            //ImportLogList.Add(new SimpleMarsLog("Begin to deal dataset....")); 

            foreach (MarsImpExp_Node_TestData dataItm in dataSetWithSettingDataRecords)
            {
                if (dataItm == null) continue;
                ///算法：
                /// 1，判断是否存在同名的data set name，如果存在，在dataset后面添加_imp_MMddYYYY
                /// 该步可以省略，因为不同的dataset会依附不同的testcase
                /// 2，创建dataset
                /// 3，assign dataset 和testcase
                /// 4，添加数据到 sharepool中
                /// 5，创建Test_data_setting表中
                /// 
                bool isOk = false;
                B_T_TEST_DATA_SUMMARYDTO objDataSetSum = new B_T_TEST_DATA_SUMMARYDTO();
                long lNewDataSetId = objDataSetSum.CreateDataSet(dataItm.DataSetName, dataItm.Description, ref isOk, ref strError, objTrans.CurrentDBContext);
                if (!isOk)
                {
                    Logger.Error("InsertAndMappingDataSet", string.Format("Error when call B_T_TEST_DATA_SUMMARYDTO.CreateDataSet. \r\n{0}", strError));
                    return false;
                }
                dataItm.NewDataSetId = lNewDataSetId;

                //ImportLogList.Add(new SimpleMarsLog(string.Format("create one dataset [{0}] with id [{1}]--uncommitted.",dataItm.DataSetName,lNewDataSetId)));

                /// 3，assign dataset 和testcase
                /// 
                long lNewRelDat_Tc = objDataSetSum.AssignDataSetToTestCase(testCaseNodeInfo.TestCaseId, lNewDataSetId, objTrans.CurrentDBContext, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("InsertAndMappingDataSet", string.Format("Error when call B_T_TEST_DATA_SUMMARYDTO.AssignDataSetToTestCase. \r\n{0}", strError));
                    return false;
                }

                /// 4，添加数据到 sharepool和test——data——setting中
                /// 
                Dictionary<string, List<MarsImpExp_Node_Test>> dicTestStpObjects = testSteps.GroupBy(p => p.ObjectId == -1 ? p.Keyword : p.ObjectName)
                    .ToDictionary(p => p.Key, v => v.ToList());

                isOk = CreateSharePoolAndTestDataRecord(lNewDataSetId, testSteps, dataItm, dicTestStpObjects, objTrans.CurrentDBContext, ref strError);
                if (!isOk)
                {
                    Logger.Error("InsertAndMappingDataSet", string.Format("Error when call CreateSharePool \r\n{0}", strError));
                    return false;
                }
                Thread.Sleep(5);

            }
            return true;
        }        
        private bool CreateOrModifySharePoolAndTestDataRecord(long lDataSetId, ObservableCollection<MarsImpExp_Node_Test> testSteps, MarsImpExp_Node_TestData testDataXml,
            Dictionary<string, List<MarsImpExp_Node_Test>> dicTestStpObjects,
            MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("CreateOrModifySharePoolAndTestDataRecord", string.Format("DataSetId:[{0}]", lDataSetId));
            /// 算法：
            /// 1，group by testStep 获得数据名称
            /// 2，循环对group by的数据进行处理
            /// 3，依据stepid 从dataxml中取数据
            try
            {
                B_SHARED_OBJECT_POOL objSharePool = new B_SHARED_OBJECT_POOL();
                B_TEST_DATA_SETTING objDataRec = new B_TEST_DATA_SETTING();
                bool isOk = false;
                long iPoolId = -1;
                int iObjectOrder = -1;

                //ImportLogList.Add(new SimpleMarsLog("CreateSharePoolAndTestDataRecord begins..."));

                foreach (string strObjectKey in dicTestStpObjects.Keys)
                {
                    List<MarsImpExp_Node_Test> lstSorted = dicTestStpObjects[strObjectKey];
                    if ((lstSorted != null) || (lstSorted.Count > 0))
                        lstSorted.OrderBy(p => p.RunOrder);
                    iObjectOrder = 1;
                    foreach (var objItm in lstSorted)
                    {
                        MarsImpExp_Node_Step_Data stepData = testDataXml.StepData.Where(p => p.Step_id == objItm.TestStepId).FirstOrDefault();
                        if (stepData == null) continue;

                        ///Write data to pool and Test Data setting table
                        /// 
                        iPoolId = objSharePool.CreateNewRecorder(lDataSetId, strObjectKey, iObjectOrder, 1, stepData.DataValue, dbCntx, ref isOk, ref strError);
                        iObjectOrder++;
                        if (!isOk)
                        {
                            Logger.Error("CreateOrModifySharePoolAndTestDataRecord", string.Format("Error when call CreateNewRecorder-\r\n{0}", strError));
                            return false;
                        }

                        //ImportLogList.Add(new SimpleMarsLog(string.Format("created one Data item [{1}] to shared pool with id :[{0}]--uncommitted", iPoolId, stepData.DataValue)));

                        ///将数据写入Test data setting表中
                        /// 
                        long lDataRecId = objDataRec.InsertDataRec(objItm.NewStepId, 1, stepData.DataValue, 2, stepData.Description, lDataSetId, 1, iPoolId, dbCntx, ref isOk, ref strError);
                        if (!isOk)
                        {
                            Logger.Error("CreateOrModifySharePoolAndTestDataRecord", string.Format("Error when call InsertDataRec-\r\n{0}", strError));
                            return false;
                        }
                        //ImportLogList.Add(new SimpleMarsLog(string.Format("created one Data item [{1}] to Data record for test step:[{2}] with id :[{0}]--uncommitted", lDataRecId, stepData.DataValue,
                        //string.Format("#{4} [{0}]-[{1}]-[{2}]-[{3}]", objItm.Keyword,objItm.ObjectName,objItm.StepsParamenter, stepData.DataValue,objItm.RunOrder)
                        //)));
                    }
                    //testDataXml.StepData.Where(p=>p.Step_id)
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateOrModifySharePoolAndTestDataRecord", strError = string.Format("Exception:[{0}],stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("CreateOrModifySharePoolAndTestDataRecord");
            }


        }        
        private bool ModifySharePoolAndTestDataRecord(long lDataSetId, ObservableCollection<MarsImpExp_Node_Test> testSteps, MarsImpExp_Node_TestData testDataXml,
            Dictionary<string, List<MarsImpExp_Node_Test>> dicTestStpObjects,
            MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("ModifySharePoolAndTestDataRecord", string.Format("DataSetId:[{0}]", lDataSetId));
            /// 算法：
            /// 1，group by testStep 获得数据名称
            /// 2，循环对group by的数据进行处理
            /// 3，依据stepid 从dataxml中取数据
            try
            {
                B_SHARED_OBJECT_POOL objSharePool = new B_SHARED_OBJECT_POOL();
                B_TEST_DATA_SETTING objDataRec = new B_TEST_DATA_SETTING();
                bool isOk = false;
                long iPoolId = -1;
                int iObjectOrder = -1;

                Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> objOrginalSharedPoolInfo_BasedOnDataSetId = B_SHARED_OBJECT_POOL.GetSharePoolInfoByDataSetId(lDataSetId, dbCntx, ref isOk, ref strError);
                if (!isOk) return false;
                //ImportLogList.Add(new SimpleMarsLog("CreateSharePoolAndTestDataRecord begins..."));

                foreach (string strObjectKey in dicTestStpObjects.Keys)
                {
                    List<MarsImpExp_Node_Test> lstSorted = dicTestStpObjects[strObjectKey];
                    if ((lstSorted != null) || (lstSorted.Count > 0))
                        lstSorted=lstSorted.OrderBy(p => p.RunOrder).ToList();

                    List<T_SHARED_OBJECT_POOLDTO> lstOrgShareData = null;
                    if (objOrginalSharedPoolInfo_BasedOnDataSetId.Keys.Contains(strObjectKey))
                    {
                        //share Data表中有该对象信息，那木
                        lstOrgShareData = objOrginalSharedPoolInfo_BasedOnDataSetId[strObjectKey];
                        ///这里在两种情况
                        ///1，已经按照对象变量名称规整的，从xml文件中来的数据量小于从数据库来的数据， 那么，直接按照数据库的序号更新数据库，然后增加或者更新test_data_setting表
                        ///2，已经按照对象变量名称规整的，从xml文件中来的数据量大于从数据库来的数据， 那么，先执行1，然后增加pool再更新test_data_setting表
                        for (int i = 0; i < lstOrgShareData.Count; i++)
                        {
                            if (i >= lstSorted.Count) break;
                            /// update 
                            /// 

                            isOk = objSharePool.updateRecordwithNewData(lstOrgShareData[i], lstSorted[i].CurrentData, dbCntx, ref strError);
                            if (!isOk)
                            {
                                Logger.Error("ModifySharePoolAndTestDataRecord", string.Format("Error when call B_SHARED_OBJECT_POOL.updateRecordwithNewData, [{0}]", strError));
                                return false;
                            }
                            
                            /// 处理 test Data setting表
                            /// 
                            isOk = objDataRec.CheckAndUpdateOneRecord(lstSorted[i].NewStepId,
                                lDataSetId,
                                lstOrgShareData[i].OBJECT_POOL_ID,
                                lstSorted[i].CurrentData,
                                dbCntx,
                                ref strError);
                            if (!isOk)
                                return false;
                        }
                        if (lstSorted.Count>lstOrgShareData.Count)
                        {
                            for (int i= lstOrgShareData.Count;i<lstSorted.Count;i++)
                            {
                                var stepData = lstSorted[i];
                                iPoolId = objSharePool.CreateNewRecorder(lDataSetId, strObjectKey, iObjectOrder, 1, stepData.CurrentData, dbCntx, ref isOk, ref strError);
                                if (!isOk)
                                    return false;
                                /// 处理 test Data setting表
                                /// 
                                isOk = objDataRec.CheckAndUpdateOneRecord(lstSorted[i].NewStepId,
                                    lDataSetId,
                                    iPoolId,
                                    lstSorted[i].CurrentData,
                                    dbCntx,
                                    ref strError);
                                if (!isOk)
                                    return false;
                            }
                        }
                    }
                    else
                    {
                        //share Data表中没有该对象信息，那木 需要创建
                        foreach (var objItm in lstSorted)
                        {
                            MarsImpExp_Node_Step_Data stepData = testDataXml.StepData.Where(p => p.Step_id == objItm.TestStepId).FirstOrDefault();
                            if (stepData == null)
                                continue;
                            
                            iPoolId = objSharePool.CreateNewRecorder(lDataSetId, strObjectKey, 0, 1, stepData.DataValue, dbCntx, ref isOk, ref strError);
                            if (!isOk)
                                return false;
                            if (objOrginalSharedPoolInfo_BasedOnDataSetId.Keys.Contains(strObjectKey))
                            {
                                var lstObjPool = objOrginalSharedPoolInfo_BasedOnDataSetId[strObjectKey];
                                if (lstObjPool == null) {
                                    lstObjPool = new List<T_SHARED_OBJECT_POOLDTO>();
                                    objOrginalSharedPoolInfo_BasedOnDataSetId[strObjectKey] = lstObjPool;
                                }
                                lstObjPool.Add(new T_SHARED_OBJECT_POOLDTO()
                                {
                                    OBJECT_NAME = strObjectKey,
                                    OBJECT_POOL_ID = iPoolId,
                                    DATA_VALUE = stepData.DataValue,
                                    OBJECT_ORDER = 0,
                                });
                            }
                            else 
                            {
                                objOrginalSharedPoolInfo_BasedOnDataSetId.Add(strObjectKey, new List<T_SHARED_OBJECT_POOLDTO>()
                                {
                                    new T_SHARED_OBJECT_POOLDTO()
                                    {
                                        OBJECT_NAME=strObjectKey,
                                        OBJECT_POOL_ID = iPoolId,
                                        DATA_VALUE = stepData.DataValue,
                                        OBJECT_ORDER = 0 ,
                                    }
                                });
                            }
                            
                            /// 处理 test Data setting表
                            /// 
                            isOk = objDataRec.CheckAndUpdateOneRecord(objItm.NewStepId,
                                lDataSetId,
                                iPoolId,
                                objItm.CurrentData,
                                dbCntx,
                                ref strError);
                            if (!isOk)
                                return false;
                        }
                        
                    }
                    
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ModifySharePoolAndTestDataRecord", strError = string.Format("Exception:[{0}],stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("ModifySharePoolAndTestDataRecord");
            }


        }

        private bool CreateSharePoolAndTestDataRecord(long lDataSetId, ObservableCollection<MarsImpExp_Node_Test> testSteps, MarsImpExp_Node_TestData testDataXml,
            Dictionary<string, List<MarsImpExp_Node_Test>> dicTestStpObjects,
            MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("CreateSharePoolAndTestDataRecord", string.Format("DataSetId:[{0}]", lDataSetId));
            /// 算法：
            /// 1，group by testStep 获得数据名称
            /// 2，循环对group by的数据进行处理
            /// 3，依据stepid 从dataxml中取数据
            try
            {
                B_SHARED_OBJECT_POOL objSharePool = new B_SHARED_OBJECT_POOL();
                B_TEST_DATA_SETTING objDataRec = new B_TEST_DATA_SETTING();
                bool isOk = false;
                long iPoolId = -1;
                int iObjectOrder = -1;

                //ImportLogList.Add(new SimpleMarsLog("CreateSharePoolAndTestDataRecord begins..."));

                foreach (string strObjectKey in dicTestStpObjects.Keys)
                {
                    List<MarsImpExp_Node_Test> lstSorted = dicTestStpObjects[strObjectKey];
                    if ((lstSorted != null) || (lstSorted.Count > 0))
                        lstSorted.OrderBy(p => p.RunOrder);
                    iObjectOrder = 1;
                    foreach (var objItm in lstSorted)
                    {
                        MarsImpExp_Node_Step_Data stepData = testDataXml.StepData.Where(p => p.Step_id == objItm.TestStepId).FirstOrDefault();
                        if (stepData == null) continue;

                        ///Write data to pool and Test Data setting table
                        /// 
                        iPoolId = objSharePool.CreateNewRecorder(lDataSetId, strObjectKey, iObjectOrder, 1, stepData.DataValue, dbCntx, ref isOk, ref strError);
                        iObjectOrder++;
                        if (!isOk)
                        {
                            Logger.Error("CreateSharePoolAndTestDataRecord", string.Format("Error when call CreateNewRecorder-\r\n{0}", strError));
                            return false;
                        }

                        //ImportLogList.Add(new SimpleMarsLog(string.Format("created one Data item [{1}] to shared pool with id :[{0}]--uncommitted", iPoolId, stepData.DataValue)));

                        ///将数据写入Test data setting表中
                        /// 
                        long lDataRecId = objDataRec.InsertDataRec(objItm.NewStepId, 1, stepData.DataValue, 2, stepData.Description, lDataSetId, 1, iPoolId, dbCntx, ref isOk, ref strError);
                        if (!isOk)
                        {
                            Logger.Error("CreateSharePoolAndTestDataRecord", string.Format("Error when call InsertDataRec-\r\n{0}", strError));
                            return false;
                        }
                        //ImportLogList.Add(new SimpleMarsLog(string.Format("created one Data item [{1}] to Data record for test step:[{2}] with id :[{0}]--uncommitted", lDataRecId, stepData.DataValue,
                            //string.Format("#{4} [{0}]-[{1}]-[{2}]-[{3}]", objItm.Keyword,objItm.ObjectName,objItm.StepsParamenter, stepData.DataValue,objItm.RunOrder)
                            //)));
                    }
                    //testDataXml.StepData.Where(p=>p.Step_id)
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateSharePoolAndTestDataRecord", strError = string.Format("Exception:[{0}],stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("CreateSharePoolAndTestDataRecord");
            }


        }

        private bool InsertAndMappingTestSteps(ObservableCollection<MarsImpExp_Node_Test> testSteps, MarsImpExp_Node_TestCaseInfo testCaseNodeInfo,
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> dicObjectContainer,
            List<long> lstAppIds,
            string strTestCaseName,
            MarsTransactionMgr objTrans, ref string strError,
            bool isOverride=false)
        {
            Logger.logBegin("InsertAndMappingTestSteps", string.Format("TestCaseName:[{0}],TestCaseDesc:[{1}]", testCaseNodeInfo == null ? "N/A" : testCaseNodeInfo.TestCaseName, testCaseNodeInfo == null ? "N/A" : testCaseNodeInfo.TestcaseDesc));
            try
            {
                if (testSteps == null) return true;
                ///算法：
                /// 1，判断是否存在同名的Test case，如果存在，改名，附加_imp_MMddYYYY
                /// 2，使用新的testcase id
                /// 
                B_TEST_CASE objTstCase = new B_TEST_CASE();
                bool isOk = false, isExistsTestCase=false;
                string strNewTestcaseName = testCaseNodeInfo.TestCaseName;
                //ImportLogList.Add(new SimpleMarsLog("Create test case..."));
                long iTestCaseId = objTstCase.MergeTestCaseByNameAndDesc(
                    MarsMainWindow.CurrentDatabaseIdx,
                    strNewTestcaseName, testCaseNodeInfo.TestcaseDesc,
                    ref strNewTestcaseName, 
                    ref strError, 
                    objTrans.CurrentDBContext,
                    ref isExistsTestCase,
                    isOverride);
                ///这里 可以添加test case name变化的处理
                /// 
                isOk = iTestCaseId > 0;
                if (!isOk)
                {
                    Logger.Error("InsertAndMappingTestSteps", string.Format("Error when call MergeTestCaseByNameAndDesc.\r\n[{0}]", strError));
                    return false;
                }
                //添加 test case 和application的关系表
                if (!isExistsTestCase)
                {
                    isOk = CreateTestCaseAndItsAppRelRec(iTestCaseId, lstAppIds, objTrans, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingTestSteps", string.Format("Error when call CreateTestCaseAndItsAppRelRec.\r\n[{0}]", strError));
                        return false;
                    }
                }
                //ImportLogList.Add(new SimpleMarsLog(string.Format("Done, new test case Id:[{0}] --uncommitted", iTestCaseId)));

                testCaseNodeInfo.TestCaseId = iTestCaseId;

                B_TEST_STEPS objStp = new B_TEST_STEPS();
                int iCnt = 0;
                long lNewStpId = -1;
                objStp.TEST_CASE_ID = iTestCaseId;

                List<B_REGISTED_OBJECT> lstAllObjects = new List<B_REGISTED_OBJECT>();
                foreach(var k in dicObjectContainer.Keys)
                {
                    if (k == null) continue;
                    lstAllObjects.Add(k);
                    lstAllObjects.AddRange(dicObjectContainer[k]);
                }
                /// 
                List<T_TEST_STEPSDTO> lstOriginalSteps = new List<T_TEST_STEPSDTO>();
                if (isOverride)
                {
                    lstOriginalSteps = BoHelper.GetTestStepByName(MarsMainWindow.CurrentDatabaseIdx, 
                        strTestCaseName, objTrans.CurrentDBContext,ref strError, ref isOk);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingTestSteps",string.Format("Get TestCaseName generate errors:[{0}]",strError));
                        return false;
                    }
                    if (testSteps.Count < lstOriginalSteps.Count)
                    {
                        isOk = objStp.DeleteTestStep(lstOriginalSteps.Where(p => p.RUN_ORDER > testSteps.Count).Select(p => p.STEPS_ID).ToList(),
                            objTrans.CurrentDBContext,
                            ref strError);

                    }
                }
                
                /// already sorted test steps by runorder
                /// 
                foreach (MarsImpExp_Node_Test stpNode in testSteps)
                {
                    if (stpNode == null) continue;
                    var orgTstStp = lstOriginalSteps.Where(p => p.RUN_ORDER == stpNode.RunOrder).FirstOrDefault();

                    if (stpNode.ObjectId != -1)
                    {
                        var q = from v_s in lstAllObjects
                                where v_s.OBJECT_ID == stpNode.ObjectId
                                select v_s;
                        if (q.FirstOrDefault() == null)
                        {
                            ///something wrong
                            /// 
                            continue;
                        }
                        var qo = q.FirstOrDefault();

                        if (isOverride)
                        {
                            
                            if (orgTstStp == null)
                            {
                                lNewStpId = objStp.InsertNewStep( 
                                    MarsMainWindow.CurrentDatabaseIdx,
                                    iTestCaseId, stpNode.Keyword, qo.NewObjectNameId, stpNode.RunOrder, stpNode.StepsParamenter, "Imported from XML", qo.NewObjectRegTableId, ref isOk, ref strError, objTrans.CurrentDBContext, isOverride);                                
                            }
                            else
                            {
                                isOk = objStp.OverrideTestStep(MarsMainWindow.CurrentDatabaseIdx, 
                                    orgTstStp.STEPS_ID, 
                                    iTestCaseId, 
                                    stpNode.Keyword, 
                                    qo.NewObjectNameId, 
                                    stpNode.RunOrder, 
                                    stpNode.StepsParamenter, 
                                    "Imported from XML", 
                                    qo.NewObjectRegTableId, 
                                    ref strError, 
                                    objTrans.CurrentDBContext);
                                lNewStpId = orgTstStp.STEPS_ID;
                            }
                        }
                        else
                        {
                            lNewStpId = objStp.InsertNewStep(
                                MarsMainWindow.CurrentDatabaseIdx,
                                iTestCaseId, stpNode.Keyword, qo.NewObjectNameId, stpNode.RunOrder, stpNode.StepsParamenter, "Imported from XML", qo.NewObjectRegTableId, ref isOk, ref strError, objTrans.CurrentDBContext, isOverride);
                            lNewStpId = orgTstStp.STEPS_ID;
                        }                        
                    }
                    else
                    {
                        //无需对象的keyword
                        if (isOverride)
                        {
                            if (orgTstStp == null)
                            {
                                lNewStpId = objStp.InsertNewStep(
                                    MarsMainWindow.CurrentDatabaseIdx,
                                    iTestCaseId, stpNode.Keyword, null, stpNode.RunOrder, stpNode.StepsParamenter, "Imported from XML", null, ref isOk, ref strError, objTrans.CurrentDBContext, isOverride);
                            }
                            else
                            {
                                //no object is required
                                isOk = objStp.OverrideTestStep(MarsMainWindow.CurrentDatabaseIdx, 
                                    orgTstStp.STEPS_ID, 
                                    iTestCaseId, 
                                    stpNode.Keyword, 
                                    null, 
                                    stpNode.RunOrder, 
                                    stpNode.StepsParamenter, 
                                    "Imported from XML", 
                                    null, 
                                    ref strError, 
                                    objTrans.CurrentDBContext);
                                lNewStpId = orgTstStp.STEPS_ID;
                            }
                        }
                        else
                        {
                            lNewStpId = objStp.InsertNewStep(
                                MarsMainWindow.CurrentDatabaseIdx,
                                iTestCaseId, stpNode.Keyword, null, stpNode.RunOrder, stpNode.StepsParamenter, "Imported from XML", null, ref isOk, ref strError, objTrans.CurrentDBContext);
                            
                        }
                    }
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingTestSteps", string.Format("Error from InsertNewStep [{0}]", strError));
                        return false;
                    }
                    stpNode.NewStepId = lNewStpId;
                    //ImportLogList.Add(new SimpleMarsLog(string.Format("Test step Done[#{4}:{1}-{2}-{3}], new test step Id:[{0}] --uncommitted", lNewStpId, stpNode.Keyword, stpNode.ObjectName, stpNode.StepsParamenter, stpNode.RunOrder)));
                    iCnt++;
                }

                
                Logger.Info("InsertAndMappingTestSteps", string.Format("[{0}] records are inserted and mapped", iCnt));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertAndMappingTestSteps", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("InsertAndMappingTestSteps");
            }
        }

        private bool CreateTestCaseAndItsAppRelRec(long iTestCaseId, List<long> lstAppIds, MarsTransactionMgr objTrans, ref string strError)
        {
            Logger.logBegin("CreateTestCaseAndItsAppRelRec",string.Format("TestCaseId :[{0}] AppIds' Count:[{1}]", iTestCaseId, lstAppIds==null?-1:lstAppIds.Count));
            try
            {
                B_REL_APP_TESTCASE objAppTC = new B_REL_APP_TESTCASE();
                bool isOk = objAppTC.CreateAppWithTCId(MarsMainWindow.CurrentDatabaseIdx, 
                    iTestCaseId, 
                    lstAppIds, 
                    objTrans.CurrentDBContext, 
                    ref strError);
                return isOk;
            }
            finally
            {
                Logger.logEnd("CreateTestCaseAndItsAppRelRec");
            }
        }

        private Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> ConverFromImportObjectsInfo(List<MarsImpExp_Node_ParentObject> parentObjects, ref bool isOk, ref string strError)
        {
            bool isNull = false;
            try
            {
                Logger.logBegin("ConverFromImportObjectsInfo", string.Format("object count:{0}", (isNull = (parentObjects == null)) ? "N/A" : parentObjects.Count + ""));
                if (isNull) return null;
                Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstRslt = new Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>>();
                foreach (var itm in parentObjects)
                {
                    
                    if (itm == null) continue;
                    //ImportLogList.Add(new SimpleMarsLog(string.Format("Convert Pegwindow:[{0}]...", itm.ObjectName)));
                    B_REGISTED_OBJECT objPeg = itm.ConvertTo();
                    if (objPeg.TYPE_ID < 0)
                    {
                        isOk = false;
                        Logger.Error("ConverFromImportObjectsInfo", strError = string.Format("Can't find object type from db:[{0}]", itm.ObjectTestType));
                        return null;
                    }
                    lstRslt.Add(objPeg, itm.ConvertToBusinessObj());
                    //lstRslt.AddRange(itm.ConvertToBusinessObj());
                }
                isOk = true;
                return lstRslt;
            }
            finally
            {
                Logger.logEnd("ConverFromImportObjectsInfo");
            }
        }

        private List<B_REGISTERED_APPS> ConvertFromImportAppInfo(ObservableCollection<MarImpExp_Node_AppItem> marsApps)
        {
            List<B_REGISTERED_APPS> lstRslt = new List<B_REGISTERED_APPS>();
            foreach (MarImpExp_Node_AppItem appExported in marsApps)
            {
                if (appExported == null) continue;
                B_REGISTERED_APPS objTarget = (new MarImpExp_Node_AppItem.DBSubService(appExported)).Convert2();
                if (objTarget == null) continue;
                lstRslt.Add(objTarget);
                //objTarget.APPLICATION_ID
            }
            return lstRslt;
        }

        private void FindXmlFile()
        {
            CurrentFileName = OpenFileBrowserAndReturnFileFile("MARS test case export/Import File (*.xml)|*.xml");
            if (currentFileName == null) return;
            if (!File.Exists(currentFileName))
            {
                HintByMessageBox(string.Format("No such file exists [{0}]", currentFileName), "Error");
                return;
            }

            ///load file
            /// 
            //if (currentXmlTestCaseInfo == null)
            //    currentXmlTestCaseInfo = new TestCaseExportXmlNodes();
            string strError = "";
            TestCaseWithObjectsImp xmlImp = new TestCaseWithObjectsImp();
            bool isOkLoaded = false;
            CurrentXmlTestCaseInfo = xmlImp.LoadXmlToNodes(currentFileName, ref strError, ref isOkLoaded);
            if ((!isOkLoaded) || (currentXmlTestCaseInfo == null))
            {
                HintByMessageBox(string.Format("Can't load or convert Xml to Desired objects. \r\nFile:[{1}]\r\nErrors:[{0}]", strError, currentFileName));
                return;
            }
            //Applications = currentXmlTestCaseInfo.MarsApps;


        }




        private TestCaseExportXmlNodes currentXmlTestCaseInfo = null;
        public TestCaseExportXmlNodes CurrentXmlTestCaseInfo
        {
            get
            {
                return currentXmlTestCaseInfo;
            }
            set
            {
                currentXmlTestCaseInfo = value;
                if (currentXmlTestCaseInfo == null) return;
                ///set Test case default application
                /// 
                if (currentXmlTestCaseInfo.MarsApps != null)
                {
                    this.currentTestCaseApplication = currentXmlTestCaseInfo.MarsApps == null ? null : (currentXmlTestCaseInfo.MarsApps.Count <= 0 ? null : currentXmlTestCaseInfo.MarsApps[0]);
                }
                if (currentXmlTestCaseInfo.DataSetWithSettingDataRecords != null)
                {
                    this.currentDatasetting = currentXmlTestCaseInfo.DataSetWithSettingDataRecords.Count <= 0 ? null : currentXmlTestCaseInfo.DataSetWithSettingDataRecords[0];
                }
                if ((this.currentTestCaseApplication != null) && (this.currentDatasetting != null))
                {
                    LoadCurrentTestCaseToGrid();
                }
                RaisePropertyChanged("CurrentXmlTestCaseInfo");
            }
        }

        private MarsImpExp_TestStepWithData currentTest_Step;
        public MarsImpExp_TestStepWithData CurrentTest_Step
        {
            get
            {
                return currentTest_Step;
            }
            set
            {
                currentTest_Step = value;
                RaisePropertyChanged("CurrentTest_Step");
                RaisePropertyChanged("CurrentObjIdentifier");
            }
        }



        private MarImpExp_Node_AppItem currentApplication;
        public MarImpExp_Node_AppItem CurrentApplication
        {
            get
            {
                return currentApplication;
            }
            set
            {
                currentApplication = value;
                RaisePropertyChanged("CurrentApplication");
                RaisePropertyChanged("CurrentObjIdentifier");
            }
        }



        private MarImpExp_Node_AppItem currenTestCaseApplication;
        public MarImpExp_Node_AppItem CurrenTestCaseApplication
        {
            get
            {
                return currenTestCaseApplication;
            }
            set
            {
                currentTestCaseApplication = value;
                RaisePropertyChanged("CurrenTestCaseApplication");
            }
        }

        private MarsImpExp_Node_TestData currentDatasetting;
        public MarsImpExp_Node_TestData CurrentDatasetting
        {
            get { return currentDatasetting; }
            set
            {
                currentDatasetting = value;

                ///load test steps with data
                /// 
                if (currentDatasetting == null) return;
                LoadCurrentTestCaseToGrid();
                RaisePropertyChanged("currentDatasetting");

            }
        }

        private MarImpExp_Node_AppItem currentTestCaseApplication;
        public MarImpExp_Node_AppItem CurrentTestCaseApplication
        {
            get { return currentTestCaseApplication; }
            set
            {
                currentTestCaseApplication = value;
                RaisePropertyChanged("CurrentTestCaseApplication");

                if (CurrentDatasetting == null) return;
                LoadCurrentTestCaseToGrid();
            }
        }

        private void LoadCurrentTestCaseToGrid()
        {
            if (this.currentXmlTestCaseInfo == null)
                return;
            if (this.currentDatasetting == null) return;
            if (this.currentTestCaseApplication == null) return;

            var hashedDtawithStp=currentDatasetting.StepData.GroupBy(p => p.Step_id).ToDictionary(p => p.Key, x => x.ToList());
            if (hashedDtawithStp.Values.Any(p=>(p==null?0:p.Count)>1))
            {
                HintByMessageBox("There are dirty datavalues. \r\nMore than values for one step in this data set!");
                //return;
            }
            var t = from stp in this.currentXmlTestCaseInfo.TestSteps
                    join d in this.CurrentDatasetting.StepData on stp.TestStepId equals d.Step_id into stp_dt
                    from rslt in stp_dt.DefaultIfEmpty()
                    select
                        new
                        {
                            stepinfo = stp,
                            data = stp_dt.FirstOrDefault()
                        };
            if (t == null)
            {
                CurrentXmlTestCaseInfo.TestCase_Steps_For_Preview = null;
                return;
            }

            ObservableCollection<MarsImpExp_TestStepWithData> lstToDisplayTmp = new ObservableCollection<MarsImpExp_TestStepWithData>();
            foreach (var itm in t.ToList().OrderBy(p => p.stepinfo.RunOrder))
            {
                lstToDisplayTmp.Add(new MarsImpExp_TestStepWithData() { Test_Step = itm.stepinfo, Test_Data = itm.data });
            }
            CurrentXmlTestCaseInfo.TestCase_Steps_For_Preview = lstToDisplayTmp;
            RaisePropertyChanged("CurrentXmlTestCaseInfo");

        }



        public string CurrentObjIdentifier
        {
            get
            {
                if (currentTestCaseApplication == null) return "N/A";
                if (currentTest_Step == null) return "N/A";
                long lObjId = currentTest_Step.Test_Step.ObjectId;
                MarsImpExp_Node_Object objTarget;
                List<MarsImpExp_Node_ParentObject> objParents = currentXmlTestCaseInfo.ParentObjects.Where(p => p.ApplicationId == currentTestCaseApplication.APPLICATION_ID).ToList();
                if (objParents == null)
                {
                    return "N/A";
                }
                //if (string.Compare("PegWindow", currentTest_Step.Test_Step.Keyword)==0)
                //{
                MarsImpExp_Node_ParentObject objParent = objParents.Where(p => p.ObjectId == currentTest_Step.Test_Step.ObjectId).FirstOrDefault();
                if (objParent != null)
                    return objParent.QuickAcess;
                //}
                foreach (var itm in objParents)
                {
                    if (itm == null) continue;
                    if (itm.ChildObjects == null) continue;
                    objTarget = itm.ChildObjects.Where(z => z.ObjectId == currentTest_Step.Test_Step.ObjectId).FirstOrDefault();
                    if (objTarget == null) continue;
                    return objTarget.QuickAcess;
                }

                return "N/A";
            }
            set
            {
                if (currentTestCaseApplication == null)
                    return;
                if (currentTest_Step == null)
                    return;
                long lObjId = currentTest_Step.Test_Step.ObjectId;
                MarsImpExp_Node_Object objTarget;
                List<MarsImpExp_Node_ParentObject> objParents = currentXmlTestCaseInfo.ParentObjects.Where(p => p.ApplicationId == currentTestCaseApplication.APPLICATION_ID).ToList();
                if (objParents == null)
                {
                    return;
                }
                if (string.Compare("PegWindow", currentTest_Step.Test_Step.Keyword) == 0)
                {
                    MarsImpExp_Node_ParentObject objParent = objParents.Where(p => p.ObjectId == currentTest_Step.Test_Step.ObjectId).FirstOrDefault();
                    if (objParent == null) return;
                    objParent.QuickAcess = value;
                    return;
                }
                foreach (var itm in objParents)
                {
                    if (itm == null) continue;
                    if (itm.ChildObjects == null) continue;
                    objTarget = itm.ChildObjects.Where(z => z.ObjectId == currentTest_Step.Test_Step.ObjectId).FirstOrDefault();
                    if (objTarget == null) continue;
                    objTarget.QuickAcess = value;
                    return;
                }

            }
        }
    }
}
