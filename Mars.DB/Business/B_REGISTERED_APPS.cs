using Mars.message.DataLayer;
using Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData;
#if !_noEntities
using Mars.message.DataLayer.Generic;

#endif
using Mars.message.Dto;
using Mars.Model;
using Newtonsoft.Json;
#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Data.Objects;
using System.Linq;
#if _forWebClient
using System.Runtime.CompilerServices;
#endif

#if _marsLog
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    public class DataCntx_Reg_Apps : INotifyPropertyChanged
    {
#if !_marsLog
        private static MLogger Logger = MLogger.GetLogger(typeof(DataCntx_Reg_Apps));
#endif
        internal void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal B_REGISTERED_APPS mobj_HostedRegApp = null;


        public Int64 APPLICATION_ID
        {
            get
            {
                return mobj_HostedRegApp == null ? -1 : mobj_HostedRegApp.APPLICATION_ID;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.APPLICATION_ID = value;
                OnPropertyChanged("APPLICATION_ID");
            }
        }


        public String APP_SHORT_NAME
        {
            get
            {
                return mobj_HostedRegApp == null ? "N/A" : mobj_HostedRegApp.APP_SHORT_NAME;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.APP_SHORT_NAME = value;
                OnPropertyChanged("APP_SHORT_NAME");
            }
        }

        public String PROCESS_IDENTIFIER
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.PROCESS_IDENTIFIER;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.PROCESS_IDENTIFIER = value;
                OnPropertyChanged("PROCESS_IDENTIFIER");
            }
        }

        public String STARTER_PATH
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.STARTER_PATH;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.STARTER_PATH = value;
                OnPropertyChanged("STARTER_PATH");
            }
        }
#if !_noEntities
        public static int AssignProjectWithAppId(string strDBIdx, long projectId, long aPPLICATION_ID, ref bool isOk, ref string strError)
        {
            Logger.logBegin("AssignProjectWithAppId", string.Format("pid-{0} appid-{1}", projectId, aPPLICATION_ID));
            DbTransaction trans = null;
            try
            {
                //获得所有的
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var tc = from prj_ts in dbCntx.REL_TEST_SUIT_PROJECT
                         from ts_tc in dbCntx.REL_TEST_CASE_TEST_SUITE
                         from tc_app in dbCntx.REL_APP_TESTCASE
                         where prj_ts.PROJECT_ID == projectId
                         && ts_tc.TEST_SUITE_ID == prj_ts.TEST_SUITE_ID
                         && ts_tc.TEST_CASE_ID == tc_app.TEST_CASE_ID
                         && tc_app.APPLICATION_ID != aPPLICATION_ID
                         select ts_tc.TEST_CASE_ID;
                Logger.Info("AssignProjectWithAppId", tc.ToString());
                List<long> lstTCId = tc.Where(p => p != null).Distinct().Cast<long>().ToList();
                if (dbCntx.Database.Connection.State != System.Data.ConnectionState.Open)
                {
                    dbCntx.Database.Connection.Open();
                }
                trans = dbCntx.Database.Connection.BeginTransaction();
                int iCnt = 0;
                using (DbCommand dbCmd = dbCntx.Database.Connection.CreateCommand())
                {
                    foreach (var tcid in lstTCId)
                    {
                        string strSql = string.Format("INSERT INTO REL_APP_TESTCASE(RELATIONSHIP_ID,APPLICATION_ID,TEST_CASE_ID) VALUES(REL_APP_TESTCASE_SEQ.NEXTVAL,{0}, {1})",
                            aPPLICATION_ID, tcid);
                        dbCmd.CommandText = strSql;
                        int iInsert = dbCmd.ExecuteNonQuery();
                        iCnt += iInsert;
                    }

                }
                trans.Commit();
                isOk = true;
                return iCnt;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("AssignProjectWithAppId", strError = e.Message, e);
                if (trans != null)
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch (Exception)
                    {

                    }
                }
                return -1;
            }
        }
#endif

        public String STARTER_COMMAND
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.STARTER_COMMAND;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.STARTER_COMMAND = value;
                OnPropertyChanged("STARTER_COMMAND");
            }
        }

        public String VERSION
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.VERSION;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.VERSION = value;
                OnPropertyChanged("VERSION");
            }
        }

        public String COMMENT
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.COMMENT;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.COMMENT = value;
                OnPropertyChanged("COMMENT");
            }
        }

        public Nullable<Int16> APPLICATION_TYPE_ID
        {
            get
            {
                return mobj_HostedRegApp == null ? -1 : mobj_HostedRegApp.APPLICATION_TYPE_ID;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.APPLICATION_TYPE_ID = value;
                OnPropertyChanged("APPLICATION_TYPE_ID");
            }
        }

        public String RECORD_CREATE_PERSON
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.RECORD_CREATE_PERSON;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.RECORD_CREATE_PERSON = value;
                OnPropertyChanged("RECORD_CREATE_PERSON");
            }
        }

        public Nullable<DateTime> RECORD_CREATE_DATE
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.RECORD_CREATE_DATE;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.RECORD_CREATE_DATE = value;
                OnPropertyChanged("RECORD_CREATE_DATE");
            }
        }
        public String EXTRAREQUIREMENT
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.EXTRAREQUIREMENT;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.EXTRAREQUIREMENT = value??"";
                OnPropertyChanged("EXTRAREQUIREMENT");
            }
        }

        public String EXTRAPOPUPMENU
        {
            get
            {
                return mobj_HostedRegApp == null ? null : mobj_HostedRegApp.EXTRAPOPUPMENU;
            }
            set
            {
                if (mobj_HostedRegApp == null)
                    mobj_HostedRegApp = new B_REGISTERED_APPS();
                mobj_HostedRegApp.EXTRAPOPUPMENU = value;
                OnPropertyChanged("EXTRAPOPUPMENU");
            }
        }


    }

    public static class DataCntx_Reg_AppsAssem
    {
        public static DataCntx_Reg_Apps FromBDTO(this B_REGISTERED_APPS src)
        {
            DataCntx_Reg_Apps objRslt = new DataCntx_Reg_Apps();
            objRslt.mobj_HostedRegApp = src;
            return objRslt;
        }

        public static List<DataCntx_Reg_Apps> FromBDTOs(this IEnumerable<B_REGISTERED_APPS> lstSrc)
        {
            if (lstSrc == null) return null;
            return lstSrc.Select(p => p.FromBDTO()).ToList();
        }
    }

    public class B_REGISTERED_APPS : T_REGISTERED_APPSDTO
    {
        private static ObservableCollection<B_REGISTERED_APPS> SystemRegistedApplications = null;

        public const string cnst_app_require_dotNet2 = "REQUIRE_DOTNET_2";
        public const string cnst_app_require_qt = "REQUIRE_QT";

        public const string cnst_app_type_unset = "_unset";
        public const string cnst_app_type_winDotNet = "_dotNet";
        public const string cnst_app_type_QTWind = "_QTWnd";
        public const string cnst_app_type_win = "_normalWnd";
        public const string cnst_app_type_javaWnd = "_javaWindow";

        public MarsErrorCheckData errorCheckData = null;

        public bool convertExtSettingsToErrorObj(ref string strError)
        {
            errorCheckData = null;
            if (string.IsNullOrEmpty(EXTSETTING)) return true;
            try
            {
                errorCheckData = JsonConvert.DeserializeObject<MarsErrorCheckData>(EXTSETTING);
                return true;
            }catch(Exception e)
            {
                strError = e.Message;
                return false;
            }
        }

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
            new KeyValuePair<string, string>("DEVEXPRESS_V_11", "DevExpress Version 11.X"),
            new KeyValuePair<string, string>("DEVEXPRESS_V_18", "DevExpress Version 18.X")
        };
        public static KeyValuePair<int, string> GetApplicationTypeById(int iId)
        {
            return APPLICATION_TYPE_LIST.FirstOrDefault(p => p.Key == iId);
        }

        public static Int16? GetApplicationTypeIdByString(string strAppTypeName)
        {
            var r = APPLICATION_TYPE_LIST.FirstOrDefault(p => string.Compare(p.Value, strAppTypeName, true) == 0);
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

        public bool dealWithAutoErrorCheck(ref string strError)
        {
            Logger.logBegin("dealWithAutoErrorCheck");
            try
            {
                if (string.IsNullOrEmpty(this.EXTSETTING)) return true;
                this.errorCheckData = JsonConvert.DeserializeObject<MarsErrorCheckData>(this.EXTSETTING);
                return true;
            }
            catch (Exception e)
            {
                this.errorCheckData = null;
                Logger.Error("dealWithAutoErrorCheck", strError = e.Message,e);
                return false;
            }
            finally
            {
                Logger.logEnd("dealWithAutoErrorCheck");
            }
        }

#if !_noEntities
        public static List<T_REGISTERED_APPSDTO> GetAppInfoByTestSuiteId(string strDBIdx, long testSuiteId)
        {
            Logger.logBegin("GetAppInfoByTestSuiteId", string.Format("TSId:[{0}]", testSuiteId));
#if (!_forWeb)

            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var app = (from ts in dbCntx.REL_APP_TESTSUITE
                           from a in dbCntx.T_REGISTERED_APPS
                           where ts.TEST_SUITE_ID == testSuiteId
                           && ts.APPLICATION_ID == a.APPLICATION_ID
                           select a)
                          .OrderBy(p => p.APP_SHORT_NAME)
                          .ToDTOs()
                          .ToList();
                return app;

            }
            catch (Exception e)
            {
                Logger.Error("GetAppInfoByTestSuiteId", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetAppInfoByTestSuiteId");
            }

#else
        //get information from restful API

#endif
    }

#if (!_forWeb)
        public T_REGISTERED_APPSDTO GetApplicationByShortName(string strDBIdx, string strShortName, ref string strError, ref bool isOK)
        {
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var applicationId = (from application in marsEntities.T_REGISTERED_APPS
                                     where application.APP_SHORT_NAME.Equals(strShortName)
                                     select application).FirstOrDefault();
                isOK = true;
                return applicationId == null ? null : applicationId.ToDTO();
            }
            catch (Exception e)
            {
                isOK = false;
                strError = string.Format("Exception:[{0}]", e.Message);
                return null;
            }

        }
#endif
        public static B_REGISTERED_APPS GetApplicationByShortName(string strDBIdx, string strAppShortName, ref bool isOk, ref string strError)
        {
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var app = (from q in dbCntx.T_REGISTERED_APPS
                           where
                             string.Compare(strAppShortName, q.APP_SHORT_NAME, true) == 0
                           select q).FirstOrDefault();
                if (app != null)
                {
                    isOk = true;
#if !_forWebSvc
                    return B_REGISTERED_APPS.CreateFromDTO(app.ToDTO());
#else
                    return new B_REGISTERED_APPS().CreateFromDTO(app.ToDTO());
#endif
                }
                isOk = false;
                Logger.Error("GetApplicationByShortName", strError = string.Format("No such application with short Name:[{0}]", strAppShortName));
                return null;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetApplicationByShortName", strError = string.Format("Exception when gets application data:[{0}]", e.Message), e);
                return null;
            }
        }

        internal static B_REGISTERED_APPS GetApplicationByAppIdFromCache(string strDBIdx, 
            long? lApp, ref string strError, ref bool isOk)
        {
            ObservableCollection<B_REGISTERED_APPS> lstApps = GetCacheApps(strDBIdx);
            if (lstApps == null)
            {
                isOk = false;
                Logger.Error("GetApplicationByAppIdFromCache", strError = "cached applications info is null");
                return null;
            }
            var a = lstApps.FirstOrDefault(p => p.APPLICATION_ID == lApp);
            if (a == null)
            {
                isOk = false;
                Logger.Error("GetApplicationByAppIdFromCache", strError = string.Format("No such application [{0}] is found", lApp));
                return null;
            }
            isOk = true;
            return a;
        }
#endif

#if !(_forWebClient || _forWebSvc)
        public static B_REGISTERED_APPS GetApplicationByAppId(long lAppId , ref bool isOk, ref string strError,
            string strCurrentDB = "MarsEntities")
        { 
#else
        public B_REGISTERED_APPS GetApplicationByAppId(string appId, ref bool isOk, ref string strError,
            string strCurrentDB = "MarsEntities")
        {
            Logger.logBegin("GetApplicationByAppId", $"appId:{appId} currentDB:{strCurrentDB}");
            long lAppId = -1;
            if (!long.TryParse(appId, out lAppId))
            {
                isOk = false;
                strError = string.Format("parameter is not a long, [{0}]", appId);
                return null;
            }
#endif
#if _marsLog
            return null;
#else
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strCurrentDB);
                var app = (from q in dbCntx.T_REGISTERED_APPS
                           where
                             q.APPLICATION_ID == lAppId
                           select q).FirstOrDefault();
                if (app != null)
                {
                    isOk = true;
#if !_forWebSvc
                    return B_REGISTERED_APPS.CreateFromDTO(app.ToDTO());
#else
                    return new B_REGISTERED_APPS().CreateFromDTO(app.ToDTO());
#endif
                }
                isOk = false;
                Logger.Error("GetApplicationByAppId", strError = string.Format("No such application with application id:[{0}]", lAppId));
                return null;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetApplicationByAppId", strError = string.Format("Exception [app id:{1}] when gets application data:[{0}]", e.Message, lAppId), e);
                return null;
            }
#endif
        }

#if !_noEntities
        public static B_REGISTERED_APPS GetApplicationByAppId(string strDBIdx, long lAppId, ref bool isOk, ref string strError)
        {
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var app = (from q in dbCntx.T_REGISTERED_APPS
                           where
                             q.APPLICATION_ID == lAppId
                           select q).FirstOrDefault();
                if (app != null)
                {
                    isOk = true;
#if !_forWebSvc
                    return B_REGISTERED_APPS.CreateFromDTO(app.ToDTO());
#else
                    return new B_REGISTERED_APPS().CreateFromDTO(app.ToDTO());
#endif

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
#endif
        private long newApplicationId;
        public long NewApplicationId
        {
            get { return newApplicationId; }
            set { newApplicationId = value; }
        }
#if !_noEntities
        public static List<KeyValuePair<long, T_REGISTERED_APPSDTO>> CreateAppInfoIdMapping(
            string strDBIdx,
            List<B_REGISTERED_APPS> lstApps,
            ref bool isOk,
            ref string strError,
            MarsTransactionMgr objTrans)
        {
            Logger.Info("CreateAppInfoIdMapping", string.Format("Try to get mapping apps' count:[{0}]", lstApps == null ? -1 : lstApps.Count));
            if (objTrans == null)
            {
                Logger.Error("CreateAppInfoIdMapping", strError = "Transcation Mgr is null");
                isOk = false;
                return null;
            }
            List<string> lstShortNames = lstApps.Select(p => p.APP_SHORT_NAME).ToList();
            MarsEntities crntCntx = objTrans.CurrentDBContext;//BoHelper.GetMarsEntitiesInstanceByConn(objTrans.CurrentConnection);
            var apps = from a in crntCntx.T_REGISTERED_APPS
                       where lstShortNames.Contains(a.APP_SHORT_NAME)
                       select a;
            List<T_REGISTERED_APPSDTO> lstRsltFromDb = apps == null ? new List<T_REGISTERED_APPSDTO>() : T_REGISTERED_APPSAssembler.ToDTOs(apps.ToList());
            List<KeyValuePair<long, T_REGISTERED_APPSDTO>> lstRslt = new List<KeyValuePair<long, T_REGISTERED_APPSDTO>>();
            foreach (var itm in lstApps)
            {
                T_REGISTERED_APPSDTO itmFromDB = lstRsltFromDb.Where(p => string.Compare(p.APP_SHORT_NAME, itm.APP_SHORT_NAME, true) == 0).FirstOrDefault();
                if (itmFromDB == null)
                {
                    /// a new application id is required to create
                    /// 
                    long lNewId = CreateNewApplication(strDBIdx, itm, ref isOk, ref strError, objTrans);

                    if (!isOk)
                    {
                        Logger.Error("CreateAppInfoIdMapping", strError = string.Format("Error when call CreateNewApplication [{0}]", strError));
                        return null;
                    }
                    itm.newApplicationId = lNewId;
                    lstRslt.Add(new KeyValuePair<long, T_REGISTERED_APPSDTO>(lNewId, itm));
                    continue;
                }
                /// 相同名称的记录存在，返回数据库的id
                /// 
                itm.newApplicationId = itmFromDB.APPLICATION_ID;
                lstRslt.Add(new KeyValuePair<long, T_REGISTERED_APPSDTO>(itmFromDB.APPLICATION_ID, itm));
            }
            isOk = true;
            return lstRslt;
        }

        private static long CreateNewApplication(string strDBIdx, T_REGISTERED_APPSDTO obj2BeCreated, ref bool isOk, ref string strError, MarsTransactionMgr objTrans)
        {
            Logger.logBegin("CreateNewApplication");
            if (obj2BeCreated == null)
            {
                isOk = false;
                Logger.Error("CreateNewApplication", strError = "Source DTO is null");
                return -1;
            }
            if (objTrans == null)
            {
                isOk = false;
                Logger.Error("CreateNewApplication", strError = "Transaction object is null, no DB Connection");
                return -2;
            }
            MarsEntities crntCntx = objTrans.CurrentDBContext;
            B_REGISTERED_APPS objTmp = new B_REGISTERED_APPS();
            long lNewId = objTmp.getApplicationId(strDBIdx,crntCntx);

            T_REGISTERED_APPS objAppEntity = obj2BeCreated.ToEntity();
            objAppEntity.APPLICATION_ID = lNewId;
            crntCntx.Set<T_REGISTERED_APPS>();
            crntCntx.T_REGISTERED_APPS.Add(objAppEntity);

            isOk = true;
            return lNewId;
        }
#endif

#if _forWebClient
        [MethodImpl(MethodImplOptions.Synchronized)]
#endif
#if !_forWebSvc
        public static B_REGISTERED_APPS CreateFromDTO(T_REGISTERED_APPSDTO objDto)
#else
        public B_REGISTERED_APPS CreateFromDTO(T_REGISTERED_APPSDTO objDto)
#endif
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
#if !_noEntities
        public ObservableCollection<B_TEST_SUITE> TestSuite { get; set; }

        public List<B_REGISTERED_APPS> GetApplication(string strDBIdx)
        {
            List<B_REGISTERED_APPS> RegisterdApplication = null;
            if (SystemRegistedApplications == null)
            {
                InitCacheApplications(strDBIdx);
            }

            RegisterdApplication = SystemRegistedApplications.ToList();
            return RegisterdApplication;
        }

        public static ObservableCollection<B_REGISTERED_APPS> GetCacheApps(string strDBIdx)
        {
            if (SystemRegistedApplications == null)
            {
                InitCacheApplications(strDBIdx);
            }
            return SystemRegistedApplications;
        }

        public static bool RefreshCachedApplications(string strDBIdx, ref string strError)
        {
            return InitCacheApplications(strDBIdx,ref strError);
        }

       
        internal static bool InitCacheApplications(string strDBIdx, 
            ref string strError)
        {
            SystemRegistedApplications = new ObservableCollection<B_REGISTERED_APPS>();
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var applications = (from c in marsEntities.T_REGISTERED_APPS
                                    orderby c.APP_SHORT_NAME
                                    select c);

                foreach (T_REGISTERED_APPS regApps in applications)
                {

#if !_forWebSvc
                    B_REGISTERED_APPS newRegApps = B_REGISTERED_APPS.CreateFromDTO(T_REGISTERED_APPSAssembler.ToDTO(regApps));
#else
                    B_REGISTERED_APPS newRegApps = new B_REGISTERED_APPS().CreateFromDTO(T_REGISTERED_APPSAssembler.ToDTO(regApps));
#endif
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


        private static void InitCacheApplications(string strDBIdx)
        {
            string strError = "";
            InitCacheApplications(strDBIdx,ref strError);
        }


        public long getApplicationId(string strDBIdx, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx == null ? Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : dbCntx;
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("T_REGISTERED_APPS_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public bool applicationExists(string strDBIdx, string applicationName, string version)
        {
            MarsEntities marsEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
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
        
        public bool Update(ref string strError,string strDBIdx //= MarsEntitiesExtends.cnst_default_dbName
            )
        {
            Logger.Info("Update", string.Format("Parameters:[{0}],shortName:[{1}]", this.APPLICATION_ID, this.APP_SHORT_NAME));
            MarsDataAccessLayer<T_REGISTERED_APPS> objDbLayer = new MarsDataAccessLayer<T_REGISTERED_APPS>(strDBIdx);
            objDbLayer.updateCurrentSingle = cloneToForConfigChange;
            int iRslt = objDbLayer.UpdateSingle(p => p.APPLICATION_ID == this.APPLICATION_ID, ref strError);
            return iRslt > 0;

        }
#region old code
        //public List<B_REGISTERED_APPS> GetProjectApplication(string projectName)
        //{
        //    MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
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

        public List<B_REGISTERED_APPS> GetProjectApplication(string strDBIdx, long lProjectId)
        {
            MarsEntities marsEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_REGISTERED_APPS> RegisterdApplication = new List<B_REGISTERED_APPS>();
            var applications = (from c in marsEntities.T_REGISTERED_APPS
                                orderby c.APP_SHORT_NAME
                                select c);
            long projectId = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_ID == lProjectId).PROJECT_ID;
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

        public List<B_REGISTERED_APPS> GetTestSuiteApplication(string strDBIdx, string testSuiteName)
        {
            MarsEntities marsEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
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



        public ObservableCollection<B_TEST_SUITE> GetApplicationTestSuite(string strDBIdx, long applicationId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            ObservableCollection<B_TEST_SUITE> TestSuite = new ObservableCollection<B_TEST_SUITE>();
            var testSuite = (from c in marsEntities.T_TEST_SUITE
                             join d in marsEntities.REL_APP_TESTSUITE on c.TEST_SUITE_ID equals d.TEST_SUITE_ID
                             where d.APPLICATION_ID == applicationId
                             orderby c.TEST_SUITE_NAME
                             select new { c.TEST_SUITE_ID, c.TEST_SUITE_NAME, c.TEST_SUITE_DESCRIPTION, d.APPLICATION_ID });
            string strError = "";
            bool isOk = false;
            foreach (var regTestSuite in testSuite)
            {
                B_TEST_SUITE newTestSuite = new B_TEST_SUITE();
                newTestSuite.TEST_SUITE_ID = regTestSuite.TEST_SUITE_ID;
                newTestSuite.TEST_SUITE_NAME = regTestSuite.TEST_SUITE_NAME;
                newTestSuite.TEST_SUITE_DESCRIPTION = regTestSuite.TEST_SUITE_DESCRIPTION;
                B_REGISTERED_APPS currentApp = B_REGISTERED_APPS.GetApplicationByAppIdFromCache(strDBIdx,
                    regTestSuite.APPLICATION_ID, ref strError, ref isOk);
                if (currentApp != null)
                {
                    newTestSuite.APP_SHORT_NAME = currentApp.APP_SHORT_NAME;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).APP_SHORT_NAME;
                    newTestSuite.VERSION = currentApp.VERSION;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).VERSION;
                }

                TestSuite.Add(newTestSuite);
            }
            return TestSuite;
        }

        private static MLogger Logger = MLogger.GetLogger(typeof(B_REGISTERED_APPS));
        public T_REGISTERED_APPSDTO GetApplicationById(long applicationID, string strDBIdx )//= MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("GetApplicationById");
            MarsDataAccessLayer<T_REGISTERED_APPS> objApps = new MarsDataAccessLayer<T_REGISTERED_APPS>(strDBIdx);
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


        public string GetRegApplicationType(long lType, ref bool bOk, ref string strError)
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
                    && p.VALUE == lType));
            bOk = true;
            if (objResult == null)
            {
                return null;
            }
            Logger.Info("GetRegApplicationType", string.Format("type:[{0}] result :[{1}]", lType, objResult.DISPLAY_NAME));
            return objResult.DISPLAY_NAME;
        }

        public static B_REGISTERED_APPS NewApplication(string strDBIdx, string aPP_SHORT_NAME, 
            int typId, string cOMMENT, string eXTRAREQUIREMENT, string pROCESS_IDENTIFIER, 
            string vERSION, ref string strError, ref bool isOk)
        {
            Logger.logBegin("NewApplication", string.Format("shortName:[{0}]", aPP_SHORT_NAME));
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                B_REGISTERED_APPS newAppObj = new B_REGISTERED_APPS()
                {
                    APP_SHORT_NAME = aPP_SHORT_NAME,
                    APPLICATION_TYPE_ID = (short)typId,
                    EXTRAREQUIREMENT = eXTRAREQUIREMENT,
                    PROCESS_IDENTIFIER = pROCESS_IDENTIFIER,
                    COMMENT = cOMMENT
                };
                newAppObj.APPLICATION_ID = newAppObj.getApplicationId(strDBIdx, dbCntx);
                dbCntx.Set<T_REGISTERED_APPS>();
                dbCntx.T_REGISTERED_APPS.Add(newAppObj.ToEntity());
                dbCntx.SaveChanges();

                isOk = true;
                return newAppObj;
            }
            catch (Exception e)
            {
                Logger.Error("NewApplication", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                isOk = false;
                return null;
            }
        }

        public bool RecorrectRequirement(string strDBIdx, List<string> lstResult, ref string strError)
        {
            if (lstResult == null)
            {
                Logger.Error("RecorrectRequirement", strError = "return parameter-lstResult is null");
                return false;
            }

            MarsEntities marsEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            string[] arrRequirectments = this.EXTRAREQUIREMENT == null ? new string[] { "" } : this.EXTRAREQUIREMENT.ToUpper().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
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

        public List<T_REGISTERED_APPSDTO> getApplicationId(string strDBIdx, long lTestId)
        {
            Logger.Info("getApplicationId", string.Format("Try to Get assigned appIds by Testid [{0}]", lTestId));
            MarsEntities marsEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var apps = from q in marsEntities.REL_APP_TESTCASE
                       where q.TEST_CASE_ID == lTestId
                       from a in marsEntities.T_REGISTERED_APPS
                       where a.APPLICATION_ID == q.APPLICATION_ID
                       select a;
            if (apps == null) return null;
            List<T_REGISTERED_APPS> lstAppEntities = apps.ToList();
            return T_REGISTERED_APPSAssembler.ToDTOs(lstAppEntities);
        }
#if _forWebSvc
        /// <summary>
        /// 获得所有的application
        /// </summary>
        /// <param name="currentDBIdx"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public List<B_REGISTERED_APPS> GetApplications(string strDBIdx, ref string strError, ref bool isOk)
        {
            Logger.logBegin("GetApplications", $"current idx:[{strDBIdx}]");
            try
            {
                isOk = false ;
                MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                var a = from p in marsEntities.T_REGISTERED_APPS
                        select p;
                if (a == null)
                {
                    strError = "No Application exists in table";
                    Logger.Error("GetApplications", strError, Environment.StackTrace);
                    return null;
                }
                List<B_REGISTERED_APPS> lstRslt = new List<B_REGISTERED_APPS>();
                foreach (var ai in a.ToList())
                {
                    if (ai == null) continue;
                    
                    lstRslt.Add(B_REGISTERED_APPS.FromEntiy2BO(ai));
                }
                isOk = true;
                return lstRslt;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetApplications", strError = e.Message, e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplications");
            }
        }

        private static B_REGISTERED_APPS FromEntiy2BO(T_REGISTERED_APPS entity)
        {
            if (entity == null) return null;
            B_REGISTERED_APPS rslt = new B_REGISTERED_APPS();

            return  rslt.CreateFromDTO(entity.ToDTO());
            
        }
#endif

#if !_forWebSvc
        public static T_REGISTERED_APPSDTO GetApplicationByIdxShortName(string strDBIdx, string applicationNameIdx, ref bool isOk, ref string strError, MarsEntities marsEntities)
#else
        public T_REGISTERED_APPSDTO GetApplicationByIdxShortName(string strDBIdx, string applicationNameIdx, ref bool isOk, ref string strError, MarsEntities marsEntities)
#endif
        {
            Logger.logBegin("GetApplicationByIdxShortName", string.Format("[{0}]", applicationNameIdx));
            MarsEntities dbCtx = marsEntities == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : marsEntities;
            try
            {
                var apps = (from q in dbCtx.T_REGISTERED_APPS
                            where (!string.IsNullOrEmpty(q.APP_SHORT_NAME))
                            && (q.APP_SHORT_NAME.ToUpper().IndexOf(applicationNameIdx.ToUpper()) >= 0)
                            select q).FirstOrDefault();
                if (apps == null)
                {
                    isOk = false;
                    strError = string.Format("no such application shortname with [{0}]", applicationNameIdx);
                    return null;
                }
                isOk = true;
                return apps.ToDTO();
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetApplicationByIdxShortName", strError = string.Format("Exception:[{0}]", e.Message));
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplicationByIdxShortName");
            }
        }
#endif
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

        internal string getAppTypeById(short? apptypeId)
        {
            if (apptypeId == null)
            {
                return cnst_app_type_unset;
            }
            if (apptypeId == 1)
            {
                return cnst_app_type_winDotNet;
            }
            return cnst_app_type_unset;
        }
    }


}
