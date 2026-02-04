using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Dto;
using System.Data.Objects;
using System.Collections.ObjectModel;
using Mars.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;
using Mars.DataLayer.Generic;
using Mars.Model;


namespace Mars.Business
{
    public class B_REGISTERED_APPS : T_REGISTERED_APPSDTO
    {
        private static ObservableCollection<B_REGISTERED_APPS> SystemRegistedApplications = null;

        public static List<KeyValuePair<int, string>> APPLICATION_TYPE_LIST = new List<KeyValuePair<int, string>>(){
            new KeyValuePair<int, string>(1,"Windows") ,
            new KeyValuePair<int, string>(2,"Java") ,
            new KeyValuePair<int, string>(4,"Web-Dojo") ,
            new KeyValuePair<int, string>(8,"Web-Extjs") ,
            new KeyValuePair<int, string>(16,"Web")            
        };
        public readonly static List<KeyValuePair<string, string>> APPLICATION_ADDINS_INFRAGISTICS = new List<KeyValuePair<string, string>>() {
            new KeyValuePair<string, string>("None","None"),
            new KeyValuePair<string, string>("INFRAGISTICS_V_11","Infragistitcs Version 11.X") ,
            new KeyValuePair<string, string>("INFRAGISTICS_V_12","Infragistitcs Version 12") ,
            new KeyValuePair<string, string>("INFRAGISTICS_V_122","Infragistitcs Version 12.2.X") ,
            new KeyValuePair<string, string>("INFRAGISTICS_V_14","Infragistitcs Version 14.X"),
            new KeyValuePair<string, string>("INFRAGISTICS_V_152","Infragistics Version 15.2"),
            new KeyValuePair<string, string>("INFRAGISTICS_V_162","Infragistics Version 16.2"),
        };
        //,"Infragistics_V_12","Infragistics_V_122","Infragistics_V_14","Infragistics_V_7"};

        public readonly static List<KeyValuePair<string, string>> APPLICATION_ADDINS_DEVEXP = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("None", "None"),
            new KeyValuePair<string, string>("DEVEXPRESS_V_11", "DevExpress Version 11.X")
        };
        public static KeyValuePair<int,string> GetApplicationTypeById(int iId)
        {
            return APPLICATION_TYPE_LIST.FirstOrDefault(p=>p.Key==iId);
        }

        public static Int16? GetApplicationTypeIdByString(string strAppTypeName)
        {
            var r = APPLICATION_TYPE_LIST.FirstOrDefault(p=>string.Compare(p.Value,strAppTypeName,true)==0);
            try
            {
                if (r.Equals(default(KeyValuePair<int, string>))) return null;
                return (Int16)r.Key;
            }
            catch (Exception)
            {
                return null;
            }
            

        }

        public static List<T_REGISTERED_APPSDTO> GetAppInfoByTestSuiteId(long testSuiteId)
        {
            Logger.logBegin("GetAppInfoByTestSuiteId",string.Format("TSId:[{0}]",testSuiteId));
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance();
                var app = (from ts in dbCntx.REL_APP_TESTSUITE
                          from a in dbCntx.T_REGISTERED_APPS
                          where ts.TEST_SUITE_ID == testSuiteId
                          && ts.APPLICATION_ID==a.APPLICATION_ID
                          select a)
                          .OrderBy(p=>p.APP_SHORT_NAME)
                          .ToDTOs()
                          .ToList();
                return app;
                
            }
            catch (Exception e)
            {
                Logger.Error("GetAppInfoByTestSuiteId",string.Format("Exception:[{0}]", e.Message),e );
                return null;
            }
            finally
            {
                Logger.logEnd("GetAppInfoByTestSuiteId");
            }
        }

        public static B_REGISTERED_APPS GetApplicationByShortName(string strAppShortName, ref bool isOk, ref string strError)
        {
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance();
                var app = (from q in dbCntx.T_REGISTERED_APPS
                          where
                            string.Compare(strAppShortName, q.APP_SHORT_NAME, true) == 0
                          select q).FirstOrDefault();
                if (app!=null)
                {
                    isOk = true;
                    return B_REGISTERED_APPS.CreateFromDTO(app.ToDTO());
                }
                isOk = false;
                Logger.Error("GetApplicationByShortName", strError = string.Format("No such application with short Name:[{0}]", strAppShortName));
                return null;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetApplicationByShortName", strError = string.Format("Exception when gets application data:[{0}]",e.Message),e);
                return null;
            }
        }
        public static B_REGISTERED_APPS GetApplicationByAppId(long lAppId, ref bool isOk, ref string strError)
        {
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance();
                var app = (from q in dbCntx.T_REGISTERED_APPS
                           where
                             q.APPLICATION_ID==lAppId
                           select q).FirstOrDefault();
                if (app != null)
                {
                    isOk = true;
                    return B_REGISTERED_APPS.CreateFromDTO(app.ToDTO());
                }
                isOk = false;
                Logger.Error("GetApplicationByAppId", strError = string.Format("No such application with short Name:[{0}]", lAppId));
                return null;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetApplicationByAppId", strError = string.Format("Exception [app id:{1}] when gets application data:[{0}]", e.Message, lAppId), e);
                return null;
            }
        }

        private long newApplicationId;
        public long NewApplicationId
        {
            get { return newApplicationId; }
            set { newApplicationId = value;  }
        }

        public static List<KeyValuePair<long, T_REGISTERED_APPSDTO>> CreateAppInfoIdMapping(
            List<B_REGISTERED_APPS> lstApps, 
            ref bool isOk, 
            ref string strError, 
            MarsTransactionMgr objTrans)
        {
            Logger.Info("CreateAppInfoIdMapping",string.Format("Try to get mapping apps' count:[{0}]",lstApps==null?-1:lstApps.Count));
            if (objTrans==null)
            {
                Logger.Error("CreateAppInfoIdMapping",strError="Transcation Mgr is null");
                isOk = false;
                return null;
            }
            List<string> lstShortNames = lstApps.Select(p=>p.APP_SHORT_NAME).ToList();
            MarsEntities crntCntx = objTrans.CurrentDBContext;//BoHelper.GetMarsEntitiesInstanceByConn(objTrans.CurrentConnection);
            var apps = from a in crntCntx.T_REGISTERED_APPS
                       where lstShortNames.Contains(a.APP_SHORT_NAME)
                       select a;
            List<T_REGISTERED_APPSDTO> lstRsltFromDb = apps == null ? new List<T_REGISTERED_APPSDTO>() : T_REGISTERED_APPSAssembler.ToDTOs(apps.ToList());
            List<KeyValuePair<long, T_REGISTERED_APPSDTO>> lstRslt = new List<KeyValuePair<long, T_REGISTERED_APPSDTO>>();
            foreach(var itm in lstApps)
            {
                T_REGISTERED_APPSDTO itmFromDB = lstRsltFromDb.Where(p => string.Compare(p.APP_SHORT_NAME, itm.APP_SHORT_NAME, true) == 0).FirstOrDefault();
                if (itmFromDB==null)
                {
                    /// a new application id is required to create
                    /// 
                    long lNewId = CreateNewApplication(itm, ref isOk, ref strError, objTrans);
                    
                    if (!isOk)
                    {
                        Logger.Error("CreateAppInfoIdMapping", strError = string.Format("Error when call CreateNewApplication [{0}]",strError));
                        return null;
                    }
                    itm.newApplicationId = lNewId;
                    lstRslt.Add(new KeyValuePair<long, T_REGISTERED_APPSDTO>(lNewId, itm));
                    continue;
                }
                /// 相同名称的记录存在，返回数据库的id
                /// 
                lstRslt.Add(new KeyValuePair<long, T_REGISTERED_APPSDTO>(itmFromDB.APPLICATION_ID, itm));
            }
            isOk = true;
            return lstRslt;
        }

        private static long CreateNewApplication(T_REGISTERED_APPSDTO obj2BeCreated,ref bool isOk, ref string strError, MarsTransactionMgr objTrans)
        {
            Logger.logBegin("CreateNewApplication");
            if (obj2BeCreated==null)
            {
                isOk = false;
                Logger.Error("CreateNewApplication", strError="Source DTO is null");
                return -1;
            }
            if (objTrans==null)
            {
                isOk = false;
                Logger.Error("CreateNewApplication",strError="Transaction object is null, no DB Connection");
                return -2;
            }
            MarsEntities crntCntx = objTrans.CurrentDBContext;
            B_REGISTERED_APPS objTmp = new B_REGISTERED_APPS();
            long lNewId = objTmp.getApplicationId(crntCntx);

            T_REGISTERED_APPS objAppEntity = obj2BeCreated.ToEntity();
            objAppEntity.APPLICATION_ID = lNewId;
            crntCntx.Set<T_REGISTERED_APPS>();
            crntCntx.T_REGISTERED_APPS.Add(objAppEntity);

            isOk = true;
            return lNewId;
        }

        private static B_REGISTERED_APPS CreateFromDTO(T_REGISTERED_APPSDTO objDto)
        {
            if (objDto == null) return null;
            B_REGISTERED_APPS objResult = new B_REGISTERED_APPS();
            objResult.APPLICATION_ID = objDto.APPLICATION_ID;
            objResult.APPLICATION_TYPE_ID = objDto.APPLICATION_TYPE_ID;
            objResult.APP_SHORT_NAME = objDto.APP_SHORT_NAME;
            objResult.COMMENT = objDto.COMMENT;
            objResult.EXTRAPOPUPMENU = objDto.EXTRAPOPUPMENU;
            objResult.EXTRAREQUIREMENT = objDto.EXTRAREQUIREMENT;
            objResult.PROCESS_IDENTIFIER = objDto.PROCESS_IDENTIFIER;
            objResult.RECORD_CREATE_DATE = objDto.RECORD_CREATE_DATE;
            objResult.RECORD_CREATE_PERSON = objDto.RECORD_CREATE_PERSON;
            objResult.STARTER_COMMAND = objDto.STARTER_COMMAND;
            objResult.STARTER_PATH = objDto.STARTER_PATH;
            objResult.VERSION = objDto.VERSION;
            return objResult;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;

            }
        }
        //public new long APPLICATION_ID { get; set; }


        //public new string APP_SHORT_NAME { get; set; }

        //public new string VERSION { get; set; }

        //public new string EXTRAREQUIREMENT { get; set; }

        //public new string EXTRAPOPUPMENU { get; set; }

        public ObservableCollection<B_TEST_SUITE> TestSuite { get; set; }

        public List<B_REGISTERED_APPS> GetApplication()
        {
            List<B_REGISTERED_APPS> RegisterdApplication = null;
            if (SystemRegistedApplications == null)
            {
                InitCacheApplications();
            }

            RegisterdApplication = SystemRegistedApplications.ToList();
            return RegisterdApplication;
        }

        public static ObservableCollection<B_REGISTERED_APPS> GetCacheApps()
        {
            if (SystemRegistedApplications==null)
            {
                InitCacheApplications();
            }
            return SystemRegistedApplications;
        }

        public static bool RefreshCachedApplications(ref string strError)
        {
            return InitCacheApplications(ref strError);
        }

        internal static bool InitCacheApplications(ref string strError)
        {
            SystemRegistedApplications = new ObservableCollection<B_REGISTERED_APPS>();
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance();
                var applications = (from c in marsEntities.T_REGISTERED_APPS
                                    orderby c.APP_SHORT_NAME
                                    select c);

                foreach (T_REGISTERED_APPS regApps in applications)
                {

                    B_REGISTERED_APPS newRegApps = B_REGISTERED_APPS.CreateFromDTO(T_REGISTERED_APPSAssembler.ToDTO(regApps));
                    SystemRegistedApplications.Add(newRegApps);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InitCacheApplications", strError = string.Format("Can't get Application data from database with Exception:[{0}]", e.Message), e);
                SystemRegistedApplications.Clear();
                return false;
            }
        }

        private static void InitCacheApplications()
        {
            string strError = "";
            InitCacheApplications(ref strError);
        }



        public long getApplicationId(MarsEntities dbCntx=null)
        {
            MarsEntities marsEntities = dbCntx==null?Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(): dbCntx;
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("T_REGISTERED_APPS_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public bool applicationExists(string applicationName, string version)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance();
            var application = (from c in marsEntities.T_REGISTERED_APPS
                               where c.APP_SHORT_NAME.ToUpper() == applicationName && c.VERSION.Equals(version)
                               select c);

            foreach (T_REGISTERED_APPS regApplication in application)
            {
                if (regApplication.APP_SHORT_NAME != null)
                {
                    return true;
                }
            }
            return false;
        }

        private int cloneToForConfigChange(T_REGISTERED_APPS objEntity)
        {
            if (objEntity == null) return 0;
            objEntity.APP_SHORT_NAME = this.APP_SHORT_NAME;
            objEntity.APPLICATION_TYPE_ID = this.APPLICATION_TYPE_ID;
            objEntity.COMMENT = this.COMMENT;
            objEntity.EXTRAPOPUPMENU = this.EXTRAPOPUPMENU;
            objEntity.EXTRAREQUIREMENT = this.EXTRAREQUIREMENT;
            objEntity.PROCESS_IDENTIFIER = this.PROCESS_IDENTIFIER;
            objEntity.STARTER_COMMAND = this.STARTER_COMMAND;
            objEntity.STARTER_PATH = this.STARTER_PATH;
            objEntity.VERSION = this.VERSION;
            return 1;
        }

        public bool Update(ref string strError)
        {
            Logger.Info("Update",string.Format("Parameters:[{0}],shortName:[{1}]",this.APPLICATION_ID,this.APP_SHORT_NAME));
            MarsDataAccessLayer<T_REGISTERED_APPS> objDbLayer = new MarsDataAccessLayer<T_REGISTERED_APPS>();
            objDbLayer.updateCurrentSingle = cloneToForConfigChange;
            int iRslt = objDbLayer.UpdateSingle(p => p.APPLICATION_ID == this.APPLICATION_ID, ref strError);
            return iRslt > 0;

        }
        #region old code
        //public List<B_REGISTERED_APPS> GetProjectApplication(string projectName)
        //{
        //    MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance();
        //    List<B_REGISTERED_APPS> RegisterdApplication = new List<B_REGISTERED_APPS>();
        //    var applications = (from c in marsEntities.T_REGISTERED_APPS
        //                        orderby c.APP_SHORT_NAME
        //                        select c);
        //    long projectId = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_NAME == projectName).PROJECT_ID;
        //    foreach (T_REGISTERED_APPS regApps in applications)
        //    {
        //        B_REGISTERED_APPS newRegApps = new B_REGISTERED_APPS();
        //        newRegApps.APPLICATION_ID = regApps.APPLICATION_ID;
        //        newRegApps.APP_SHORT_NAME = regApps.APP_SHORT_NAME;
        //        newRegApps.VERSION = regApps.VERSION;
        //        newRegApps.EXTRAREQUIREMENT = regApps.EXTRAREQUIREMENT;
        //        newRegApps.EXTRAPOPUPMENU = regApps.EXTRAPOPUPMENU;

        //        newRegApps.IsSelected = (marsEntities.REL_APP_PROJ.FirstOrDefault(x => x.PROJECT_ID == projectId && x.APPLICATION_ID == regApps.APPLICATION_ID) != null ? true : false);
        //        RegisterdApplication.Add(newRegApps);
        //    }
        //    return RegisterdApplication;
        //}
        #endregion //old code

        public List<B_REGISTERED_APPS> GetProjectApplication(long lProjectId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance();
            List<B_REGISTERED_APPS> RegisterdApplication = new List<B_REGISTERED_APPS>();
            var applications = (from c in marsEntities.T_REGISTERED_APPS
                                orderby c.APP_SHORT_NAME
                                select c);
            long projectId = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_ID==lProjectId).PROJECT_ID;
            foreach (T_REGISTERED_APPS regApps in applications)
            {
                B_REGISTERED_APPS newRegApps = new B_REGISTERED_APPS();
                newRegApps.APPLICATION_ID = regApps.APPLICATION_ID;
                newRegApps.APP_SHORT_NAME = regApps.APP_SHORT_NAME;
                newRegApps.VERSION = regApps.VERSION;
                newRegApps.EXTRAREQUIREMENT = regApps.EXTRAREQUIREMENT;
                newRegApps.EXTRAPOPUPMENU = regApps.EXTRAPOPUPMENU;

                newRegApps.IsSelected = (marsEntities.REL_APP_PROJ.FirstOrDefault(x => x.PROJECT_ID == projectId && x.APPLICATION_ID == regApps.APPLICATION_ID) != null ? true : false);
                RegisterdApplication.Add(newRegApps);
            }
            return RegisterdApplication;
        }

        public List<B_REGISTERED_APPS> GetTestSuiteApplication(string testSuiteName)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance();
            List<B_REGISTERED_APPS> RegisterdApplication = new List<B_REGISTERED_APPS>();
            var applications = (from c in marsEntities.T_REGISTERED_APPS
                                orderby c.APP_SHORT_NAME
                                select c);
            long testSuiteId = marsEntities.T_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_NAME == testSuiteName).TEST_SUITE_ID;
            foreach (T_REGISTERED_APPS regApps in applications)
            {
                B_REGISTERED_APPS newRegApps = new B_REGISTERED_APPS();
                newRegApps.APPLICATION_ID = regApps.APPLICATION_ID;
                newRegApps.APP_SHORT_NAME = regApps.APP_SHORT_NAME;
                newRegApps.VERSION = regApps.VERSION;
                newRegApps.IsSelected = (marsEntities.REL_APP_TESTSUITE.FirstOrDefault(x => x.TEST_SUITE_ID == testSuiteId && x.APPLICATION_ID == regApps.APPLICATION_ID) != null ? true : false);
                newRegApps.EXTRAREQUIREMENT = regApps.EXTRAREQUIREMENT;
                newRegApps.EXTRAPOPUPMENU = regApps.EXTRAPOPUPMENU;

                RegisterdApplication.Add(newRegApps);
            }
            return RegisterdApplication;
        }

        public ObservableCollection<B_TEST_SUITE> GetApplicationTestSuite(long applicationId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance();
            ObservableCollection<B_TEST_SUITE> TestSuite = new ObservableCollection<B_TEST_SUITE>();
            var testSuite = (from c in marsEntities.T_TEST_SUITE
                             join d in marsEntities.REL_APP_TESTSUITE on c.TEST_SUITE_ID equals d.TEST_SUITE_ID
                             where d.APPLICATION_ID == applicationId
                             orderby c.TEST_SUITE_NAME
                             select new { c.TEST_SUITE_ID, c.TEST_SUITE_NAME, c.TEST_SUITE_DESCRIPTION, d.APPLICATION_ID });

            foreach (var regTestSuite in testSuite)
            {
                B_TEST_SUITE newTestSuite = new B_TEST_SUITE();
                newTestSuite.TEST_SUITE_ID = regTestSuite.TEST_SUITE_ID;
                newTestSuite.TEST_SUITE_NAME = regTestSuite.TEST_SUITE_NAME;
                newTestSuite.TEST_SUITE_DESCRIPTION = regTestSuite.TEST_SUITE_DESCRIPTION;
                newTestSuite.APP_SHORT_NAME = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).APP_SHORT_NAME;
                newTestSuite.VERSION = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).VERSION;

                TestSuite.Add(newTestSuite);
            }
            return TestSuite;
        }

        private static MLogger Logger = MLogger.GetLogger(typeof(B_REGISTERED_APPS));
        public T_REGISTERED_APPSDTO GetApplicationById(long applicationID)
        {
            Logger.logBegin("GetApplicationById");
            MarsDataAccessLayer<T_REGISTERED_APPS> objApps = new MarsDataAccessLayer<T_REGISTERED_APPS>();
            T_REGISTERED_APPS objResult = objApps.GetSingle(result => result.APPLICATION_ID == applicationID);
            try
            {
                if (objResult == null) return null;
                T_REGISTERED_APPSDTO objDTO = T_REGISTERED_APPSAssembler.ToDTO(objResult);
                return objDTO;
            }
            catch (Exception e)
            {
                Logger.Error("GetApplicationById", string.Format("Error:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplicationById");

            }


        }

        
        public string GetRegApplicationType(long lType,ref bool bOk, ref string strError)
        {
            ///get type infor from database 
            /// 
            Mars_CachedObjects_Base objCache = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_SYSTEM_LOOKUP);
            bOk = false;
            if (!(objCache is Mars_Cached_Lookup))
            {
                Logger.Error("GetRegApplicationType", strError = string.Format("Object type should be [{0}], but it is :[{1}]", typeof(Mars_Cached_Lookup), objCache.GetType()));
                return null;
            }
            Mars_Cached_Lookup objLookup = (Mars_Cached_Lookup)objCache;
            List<B_SYSTEM_LOOKUP> lstRslt = objCache.GetCachedObjctAs<List<B_SYSTEM_LOOKUP>>(null);
            B_SYSTEM_LOOKUP objResult = lstRslt.FirstOrDefault(p => (
                    string.Compare(p.TABLE_NAME, B_SYSTEM_LOOKUP.CNST_TABLENAME_T_REGISTERED_APPS, true) == 0)
                    && (string.Compare(p.FIELD_NAME, B_SYSTEM_LOOKUP.CNST_FIELDNAME_APPLICATION_TYPE, true) == 0
                    && p.VALUE==lType));
            bOk = true;
            if (objResult==null)
            {
                return null;
            }
            Logger.Info("GetRegApplicationType", string.Format("type:[{0}] result :[{1}]", lType, objResult.DISPLAY_NAME));
            return objResult.DISPLAY_NAME;
        }

        public bool RecorrectRequirement(List<string> lstResult,ref string strError)
        {
            if (lstResult==null)
            {
                Logger.Error("RecorrectRequirement",strError="return parameter-lstResult is null");
                return false;
            }

            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance();
            string[] arrRequirectments = this.EXTRAREQUIREMENT == null ? new string[] { "" } : this.EXTRAREQUIREMENT.ToUpper().Split(new string[] { ";" },StringSplitOptions.RemoveEmptyEntries);
            List<string> lstRequirements = new List<string>(arrRequirectments);

            Mars_CachedObjects_Base objCache = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_SYSTEM_LOOKUP);
            bool bOk = false;
            if (!(objCache is Mars_Cached_Lookup))
            {
                Logger.Error("GetRegApplicationType", strError = string.Format("Object type should be [{0}], but it is :[{1}]", typeof(Mars_Cached_Lookup), objCache.GetType()));
                return bOk;
            }
            Mars_Cached_Lookup objLookup = (Mars_Cached_Lookup)objCache;
            List<B_SYSTEM_LOOKUP> lstRslt = objCache.GetCachedObjctAs<List<B_SYSTEM_LOOKUP>>(null);
            IEnumerable<B_SYSTEM_LOOKUP> query = from p in lstRslt
                        where string.Compare(p.TABLE_NAME, B_SYSTEM_LOOKUP.CNST_TABLENAME_T_REGISTERED_APPS, true) == 0
                            && (string.Compare(p.FIELD_NAME, B_SYSTEM_LOOKUP.CNST_FIELDNAME_EXTRA_REQUIRE_ADDINS, true) == 0)
                            && lstRequirements.Contains(p.DISPLAY_NAME.ToUpper())
                        select p;
            foreach (B_SYSTEM_LOOKUP objItm in query)
            {
                if (objItm == null) continue;
                lstResult.Add(objItm.DISPLAY_NAME);
            }
            return true;
        }

        public List<T_REGISTERED_APPSDTO> getApplicationId(long lTestId)
        {
            Logger.Info("getApplicationId",string.Format("Try to Get assigned appIds by Testid [{0}]", lTestId));
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance();
            var apps = from q in marsEntities.REL_APP_TESTCASE
                       where q.TEST_CASE_ID == lTestId
                       from a in marsEntities.T_REGISTERED_APPS
                       where a.APPLICATION_ID == q.APPLICATION_ID
                       select a;
            if (apps == null) return null;
            List<T_REGISTERED_APPS> lstAppEntities = apps.ToList() ;
            return T_REGISTERED_APPSAssembler.ToDTOs(lstAppEntities);
        }
    }


}
