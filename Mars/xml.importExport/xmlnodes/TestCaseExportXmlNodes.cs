using Mars.Business;
using Mars.Dto;
using Mars.ViewModel;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mars.Model;
using Mars.DataLayer;
using System.Threading;

namespace Mars.xml.importExport.xmlnodes
{
    public class MarsImpExpConst
    {
        internal const string cnst_MarsExpImpRoot = "MarsExpImpRoot";

        internal const string cnst_ApplicationsRoot = "MARS_APPS";
        internal const string cnst_ApplicationNode = "Mars_app";
        internal const string cnst_MarsObjects = "Mars_Objects";
        internal const string cnst_test_steps_root = "Mars_TestSteps";
        internal const string cnst_DataSet_Root = "Mars_DataSets";
        internal const string cnst_Test_caseInfo = "Mars_TestCase";

        #region application part
        internal const string cnst_app_AppName = "App_Name";
        internal const string cnst_app_Desc = "App_Desc";
        internal const string cnst_app_Addins = "App_Addins";
        internal const string cnst_app_Version = "App_Ver";
        internal const string cnst_app_Id = "App_Id";
        internal const string cnst_app_TypeName = "App_Type";
        internal const string cnst_app_Identifier = "App_Identifier";
        internal const string cnst_app_StartPath = "App_Path";

        //internal const string 
        #endregion //application part

        #region Mars objects section
        internal const string cnst_object_root = "Mars_Object";
        internal const string cnst_object_name = "ObjectName";
        internal const string cnst_object_appid = "Application_Id";
        internal const string cnst_object_quickAccess = "Identifier";
        internal const string cnst_object_type = "ObjectType";
        internal const string cnst_object_children = "ChildrenObjects";
        internal const string cnst_object_id = "Object_Id";
        internal const string cnst_object_typeName = "ObjectTestType";
        internal const string cnst_object_Enum = "objectEnum";
        internal const string cnst_object_description = "Description";

        #endregion //Mars objects section

        #region Test steps
        internal const string cnst_steps_node = "Mars_TestStep";
        internal const string cnst_steps_keyword_name = "Keyword";
        internal const string cnst_steps_isskip = "IsSkip";
        internal const string cnst_steps_id = "Step_Id";
        internal const string cnst_steps_object_id = "Step_Object";
        internal const string cnst_steps_object_name = "ObjectName"; // for reference, 
        internal const string cnst_steps_para = "Step_Parameter";
        internal const string cnst_steps_runorder = "Run_Order";

        #endregion //test steps

        #region Test Data
        internal const string cnst_steps_testData_root = "Step_Datas";
        internal const string cnst_steps_testData = "Step_Data";
        internal const string cnst_steps_testData_setName = "DataSetName";
        internal const string cnst_steps_testData_setId = "DataSetId";


        #endregion //Test data

        #region Data Set
        internal const string cnst_data_set = "Mars_DataSet";
        internal const string cnst_steps_testData_desc = "DataSet_Description";
        internal const string cnst_data_record_root = "Step_Datas";
        internal const string cnst_data_record = "Step_Data";
        internal const string cnst_data_DataValue = "Data_Value";
        internal const string cnst_data_ValueOrObj = "Value_Or_Object";
        internal const string cnst_data_DataDirection = "Data_Direction";
        internal const string cnst_data_StepDescription = "Data_Desc";
        #endregion

        #region TestCase
        internal const string cnst_testcase_Name = "TestCase_Name";
        internal const string cnst_testcase_Desc = "TestCase_Desc";
        #endregion //Test case

        #region project export
        internal const string cnst_project_root = "Mars_Project";
        internal const string cnst_project_name = "Project_Name";
        internal const string cnst_project_desc = "Project_Description";
        internal const string cnst_project_rel_app = "AssignedApplications";
        internal const string cnst_project_app = "ApplicationInfo";
        internal const string cnst_project_app_name = "Application_Name";
        internal const string cnst_project_app_id = "Application_Id";

        internal const string cnst_project_exp_desc = "DescriptionOfExp";

        internal const string cnst_project_rel_testsuites = "TestSuites";
        internal const string cnst_project_rel_testsuite = "TS";
        internal const string cnst_project_test_suite_name = "Name";
        internal const string cnst_project_test_suite_desc = "Description";
        internal const string cnst_project_test_suite_Id = "Id";

        internal const string cnst_project_storyboard_root = "StoryboardsRoot";
        internal const string cnst_project_storyboard = "Storyboard";
        internal const string cnst_project_storyboard_Details = "Storyboard_Details";
        internal const string cnst_project_storyboard_Detail = "StepDetail";
        internal const string cnst_project_storyboard_DetailItem = "";
        internal const string cnst_project_storyboardname = "StoryboardName";
        internal const string cnst_project_storyboard_desc = "Description";

        internal const string cnst_sb_dtl_Action = "Action";
        internal const string cnst_sb_dtl_RunOrd = "RunOrder";
        internal const string cnst_sb_dtl_TestSuiteId = "TestSuite_Id";
        internal const string cnst_sb_dtl_TestSuiteName = "TestSuite_Name";
        internal const string cnst_sb_dtl_TestCaseId = "TestCase_Id";
        internal const string cnst_sb_dtl_TestCaseName = "TestCase_Name";
        internal const string cnst_sb_dtl_AliasName = "Alias_Name";
        internal const string cnst_sb_dtl_DS_Id = "DataSet_Id";
        internal const string cnst_sb_dtl_DS_Name = "DataSet_Name";
        #endregion
    }

    public class MarsImpExp_Node_TestCaseInfo:Notify
    {
        private T_TEST_CASE_SUMMARYDTO AssignTestcaseInfo = null;

        private string testCaseName;
        [XmlElement(MarsImpExpConst.cnst_testcase_Name)]
        public string TestCaseName
        {
            get { return testCaseName; }
            set {
                testCaseName = value;
                OnPropertyChanged("TestCaseName");
                if (testcaseNameFromXml == null)
                    testcaseNameFromXml = value;
            }
        }

        private string testcaseNameFromXml = null;
        [XmlIgnore]
        private string TestCaseNameFromXml
        {
            get
            {
                return testcaseNameFromXml;
            }
            set
            {
                testcaseNameFromXml = value;
                OnPropertyChanged("TestCaseNameFromXml");
            }
        }

        private string testcaseDesc;
        [XmlElement(MarsImpExpConst.cnst_testcase_Desc)]
        public string TestcaseDesc
        {
            get { return testcaseDesc; }
            set { testcaseDesc = value;  OnPropertyChanged("TestcaseDesc"); }
        }

        internal static MarsImpExp_Node_TestCaseInfo ConvertFromDTO(T_TEST_CASE_SUMMARYDTO objDTO)
        {
            if (objDTO == null) return null;
            MarsImpExp_Node_TestCaseInfo objResult = new MarsImpExp_Node_TestCaseInfo();
            objResult.testCaseName = objDTO.TEST_CASE_NAME;
            objResult.TestcaseDesc = objDTO.TEST_STEP_DESCRIPTION;
            objResult.AssignTestcaseInfo = objDTO;

            return objResult;
        }

        
        private long testCaseIdFromImportFileName;
        [XmlIgnore]
        public long TestCaseIdFromImportFileName
        {
            get
            {
                return testCaseIdFromImportFileName;
            }
            set
            {
                testCaseIdFromImportFileName = value;
            }
        }

        private long testCaseId=-1;
        [XmlIgnore]
        public long TestCaseId {
            get {
                return testCaseId;
            }
            set {
                testCaseId = value;
            }
        }
    }

  
    public class MarImpExp_Node_AppItem
    {
        private T_REGISTERED_APPSDTO AssignedAppDto = null;

        [XmlIgnore]
        public T_REGISTERED_APPSDTO targetApplication = new T_REGISTERED_APPSDTO();

        [XmlElement(MarsImpExpConst.cnst_app_AppName)]
        public string APP_SHORT_NAME
        {
            get
            {
                if (AssignedAppDto == null) return "";
                return AssignedAppDto.APP_SHORT_NAME;
            }
            set
            {
                CheckAndInitAppObjIfNull();
                AssignedAppDto.APP_SHORT_NAME = value;
            }
        }
        [XmlElement(MarsImpExpConst.cnst_app_Desc)]
        public string COMMENT
        {
            get
            {
                if (AssignedAppDto == null) return "";
                return AssignedAppDto.COMMENT;
            }
            set
            {
                CheckAndInitAppObjIfNull();
                AssignedAppDto.COMMENT = value;
            }
        }
        [XmlAttribute(MarsImpExpConst.cnst_app_Id)]
        public long APPLICATION_ID
        {
            get
            {
                if (AssignedAppDto == null) return -1;
                return AssignedAppDto.APPLICATION_ID;
            }
            set
            {
                CheckAndInitAppObjIfNull();
                AssignedAppDto.APPLICATION_ID = value;
            }
        }

        private long AppType;
        [XmlIgnore]
        public long Application_Type_id_fromName
        {
            get
            {
                return AppType;
            }
        }

        [XmlElement(MarsImpExpConst.cnst_app_TypeName)]
        public string APPLICATION_TYPE_NAME
        {
            get
            {
                return SubAppMgr.GetAppTypeNameById(MarsMainWindow.CurrentDatabaseIdx ,AppType);
            }
            set
            {
                AppType = SubAppMgr.GetAppTypeIdByName(MarsMainWindow.CurrentDatabaseIdx, value);
            }
        }

        internal class DBSubService
        {
            MarImpExp_Node_AppItem outterRef;
            internal DBSubService(MarImpExp_Node_AppItem o)
            {
                outterRef = o;
            }
            internal B_REGISTERED_APPS Convert2()
            {
                B_REGISTERED_APPS objResult = new B_REGISTERED_APPS();
                objResult.APPLICATION_ID = outterRef.APPLICATION_ID;
                objResult.APPLICATION_TYPE_ID = B_REGISTERED_APPS.GetApplicationTypeIdByString(outterRef.APPLICATION_TYPE_NAME);
                objResult.APP_SHORT_NAME = outterRef.APP_SHORT_NAME;
                objResult.COMMENT = outterRef.COMMENT;
                objResult.VERSION = outterRef.VERSION;
                objResult.PROCESS_IDENTIFIER = outterRef.PROCESS_IDENTIFIER;
                
                //objResult.
                return objResult;
            }
        }

        class SubAppMgr
        {
            private static List<B_SYSTEM_LOOKUP> AppTypeList = null;
            internal static string GetAppTypeNameById(string strDBIdx,long appType)
            {
                if (AppTypeList == null)
                    AppTypeList = B_SYSTEM_LOOKUP.ApplicationTypes(strDBIdx);
                if (AppTypeList == null) return "";
                var itm = AppTypeList.Where(p => p.VALUE == appType).FirstOrDefault();
                if (itm == null) return "";
                return itm.DISPLAY_NAME;
            }
            internal static long GetAppTypeIdByName(string strDBIdx, string strAppName)
            {
                if (AppTypeList == null)
                    AppTypeList = B_SYSTEM_LOOKUP.ApplicationTypes(strDBIdx);
                if (AppTypeList == null) return -1;
                var itm = AppTypeList.Where(p => string.Compare(p.DISPLAY_NAME, strAppName, true) == 0).FirstOrDefault();
                if (itm == null) return -1;
                return itm.VALUE ?? -1;
            }
        }
        [XmlElement(MarsImpExpConst.cnst_app_Identifier)]
        public string PROCESS_IDENTIFIER
        {
            get
            {
                return AssignedAppDto == null ? "" : AssignedAppDto.PROCESS_IDENTIFIER;
            }
            set
            {
                CheckAndInitAppObjIfNull();
                AssignedAppDto.PROCESS_IDENTIFIER = value;
            }
        }
        [XmlElement(MarsImpExpConst.cnst_app_StartPath)]
        public string STARTER_PATH
        {
            get
            {
                return this.AssignedAppDto == null ? "" : this.AssignedAppDto.STARTER_PATH;
            }
            set
            {
                CheckAndInitAppObjIfNull();
                AssignedAppDto.STARTER_PATH = value;
            }
        }

        [XmlElement(MarsImpExpConst.cnst_app_Addins)]
        public string EXTRAREQUIREMENT
        {
            get
            {
                return this.AssignedAppDto == null ? "" : this.AssignedAppDto.EXTRAREQUIREMENT;
            }
            set
            {
                CheckAndInitAppObjIfNull();
                this.AssignedAppDto.EXTRAREQUIREMENT = value;
            }
        }


        [XmlElement(MarsImpExpConst.cnst_app_Version)]
        public string VERSION
        {
            get { return this.AssignedAppDto == null ? "" : this.AssignedAppDto.VERSION; }
            set
            {
                CheckAndInitAppObjIfNull();
                AssignedAppDto.VERSION = value;
            }
        }

        private void CheckAndInitAppObjIfNull()
        {
            if (AssignedAppDto == null)
                AssignedAppDto = new T_REGISTERED_APPSDTO();
        }
        public MarImpExp_Node_AppItem(T_REGISTERED_APPSDTO objAssignedAppDto)
        {
            this.AssignedAppDto = objAssignedAppDto;
        }

        public MarImpExp_Node_AppItem()
        {
            this.AssignedAppDto = new T_REGISTERED_APPSDTO();
        }

        public static ObservableCollection<MarImpExp_Node_AppItem> ConvertFrom(List<T_REGISTERED_APPSDTO> lstDBDTO)
        {
            if (lstDBDTO == null) return null;
            ObservableCollection<MarImpExp_Node_AppItem> lstRslt = new ObservableCollection<MarImpExp_Node_AppItem>();
            foreach (var itm in lstDBDTO)
            {
                MarImpExp_Node_AppItem objAppItm = ConvertFrom(itm);
                if (objAppItm == null) continue;
                lstRslt.Add(objAppItm);
            }
            return lstRslt;
        }

        internal static MarImpExp_Node_AppItem ConvertFrom(T_REGISTERED_APPSDTO objDTO)
        {
            if (objDTO == null) return null;
            MarImpExp_Node_AppItem objNodeApp = new MarImpExp_Node_AppItem(objDTO);
            return objNodeApp;
        }
    }

    public class MarsImpExp_Node_base:Notify
    {
        protected V_TEST_STEPS_FULLVISIONDTO assignedTestStepWithObjectInfo = null;
    }

    public class MarsImpExp_Node_Object : MarsImpExp_Node_base
    {
        [XmlIgnore]
        public T_REGISTED_OBJECTDTO targetObject;
        [XmlIgnore]
        public MarImpExp_Node_AppItem assignedApp;

        [XmlElement(MarsImpExpConst.cnst_object_name)]
        public string ObjectName
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return null;
                return assignedTestStepWithObjectInfo.OBJECT_HAPPY_NAME;

            }
            set
            {
                if (assignedTestStepWithObjectInfo == null)
                    assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.OBJECT_HAPPY_NAME = value;
                OnPropertyChanged("ObjectName");
            }
        }
        [XmlElement(MarsImpExpConst.cnst_object_quickAccess)]
        public string QuickAcess
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return null;
                return assignedTestStepWithObjectInfo.QUICK_ACCESS;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null) assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.QUICK_ACCESS = value;
                OnPropertyChanged("QuickAcess");
            }
        }

        
        [XmlAttribute(MarsImpExpConst.cnst_object_Enum)]
        public string ObjectEnum
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return null;
                return assignedTestStepWithObjectInfo.ENUM_TYPE;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null) assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.ENUM_TYPE = value;
                OnPropertyChanged("ObjectEnum");
            }
        }
        [XmlAttribute(MarsImpExpConst.cnst_object_typeName)]
        public string ObjectTestType
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return null;
                return assignedTestStepWithObjectInfo.TYPE_NAME;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null) assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.TYPE_NAME = value;
                OnPropertyChanged("ObjectTestType");
            }
        }

        [XmlAttribute(MarsImpExpConst.cnst_object_type)]
        public string ObjectType
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return null;
                return assignedTestStepWithObjectInfo.OBJECT_TYPE;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null) assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.OBJECT_TYPE = value;
            }
        }
        [XmlAttribute(MarsImpExpConst.cnst_object_appid)]
        public long ApplicationId
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return -1;
                return assignedTestStepWithObjectInfo.APPLICATION_ID ?? -1;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null)
                    assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.APPLICATION_ID = value;
            }
        }
        [XmlAttribute(MarsImpExpConst.cnst_object_id)]
        public long ObjectId
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return -1;
                return assignedTestStepWithObjectInfo.OBJECT_ID;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null)
                    assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.OBJECT_ID = value;
            }
        }

        public MarsImpExp_Node_Object(V_TEST_STEPS_FULLVISIONDTO objStpInfo)
        {
            assignedTestStepWithObjectInfo = objStpInfo;
        }
        public MarsImpExp_Node_Object()
        {

        }
        private ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> typeListCache = null;
        public B_REGISTED_OBJECT ConvertTo()
        {
            B_REGISTED_OBJECT objDto = new B_REGISTED_OBJECT();
            if (typeListCache == null)
                typeListCache = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeListEx(MarsMainWindow.CurrentDatabaseIdx);

            objDto.APPLICATION_ID = this.ApplicationId;
            objDto.COMMENT = "";
            objDto.ENUM_TYPE = this.ObjectEnum;
            objDto.OBJECT_ID = this.ObjectId;
            objDto.OBJECT_NAME_ID = null;
            objDto.OBJECT_TYPE = this.ObjectType;
            objDto.QUICK_ACCESS = this.QuickAcess;
            objDto.OBJECT_HAPPY_NAME = this.ObjectName;

            B_GUI_COMPONENT_TYPE_DIC objTmpType = typeListCache.Where(p => string.Compare(p.TYPE_NAME, this.ObjectTestType, true) == 0).FirstOrDefault();
            objDto.TYPE_ID = objTmpType == null ? -1 : objTmpType.TYPE_ID;

            objDto.T_GUI_COMPONENT_TYPE_DIC = (new T_GUI_COMPONENT_TYPE_DICDTO()).ToEntity();
            objDto.T_GUI_COMPONENT_TYPE_DIC.TYPE_ID = objDto.TYPE_ID??-1;
            objDto.T_GUI_COMPONENT_TYPE_DIC.TYPE_NAME = this.ObjectTestType;
            objDto.T_GUI_COMPONENT_TYPE_DIC.APPLICATION_TYPE_ID = null;
            objDto.T_GUI_COMPONENT_TYPE_DIC.DESCRIPTION = "From Import";

            
            return objDto;
        }
    }

    public class MarsImpExp_Node_ParentObject : MarsImpExp_Node_Object
    {

        
        /****
        Demo:
        <ObjectName>SWAP_TRADE</ObjectName>
        <QuickAccess>.....</QuickAccess>
        <Children>
            <Mars_Object application_id="1" ObjectType="SwfEdit" Object_Id="100232">
                <ObjectName ></ObjectName>
                <QuickAccess>.....</QuickAccess>
            </Mars_Object>
        </Children>
        **/
        [XmlArray(MarsImpExpConst.cnst_object_children)]
        [XmlArrayItem(MarsImpExpConst.cnst_object_root)]
        public List<MarsImpExp_Node_Object> ChildObjects { get; set; }

        internal List<B_REGISTED_OBJECT> ConvertToBusinessObj()
        {
            List<B_REGISTED_OBJECT> lstRslt = new List<B_REGISTED_OBJECT>();
            B_REGISTED_OBJECT objParentB = this.ConvertTo();
            //lstRslt.Add(objParentB);
            if (ChildObjects == null) return lstRslt;
            foreach(var itm in ChildObjects)
            {
                B_REGISTED_OBJECT objTmp = itm.ConvertTo();
                lstRslt.Add(objTmp);
            }
            return lstRslt;
        }
    }

    public class MarsImpExp_Node_Step_Data:Notify
    {
        internal TEST_DATA_SETTINGDTO AssignedStep = null;
        [XmlAttribute(MarsImpExpConst.cnst_steps_id)]
        public long Step_id
        {
            get
            {
                if (AssignedStep == null)
                    return -1;
                return AssignedStep.STEPS_ID;
            }
            set
            {
                if (AssignedStep == null)
                    AssignedStep = new TEST_DATA_SETTINGDTO();
                AssignedStep.STEPS_ID = value;
                OnPropertyChanged("Step_id");
            }
        }

        [XmlElement(MarsImpExpConst.cnst_data_DataValue)]
        public string DataValue
        {
            get
            {
                if (AssignedStep == null)
                    return null;
                return AssignedStep.DATA_VALUE;
            }
            set
            {
                if (AssignedStep == null)
                    AssignedStep = new TEST_DATA_SETTINGDTO();
                AssignedStep.DATA_VALUE = value;
                OnPropertyChanged("DataValue");
            }
        }
        [XmlAttribute(MarsImpExpConst.cnst_data_DataDirection)]
        public short DataDirection
        {
            get {
                if (AssignedStep == null)
                    return -1;
                return AssignedStep.DATA_DIRECTION ?? -1;
            }
            set
            {
                if (AssignedStep == null)
                    AssignedStep = new TEST_DATA_SETTINGDTO();
                AssignedStep.DATA_DIRECTION = value;
                OnPropertyChanged("DataDirection");
            }
        }
        [XmlAttribute(MarsImpExpConst.cnst_data_ValueOrObj)]
        public short DataOrObjectMark
        {
            get
            {
                return (short)(AssignedStep == null ? -1 : (AssignedStep.VALUE_OR_OBJECT??-1));
            }
            set
            {
                if (AssignedStep == null) AssignedStep = new TEST_DATA_SETTINGDTO();
                AssignedStep.VALUE_OR_OBJECT = value;
                OnPropertyChanged("DataOrObjectMark");
            }
        }
        [XmlElement(MarsImpExpConst.cnst_data_StepDescription)]
        public string Description
        {
            get {
                return this.AssignedStep == null ? null : this.AssignedStep.DESCRIPTION;
            }
            set
            {
                if (this.AssignedStep == null)
                    this.AssignedStep = new TEST_DATA_SETTINGDTO();
                this.AssignedStep.DESCRIPTION = value;
                OnPropertyChanged("Description");
            }
        }
    }

    public class MarsImpExp_Node_TestData:Notify
    {
        [XmlIgnore]
        public T_TEST_DATA_SUMMARYDTO targetDataSet;

        internal T_TEST_DATA_SUMMARYDTO AssignedDataSummary = null;
        [XmlAttribute(MarsImpExpConst.cnst_steps_testData_setName)]
        public string DataSetName
        {
            get
            {
                if (AssignedDataSummary == null) return null;
                return AssignedDataSummary.ALIAS_NAME;
            }
            set
            {
                if (AssignedDataSummary == null)
                    AssignedDataSummary = new T_TEST_DATA_SUMMARYDTO();
                AssignedDataSummary.ALIAS_NAME = value;
                OnPropertyChanged("DataSetName");
            }
        }

        [XmlAttribute(MarsImpExpConst.cnst_steps_testData_setId)]
        public long DataSetId
        {
            get
            {
                if (AssignedDataSummary == null)
                    return -1;
                return AssignedDataSummary.DATA_SUMMARY_ID;
            }
            set
            {
                if (AssignedDataSummary == null)
                    AssignedDataSummary = new T_TEST_DATA_SUMMARYDTO();
                AssignedDataSummary.DATA_SUMMARY_ID = value;
                OnPropertyChanged("DataSetId");
            }
        }

        [XmlElement(MarsImpExpConst.cnst_steps_testData_desc)]
        public string Description
        {
            get
            {
                if (AssignedDataSummary == null) return null;
                return AssignedDataSummary.DESCRIPTION_INFO;
            }
            set
            {
                if (AssignedDataSummary == null) AssignedDataSummary = new T_TEST_DATA_SUMMARYDTO();
                AssignedDataSummary.DESCRIPTION_INFO = value;
                OnPropertyChanged("Description");
            }
        }

        [XmlArray(MarsImpExpConst.cnst_data_record_root)]
        [XmlArrayItem(MarsImpExpConst.cnst_data_record)]
        public List<MarsImpExp_Node_Step_Data> StepData;

        private long newDataSetId;
        [XmlIgnore]
        public long NewDataSetId
        {
            get { return newDataSetId; }
            set { newDataSetId = value; }
        }
    }

    public class MarsImpExp_TestStepWithData:Notify
    {
        private MarsImpExp_Node_Test test_step;
        public MarsImpExp_Node_Test Test_Step
        {
            get { return test_step; }
            set
            {
                test_step = value;
                OnPropertyChanged("Test_Step");
            }
        }

        private MarsImpExp_Node_Step_Data test_Data;
        public MarsImpExp_Node_Step_Data Test_Data
        {
            get { return test_Data; }
            set {
                test_Data = value;
                OnPropertyChanged("Test_Data");
            }
        }
    }

    public class MarsImpExp_Node_Test : MarsImpExp_Node_base
    {
        [XmlIgnore]
        public Dictionary<MarImpExp_Node_AppItem, MarsImpExp_Node_Object> AssignedObjects=new Dictionary<MarImpExp_Node_AppItem, MarsImpExp_Node_Object>();
        [XmlIgnore]
        public Dictionary<MarsImpExp_Node_TestData, MarsImpExp_Node_Step_Data> AssignedDataAndDataSet = new Dictionary<MarsImpExp_Node_TestData, MarsImpExp_Node_Step_Data>();
        [XmlIgnore]
        public T_KEYWORDDTO targetKeyword=null;
        [XmlIgnore]
        public T_OBJECT_NAMEINFODTO TargetObjectName = null;

        [XmlAttribute(MarsImpExpConst.cnst_steps_id)]
        public long TestStepId
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return -1;
                return assignedTestStepWithObjectInfo.STEPS_ID;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null) assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.STEPS_ID = value;

                OnPropertyChanged("TestStepId");
            }
        }

        [XmlAttribute(MarsImpExpConst.cnst_steps_keyword_name)]
        public string Keyword
        {
            get
            {
                if (this.assignedTestStepWithObjectInfo == null) return "";
                return assignedTestStepWithObjectInfo.KEY_WORD_NAME;
            }
            set
            {
                if (this.assignedTestStepWithObjectInfo == null)
                    this.assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.KEY_WORD_NAME = value;
                OnPropertyChanged("Keyword");
            }
        }
        [XmlAttribute(MarsImpExpConst.cnst_steps_object_id)]
        public long ObjectId
        {
            get
            {
                if (this.assignedTestStepWithObjectInfo == null) return -1;
                return assignedTestStepWithObjectInfo.OBJECT_ID;
            }
            set
            {
                if (this.assignedTestStepWithObjectInfo == null)
                    assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                //long lv;
                assignedTestStepWithObjectInfo.OBJECT_ID = value;
            }
        }

        [XmlAttribute(MarsImpExpConst.cnst_steps_object_name)]
        public string ObjectName
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return null;
                return assignedTestStepWithObjectInfo.OBJECT_HAPPY_NAME;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null)
                    assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.OBJECT_HAPPY_NAME = value;
                OnPropertyChanged("ObjectName");
            }
        }

        [XmlAttribute(MarsImpExpConst.cnst_steps_runorder)]
        public Int64 RunOrder
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return -1;
                return assignedTestStepWithObjectInfo.RUN_ORDER;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null)
                    assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.RUN_ORDER = value;
                OnPropertyChanged("RunOrder");
            }
        }
        [XmlElement(MarsImpExpConst.cnst_steps_para)]
        public string StepsParamenter
        {
            get
            {
                if (assignedTestStepWithObjectInfo == null) return null;
                return assignedTestStepWithObjectInfo.COLUMN_ROW_SETTING;
            }
            set
            {
                if (assignedTestStepWithObjectInfo == null)
                    assignedTestStepWithObjectInfo = new V_TEST_STEPS_FULLVISIONDTO();
                assignedTestStepWithObjectInfo.COLUMN_ROW_SETTING = value;
                OnPropertyChanged("StepsParamenter");
            }
        }
        //[XmlArray(MarsImpExpConst.cnst_steps_testData_root)]
        //[XmlArrayItem(MarsImpExpConst.cnst_steps_testData)]
        //public List<MarsImpExp_Node_TestData> TestDataRecords
        //{

        //}

        private long newStepId;
        [XmlIgnore]
        public long NewStepId
        {
            get { return newStepId; }
            set { newStepId = value; }
        }


        private string currentData;
        [XmlIgnore]
        public string CurrentData
        {
            get {
                return currentData;
            }
            set
            {
                currentData = value;
                OnPropertyChanged("CurrentData");
            }
        }
    }

    [Serializable]
    [XmlRoot(ElementName = MarsImpExpConst.cnst_MarsExpImpRoot)]
    public class TestCaseExportXmlNodes: INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseExportXmlNodes));
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        [XmlIgnore]
        public T_TEST_CASE_SUMMARYDTO targetTestCase;

        private MarsImpExp_Node_TestCaseInfo testCaseNodeInfo;
        [XmlElement(MarsImpExpConst.cnst_Test_caseInfo)]
        public MarsImpExp_Node_TestCaseInfo TestCaseNodeInfo{
            get { return testCaseNodeInfo; }
            set {
                testCaseNodeInfo = value;
                RaisePropertyChanged("TestCaseNodeInfo");
            }
        } 

        private ObservableCollection<MarImpExp_Node_AppItem> marsApp;
        [XmlArrayItem(MarsImpExpConst.cnst_ApplicationNode)]
        [XmlArray(MarsImpExpConst.cnst_ApplicationsRoot)]
        public ObservableCollection<MarImpExp_Node_AppItem> MarsApps { get { return marsApp; } set { marsApp = value; RaisePropertyChanged("MarsApps"); } }

        [XmlArrayItem(MarsImpExpConst.cnst_object_root)]
        [XmlArray(MarsImpExpConst.cnst_MarsObjects)]
        public List<MarsImpExp_Node_ParentObject> ParentObjects { get; set; }


        private ObservableCollection<MarsImpExp_Node_TestData> dataSetWithSettingDataRecords;
        [XmlArray(MarsImpExpConst.cnst_DataSet_Root)]
        [XmlArrayItem(MarsImpExpConst.cnst_data_set)]
        public ObservableCollection<MarsImpExp_Node_TestData> DataSetWithSettingDataRecords
        {
            get { return dataSetWithSettingDataRecords; }
            set { dataSetWithSettingDataRecords = value;
                RaisePropertyChanged("DataSetWithSettingDataRecords");  }
        }

        private MarsImpExp_Node_TestData currentDataSet;
        [XmlIgnore]
        public MarsImpExp_Node_TestData CurrentDataSet
        {
            get
            {
                return currentDataSet;
            }
            set
            {
                if (currentDataSet != value)
                {
                    currentDataSet = value;
                    RaisePropertyChanged("CurrentDataSet");

                    ///更新test step中的数据项
                    /// 
                    UpdateTeststepsDataWhenCurrentDataSetChange();
                }
            }
        }

        private void UpdateTeststepsDataWhenCurrentDataSetChange()
        {
            if (currentDataSet == null) return;
            if ((currentDataSet.StepData == null) || (currentDataSet.StepData.Count == 0)) return;
            if (testSteps == null) return;

            foreach (var stp in testSteps)
            {
                if (stp == null) continue;
                var stpData = currentDataSet.StepData.Where(p=>p.Step_id==stp.TestStepId).FirstOrDefault();
                if (stpData == null) continue;
                stp.CurrentData = stpData.DataValue;
            }
        }

        private ObservableCollection<MarsImpExp_Node_Test> testSteps { get; set; }
        [XmlArray(MarsImpExpConst.cnst_test_steps_root)]
        [XmlArrayItem(MarsImpExpConst.cnst_steps_node)]
        public ObservableCollection<MarsImpExp_Node_Test> TestSteps
        {
            get
            {
                return testSteps;
            }
            set
            {
                if (value!=null)
                {
                    testSteps= new ObservableCollection<MarsImpExp_Node_Test>(value.OrderBy(p => p.RunOrder));
                }else
                    testSteps = null;
                RaisePropertyChanged("TestSteps");
            }
        }


        private ObservableCollection<MarsImpExp_TestStepWithData> testCase_Steps_4Preview;
        public ObservableCollection<MarsImpExp_TestStepWithData> TestCase_Steps_For_Preview
        {
            get { return testCase_Steps_4Preview; }
             set {
                testCase_Steps_4Preview = value;
                RaisePropertyChanged("TestCase_Steps_For_Preview");
            }
        }


        public List<MarsImpExp_Node_Object> GetAllObjectsByAPPID(long iAppId)
        {
            //ParentObjects
            return null;
        }

        public void CompoundTestStepWithDataForYuLang()
        {
            if (testSteps == null) return;

            //ObservableCollection<MarsImpExp_TestStepWithData> lstData = new ObservableCollection<MarsImpExp_TestStepWithData>();
            //foreach (var itm in dataSetWithSettingDataRecords)
            //{
            //    if (itm == null) continue;
            //    if (itm.AssignedDataSummary == null) continue;
            //    /// steps:
            //    /// 1, lstData中是否有相关的stepinfo
            //    /// 
            //    var lstStpsIds = lstData.Select(p => p.TestStepId).ToList();
            //    if ((lstStpsIds != null) && (lstStpsIds.Count> 0)) {
            //        var notIn = itm.StepData.Where(p => !lstStpsIds.Contains(p.Step_id));
            //        foreach(var itmNon  in notIn)
            //        {
            //            lstData.Add(new MarsImpExp_TestStepWithData(itm));
            //        }
            //        lstData.Add(new MarsImpExp_TestStepWithData() { })
            //    }
            //    if ()
            //}
            /// stepid, data set id and value 

                //List<KeyValuePair<MarsImpExp_Node_Step_Data, List<T_TEST_DATA_SUMMARYDTO>>> lstDataInfo = 
                //    new List<KeyValuePair<MarsImpExp_Node_Step_Data, List<T_TEST_DATA_SUMMARYDTO>>>();
                //foreach (var itm in dataSetWithSettingDataRecords)
                //{
                //    List<MarsImpExp_Node_Step_Data> lstDta = itm.StepData;
                //    List<long> stpIds = lstDta == null ? (new List<long>()) : lstDta.Select(p => p.Step_id).ToList();
                //    List<long> stpIdForRestore = lstDataInfo.Select(p => p.Key.Step_id).ToList();
                //    var notIn = itm.StepData.Where(p => !(stpIdForRestore.Contains(p.Step_id))) ;

                //    List<T_TEST_DATA_SUMMARYDTO> lstCurrntDto;
                //    foreach (var stmNotIn in notIn)
                //    {
                //        lstDataInfo.Add(new KeyValuePair<MarsImpExp_Node_Step_Data, List<T_TEST_DATA_SUMMARYDTO>>(stmNotIn, (lstCurrntDto=new List<T_TEST_DATA_SUMMARYDTO>())));
                //        lstCurrntDto.Add(itm.AssignedDataSummary);
                //    }
                //}

                //var data = from d in dataSetWithSettingDataRecords.
                //           select d.
                //var qStepWithData = from q in testSteps
                //                    from d in dataSetWithSettingDataRecords.
                //                    where q.TestStepId = d..
        }

        private List<B_REGISTERED_APPS> ConvertFromImportAppInfo()
        {
            List<B_REGISTERED_APPS> lstRslt = new List<B_REGISTERED_APPS>();
            foreach (MarImpExp_Node_AppItem appExported in marsApp)
            {
                if (appExported == null) continue;
                B_REGISTERED_APPS objTarget = (new MarImpExp_Node_AppItem.DBSubService(appExported)).Convert2();
                if (objTarget == null) continue;
                lstRslt.Add(objTarget);
                //objTarget.APPLICATION_ID
            }
            return lstRslt;
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

        private bool CreateTestCaseAndItsAppRelRec(long iTestCaseId, List<long> lstAppIds, MarsTransactionMgr objTrans, ref string strError)
        {
            Logger.logBegin("CreateTestCaseAndItsAppRelRec", string.Format("TestCaseId :[{0}] AppIds' Count:[{1}]", iTestCaseId, lstAppIds == null ? -1 : lstAppIds.Count));
            try
            {
                B_REL_APP_TESTCASE objAppTC = new B_REL_APP_TESTCASE();
                bool isOk = objAppTC.CreateAppWithTCId(
                    MarsMainWindow.CurrentDatabaseIdx,
                    iTestCaseId, lstAppIds, objTrans.CurrentDBContext, ref strError);
                return isOk;
            }
            finally
            {
                Logger.logEnd("CreateTestCaseAndItsAppRelRec");
            }
        }

        private bool InsertAndMappingTestSteps(ObservableCollection<MarsImpExp_Node_Test> lstStps, MarsImpExp_Node_TestCaseInfo nodeInfo,
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> dicObjectContainer,
            List<long> lstAppIds,
            MarsTransactionMgr objTrans, ref string strError)
        {
            Logger.logBegin("InsertAndMappingTestSteps", string.Format("TestCaseName:[{0}],TestCaseDesc:[{1}]", nodeInfo == null ? "N/A" : nodeInfo.TestCaseName, nodeInfo == null ? "N/A" : nodeInfo.TestcaseDesc));
            try
            {
                if (lstStps == null) return true;
                ///算法：
                /// 1，判断是否存在同名的Test case，如果存在，改名，附加_imp_MMddYYYY
                /// 2，使用新的testcase id
                /// 
                B_TEST_CASE objTstCase = new B_TEST_CASE();
                bool isOk = false,isTestCaseExists=false;
                string strNewTestcaseName = nodeInfo.TestCaseName;
                //ImportLogList.Add(new SimpleMarsLog("Create test case..."));

                long iTestCaseId = objTstCase.MergeTestCaseByNameAndDesc(
                    MarsMainWindow.CurrentDatabaseIdx,
                    strNewTestcaseName, nodeInfo.TestcaseDesc, ref strNewTestcaseName, ref strError,objTrans.CurrentDBContext, ref isTestCaseExists);
                ///这里 可以添加test case name变化的处理
                /// 
                isOk = iTestCaseId > 0;
                if (!isOk)
                {
                    Logger.Error("InsertAndMappingTestSteps", string.Format("Error when call MergeTestCaseByNameAndDesc.\r\n[{0}]", strError));
                    return false;
                }
                if (!isTestCaseExists)
                {
                    //如果是新的test case就添加
                    //添加 test case 和application的关系表
                    isOk = CreateTestCaseAndItsAppRelRec(iTestCaseId, lstAppIds, objTrans, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("InsertAndMappingTestSteps", string.Format("Error when call CreateTestCaseAndItsAppRelRec.\r\n[{0}]", strError));
                        return false;
                    }
                }
                //ImportLogList.Add(new SimpleMarsLog(string.Format("Done, new test case Id:[{0}] --uncommitted", iTestCaseId)));

                nodeInfo.TestCaseId = iTestCaseId;

                B_TEST_STEPS objStp = new B_TEST_STEPS();
                int iCnt = 0;
                long lNewStpId = -1;
                objStp.TEST_CASE_ID = iTestCaseId;

                List<B_REGISTED_OBJECT> lstAllObjects = new List<B_REGISTED_OBJECT>();
                foreach (var k in dicObjectContainer.Keys)
                {
                    if (k == null) continue;
                    lstAllObjects.Add(k);
                    lstAllObjects.AddRange(dicObjectContainer[k]);
                }

                foreach (MarsImpExp_Node_Test stpNode in lstStps)
                {
                    if (stpNode == null) continue;

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
                        lNewStpId = objStp.InsertNewStep(MarsMainWindow.CurrentDatabaseIdx, iTestCaseId, stpNode.Keyword, qo.NewObjectNameId, stpNode.RunOrder, stpNode.StepsParamenter, "Imported from XML", qo.NewObjectRegTableId, ref isOk, ref strError, objTrans.CurrentDBContext);


                    }
                    else
                    {
                        //无需对象的keyword
                        lNewStpId = objStp.InsertNewStep(MarsMainWindow.CurrentDatabaseIdx, iTestCaseId, stpNode.Keyword, null, stpNode.RunOrder, stpNode.StepsParamenter, "Imported from XML", null, ref isOk, ref strError, objTrans.CurrentDBContext);
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

        private bool CreateSharePoolAndTestDataRecord(long lDataSetId, ObservableCollection<MarsImpExp_Node_Test> lstStps, MarsImpExp_Node_TestData testDataXml,
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
                int iCount = 0;
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
                        Logger.Info("CreateSharePoolAndTestDataRecord", string.Format("count;[{0}] data:[{1}]", iCount++, testDataXml.StepData==null?"": testDataXml.StepData.ToString()));
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

        private bool InsertAndMappingDataSet(ObservableCollection<MarsImpExp_Node_TestData> dataSetWithSettingDataRecords,
            MarsImpExp_Node_TestCaseInfo testCaseNodeInfo,
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstObjectFromImportXml,
            ObservableCollection<MarsImpExp_Node_Test> testSteps,
            MarsTransactionMgr objTrans,
            ref string strError,
            ObservableCollection<string> lstLog)
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

                if (lstLog != null)
                    lstLog.Add(string.Format("\tBegin to deal with data set:[{0}]", dataItm.DataSetName));

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
                if (lstLog != null)
                    lstLog.Add(("\tAssignData set to Test Case..."));
                long lNewRelDat_Tc = objDataSetSum.AssignDataSetToTestCase(testCaseNodeInfo.TestCaseId, lNewDataSetId, objTrans.CurrentDBContext, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("InsertAndMappingDataSet", string.Format("Error when call B_T_TEST_DATA_SUMMARYDTO.AssignDataSetToTestCase. \r\n{0}", strError));
                    return false;
                }

                //=====
                objTrans.CurrentDBContext.SaveChanges();

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

        internal bool ImportTCXmlFileObjectsToDB( bool isUseOutterTrans, ref string strError, MarsTransactionMgr currentTrans, ObservableCollection<string> lstLog)
        {
            Logger.logBegin("ImportTCXmlFileObjectsToDB",string.Format("Use Outter Trans:[{0}] dbCntx:[{1}]", isUseOutterTrans, currentTrans == null?"NULL":"Non-NUll"));
            //  MarsEntities dbCntx = currentDBContext == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : currentDBContext;
            bool isOk = false;
            /// 1 create TC Object
            //B_TEST_CASE objTC = new B_TEST_CASE();
            //isOk = objTC.TestCaseExists(this.testCaseNodeInfo.TestCaseName, currentTrans.CurrentDBContext);
            //if (isOk)
            //{
            //    this.testCaseNodeInfo.TestCaseName += "_imp";
            //}
            //long lNewTCId = -1;
            //isOk = objTC.AddNewTestCase(new T_TEST_CASE_SUMMARYDTO()
            //{
            //    TEST_CASE_NAME = this.testCaseNodeInfo.TestCaseName,
            //    TEST_STEP_DESCRIPTION = this.testCaseNodeInfo.TestcaseDesc,
            //    TEST_STEP_CREATOR = "MARS_IMPORTER",
            //    USAGE_STATUS = 1,
            //    TEST_STEP_CREATE_TIME = DateTime.Now
            //}, currentTrans.CurrentDBContext, ref strError, ref lNewTCId);
            //if (!isOk)
            //{
            //    return false;
            //}
            //this.testCaseNodeInfo.TestCaseId = lNewTCId;

            List<B_REGISTERED_APPS> lstApps = ConvertFromImportAppInfo();
            List<KeyValuePair<long, T_REGISTERED_APPSDTO>> lstMappedApp = B_REGISTERED_APPS.CreateAppInfoIdMapping(
                MarsMainWindow.CurrentDatabaseIdx,
                lstApps, ref isOk, ref strError, currentTrans);
            if (!isOk)
            {
                Logger.Error("ImportTCXmlFileObjectsToDB", strError = string.Format("Can't import testcase with Error from Create new Application:\r\n[{0}]", strError));
                return false;
            }
            Logger.Info("ImportTCXmlFileObjectsToDB", string.Format("Applications are ok with [{0}] mapped applications", lstMappedApp.Count));
            /// 2，添加objects，返回objects的mapping
            /// objects的mapping信息在B_REGISTED_OBJECTS中newpeg, new objects
            /// 
            if (lstLog != null)
                lstLog.Add("\tBegin to deal Objects...");
            B_REGISTED_OBJECT objB = new B_REGISTED_OBJECT();
            Logger.Info("ImportTCXmlFileObjectsToDB", "Begin to deal Objects...");
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstObjectFromImportXml = ConverFromImportObjectsInfo(ParentObjects, ref isOk, ref strError);
            if (!isOk)
            {
                Logger.Error("ImportTCXmlFileObjectsToDB", string.Format("Error from ConverFromImportObjectsInfo:{0}", strError));
                return false;
            }

            objB.CreateObjectMappingForImport(lstMappedApp, lstObjectFromImportXml, ref isOk, ref strError, currentTrans,strDBIdx:MarsMainWindow.CurrentDatabaseIdx);
            if (!isOk)
            {
                Logger.Error("ImportTCXmlFileObjectsToDB", strError = string.Format("Can't import data with error:[{0}]", strError));
                //HintByMessageBox(string.Format("Error:{0}", strError), "Error");
                return false;
            }

            #region///3, Teststeps的处理
            //ImportLogList.Add(new SimpleMarsLog("Begin to deal test steps..."));
            if (lstLog != null)
                lstLog.Add("\tBegin to deal Test Steps...");

            isOk = InsertAndMappingTestSteps(testSteps, testCaseNodeInfo, lstObjectFromImportXml,
                lstMappedApp.Select(p => p.Key).ToList(),
                currentTrans, ref strError);
            if (!isOk)
            {
                Logger.Error("ImportTCXmlFileObjectsToDB", strError = string.Format("Error when Insert Test steps\r\n{0}", strError));
                //HintByMessageBox(strError, "Error");
                return false;
            }
            #endregion //3, Teststeps的处理

            #region ///4, Dataset 的处理
            isOk = InsertAndMappingDataSet(dataSetWithSettingDataRecords, testCaseNodeInfo, lstObjectFromImportXml, testSteps, currentTrans, ref strError, lstLog);
            if (!isOk)
            {
                Logger.Error("ImportTCXmlFileObjectsToDB", strError = string.Format("Error when call InsertAndMappingDataSet\r\n{0}", strError));
                //HintByMessageBox(strError, "Error");
                return false;
            }
            #endregion
            return true;
        }
    }
}
