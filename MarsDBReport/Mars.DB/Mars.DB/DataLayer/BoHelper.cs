using Mars.Business;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;
using System.Collections.ObjectModel;
using System.Transactions;
using System.Data.EntityClient;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Data.Metadata.Edm;
using System.Xml;
using System.Data.Mapping;
using Mars.Utility.ToleranceMgr;
using System.Collections.Specialized;
using System.Configuration;

namespace Mars.DataLayer
{

    public class MarsDBCnnectionInfo
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsDBCnnectionInfo));

        public const string EntityModelName = "Model.MarsModel";

        public string userName;
        public string encodedPwd;
        public string decodedPwd; 
        public string hostName;
        public string dbType;
        public string port;
        public string newSchema;
        public string connectionStringFromCfg;
        public EntityConnectionStringBuilder cnnBuilder;
        public EntityConnection entityConnection = null;
        private string errorMessage;

        private MetadataWorkspace workspace = null  ;
        private bool              isThesame = false ;
        private DbProviderFactory DBProviderFactory = null ;
        public EntityConnection GetEntityConnection()
        {
            if (newSchema == null)
            {
                return new EntityConnection(cnnBuilder.ToString());
            }

            if (workspace == null)
            {
                if (isThesame)
                {
                    return new EntityConnection(cnnBuilder.ToString());
                }

                Func<string, Stream> generateStream =
                    extension => Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Concat(EntityModelName, extension));
                Action<IEnumerable<Stream>> disposeCollection = streams =>
                {
                    if (streams == null)
                        return;

                    foreach (var stream in streams.Where(stream => stream != null))
                        stream.Dispose();
                };
                var conceptualReader = generateStream(".csdl");
                var mappingReader = generateStream(".msl");
                var storageReader = generateStream(".ssdl");

                if (conceptualReader == null || mappingReader == null || storageReader == null)
                {
                    disposeCollection(new[] { conceptualReader, mappingReader, storageReader });
                    return null;
                }
                var storageXml = XElement.Load(storageReader);
                XNamespace store = XNamespace.Get("http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator");
                try
                {
                    foreach (var entitySet in storageXml.Descendants())
                    {
                        //var schemaAttribute = entitySet.Attributes("Schema").FirstOrDefault();
                        var schemaAttribute = entitySet.Attributes(store + "Schema").FirstOrDefault();
                        if (schemaAttribute != null)
                            schemaAttribute.SetValue(store + newSchema);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("MarsEntityConnection", string.Format("Exception:[{0}]", e.Message), e);
                }

                string strEntityName = "";
                foreach (var entitySet in storageXml.Descendants())
                {
                    var schemaAttribute = entitySet.Attributes("Schema").FirstOrDefault();
                    if (schemaAttribute != null)
                    {
                        strEntityName += "\r\n" + entitySet.Name;
                        if (string.Compare(schemaAttribute.Value, newSchema, true) == 0)
                        {
                            isThesame = true;
                            break;
                        }
                        //schemaAttribute.SetValue(schemaName);
                        schemaAttribute.Value = newSchema;
                    }
                }
                if (!isThesame)
                {
                    storageXml.CreateReader();

                    workspace = new MetadataWorkspace();

                    var storageCollection = new StoreItemCollection(new[] { storageXml.CreateReader() });
                    var conceptualCollection = new EdmItemCollection(new[] { XmlReader.Create(conceptualReader) });
                    var mappingCollection = new StorageMappingItemCollection(conceptualCollection,
                                                                            storageCollection,
                                                                            new[] { XmlReader.Create(mappingReader) });

                    workspace.RegisterItemCollection(conceptualCollection);
                    workspace.RegisterItemCollection(storageCollection);
                    workspace.RegisterItemCollection(mappingCollection);
                }
                DBProviderFactory = DbProviderFactories.GetFactory(cnnBuilder.Provider?? "System.Data.EntityClient");
                if (DBProviderFactory == null) return null;
                if (isThesame)
                {
                    return new EntityConnection(cnnBuilder.ToString());
                }
            }

            DbConnection dbCnn = DBProviderFactory.CreateConnection();
            dbCnn.ConnectionString = cnnBuilder.ProviderConnectionString;
            dbCnn.StateChange += new StateChangeEventHandler((sender, stateE) => {
                StateChangeEventArgs z = (StateChangeEventArgs)stateE;
                if ((z.CurrentState == ConnectionState.Open) && (z.OriginalState != ConnectionState.Open))
                {
                    DbCommand cmd = dbCnn.CreateCommand();
                    cmd.CommandText = string.Format("ALTER SESSION SET CURRENT_SCHEMA ={0}", this.newSchema);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("StateChangeEventHandler", string.Format("Exception:[{0}]", ex.Message), ex);
                    }
                }
            }
            );
            //if (!isThesame)
            return new EntityConnection(workspace, dbCnn);
        }

        public void createEntityConnectionStringBuilder()
        {
            try
            {
                string strConnString = string.Format(connectionStringFromCfg, decodedPwd,
                    userName,  //"", //不處理serviceId
                    port,
                    hostName
                    );
                cnnBuilder = new EntityConnectionStringBuilder(strConnString);
            }
            catch (Exception e)
            {
                Logger.Error("createEntityConnectionStringBuilder", e.Message, e);
                errorMessage = e.Message;
                cnnBuilder = null;
            }
            
        }
    }

    public partial class MarsEntitiesExtends
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsEntitiesExtends));

        public const string cnst_default_dbName         = "MarsEntities";
        public const string cnst_Oracle_default_schema  = "Oracle_default_schema";
        public const string cnst_schema_endswith        = ".NEWSCHEMA";
        public const string cnst_DB_PASSWORD            = "DB_PASSWORD";
        public const string cnst_DB_USERID              = "DB_USERID";
        public const string cnst_DB_HOST                = "DB_HOST";
        public const string cnst_DB_PORT                = "DB_PORT";

        private static MetadataWorkspace workspace = null;
        private static bool isThesame = false;
        private static string newschemaname = null;

        public static Dictionary<string, MarsDBCnnectionInfo> CachedConnectionStrings = new Dictionary<string, MarsDBCnnectionInfo>();
        public static Dictionary<string, string>              NewSchemaNameList       = new Dictionary<string, string>();
        public static string NewSchemaName
        {
            get
            {
                return newschemaname;
            }
            set
            {
                newschemaname = value;
            }
        }
        private static string                        EntityModelName   = MarsDBCnnectionInfo.EntityModelName;// "Model.MarsModel";
        public  static EntityConnectionStringBuilder connectionBuilder = null;
        public  static Dictionary<string, EntityConnectionStringBuilder> connectionBuilders = new Dictionary<string, EntityConnectionStringBuilder>();

        private static DbProviderFactory DBProviderFactory = null;

        public string ReadOracleStringFromWebConfig()
        {
            return System.Web.Configuration.WebConfigurationManager.AppSettings["Oracle_dbConnection"];            
        }

        public static EntityConnection MarsEntityConnection
        {
            get
            {
                if (NewSchemaName==null)
                {
                    return new EntityConnection(connectionBuilder.ToString());
                }
                
                if (workspace == null)
                {
                    if (isThesame)
                    {
                        return new EntityConnection(connectionBuilder.ToString());
                    }

                    Func<string, Stream> generateStream =
                        extension => Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Concat(EntityModelName, extension));
                    Action<IEnumerable<Stream>> disposeCollection = streams =>
                    {
                        if (streams == null)
                            return;

                        foreach (var stream in streams.Where(stream => stream != null))
                            stream.Dispose();
                    };
                    var conceptualReader = generateStream(".csdl");
                    var mappingReader = generateStream(".msl");
                    var storageReader = generateStream(".ssdl");

                    if (conceptualReader == null || mappingReader == null || storageReader == null)
                    {
                        disposeCollection(new[] { conceptualReader, mappingReader, storageReader });
                        return null;
                    }
                    var storageXml = XElement.Load(storageReader);
                    XNamespace store = XNamespace.Get("http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator");
                    try
                    {
                        foreach (var entitySet in storageXml.Descendants())
                        {
                            //var schemaAttribute = entitySet.Attributes("Schema").FirstOrDefault();
                            var schemaAttribute = entitySet.Attributes(store + "Schema").FirstOrDefault();
                            if (schemaAttribute != null)
                                schemaAttribute.SetValue(store + NewSchemaName);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("MarsEntityConnection",string.Format("Exception:[{0}]",e.Message), e);
                    }
                    
                    string strEntityName = "";
                    foreach (var entitySet in storageXml.Descendants())
                    {
                        var schemaAttribute = entitySet.Attributes("Schema").FirstOrDefault();
                        if (schemaAttribute != null)
                        {
                            strEntityName += "\r\n" + entitySet.Name;
                            if (string.Compare(schemaAttribute.Value, NewSchemaName, true) ==0)
                            {
                                isThesame = true;
                                break;
                            }
                            //schemaAttribute.SetValue(schemaName);
                            schemaAttribute.Value = NewSchemaName;
                        }
                    }
                    if (!isThesame)
                    {
                        storageXml.CreateReader();

                        workspace = new MetadataWorkspace();

                        var storageCollection = new StoreItemCollection(new[] { storageXml.CreateReader() });
                        var conceptualCollection = new EdmItemCollection(new[] { XmlReader.Create(conceptualReader) });
                        var mappingCollection = new StorageMappingItemCollection(conceptualCollection,
                                                                                storageCollection,
                                                                                new[] { XmlReader.Create(mappingReader) });

                        workspace.RegisterItemCollection(conceptualCollection);
                        workspace.RegisterItemCollection(storageCollection);
                        workspace.RegisterItemCollection(mappingCollection);
                    }
                    DBProviderFactory = DbProviderFactories.GetFactory(connectionBuilder.Provider);
                    if (DBProviderFactory == null) return null;      
                    if (isThesame)
                    {
                        return new EntityConnection(connectionBuilder.ToString());
                    }
                }

                DbConnection dbCnn = DBProviderFactory.CreateConnection();
                dbCnn.ConnectionString = connectionBuilder.ProviderConnectionString;
                dbCnn.StateChange += new StateChangeEventHandler((sender, stateE) => {
                    StateChangeEventArgs z = (StateChangeEventArgs)stateE;
                    if ((z.CurrentState==ConnectionState.Open)&&(z.OriginalState!=ConnectionState.Open))
                    {
                        DbCommand cmd =  dbCnn.CreateCommand();
                        cmd.CommandText = string.Format("ALTER SESSION SET CURRENT_SCHEMA ={0}", NewSchemaName);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("StateChangeEventHandler", string.Format("Exception:[{0}]",ex.Message), ex);
                        }
                    }
                }
                );
                //if (!isThesame)
                return new EntityConnection(workspace, dbCnn);                
            }
        }
        
        public static EntityConnection createConnection(string schemaName, EntityConnectionStringBuilder connectionBuilder, string modelName)
        {
            Func<string, Stream> generateStream =
                extension => Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Concat(modelName, extension));
            Action<IEnumerable<Stream>> disposeCollection = streams =>
            {
                if (streams == null)
                    return;

                foreach (var stream in streams.Where(stream => stream != null))
                    stream.Dispose();
            };
            var conceptualReader = generateStream(".csdl");
            var mappingReader = generateStream(".msl");
            var storageReader = generateStream(".ssdl");

            if (conceptualReader == null || mappingReader == null || storageReader == null)
            {
                disposeCollection(new[] { conceptualReader, mappingReader, storageReader });
                return null;
            }

            var storageXml = XElement.Load(storageReader);
            XNamespace store = XNamespace.Get("http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator");

            foreach (var entitySet in storageXml.Descendants())
            {
                //var schemaAttribute = entitySet.Attributes("Schema").FirstOrDefault();
                var schemaAttribute = entitySet.Attributes(store+"Schema").FirstOrDefault();
                if (schemaAttribute != null)
                    schemaAttribute.SetValue(store+schemaName);
            }
            string strEntityName = "";
            foreach (var entitySet in storageXml.Descendants())
            {
                var schemaAttribute = entitySet.Attributes("Schema").FirstOrDefault();
                if (schemaAttribute != null)
                {
                    strEntityName += "\r\n" + entitySet.Name;
                    //schemaAttribute.SetValue(schemaName);
                    schemaAttribute.Value = schemaName;

                }
            }

            storageXml.CreateReader();

            workspace = new MetadataWorkspace();

            var storageCollection = new StoreItemCollection(new[] { storageXml.CreateReader() });
            var conceptualCollection = new EdmItemCollection(new[] { XmlReader.Create(conceptualReader) });
            var mappingCollection = new StorageMappingItemCollection(conceptualCollection,
                                                                    storageCollection,
                                                                    new[] { XmlReader.Create(mappingReader) });

            workspace.RegisterItemCollection(conceptualCollection);
            workspace.RegisterItemCollection(storageCollection);
            workspace.RegisterItemCollection(mappingCollection);

            var connection = DbProviderFactories.GetFactory(connectionBuilder.Provider).CreateConnection();
            if (connection == null)
            {
                disposeCollection(new[] { conceptualReader, mappingReader, storageReader });
                return null;
            }

            connection.ConnectionString = connectionBuilder.ProviderConnectionString;
            return new EntityConnection(workspace, connection);
        }        

        public static void InitDBInfo(NameValueCollection appSettings, ConnectionStringSettingsCollection connectionStrings)
        {
#if !_forWebClient
            /// 获得cnnstrings
            /// 
            if ((connectionStrings == null) || (appSettings == null)) return;
            for(int i=0;i< connectionStrings.Count; i++)
            {
                if (connectionStrings[i] == null) continue;
                if ((connectionStrings[i].ElementInformation == null)
                    ||(connectionStrings[i].ElementInformation.Source == null)
                    )
                    continue;
                MarsDBCnnectionInfo currentDBCnnInfo = null;
                if (CachedConnectionStrings.ContainsKey(connectionStrings[i].Name))
                {
                    currentDBCnnInfo = CachedConnectionStrings[connectionStrings[i].Name];
                    if (currentDBCnnInfo == null)
                    {
                        CachedConnectionStrings[connectionStrings[i].Name] = currentDBCnnInfo= new MarsDBCnnectionInfo();
                    }                    
                }
                else
                {
                    CachedConnectionStrings.Add(connectionStrings[i].Name, currentDBCnnInfo = new MarsDBCnnectionInfo());                    
                }
                currentDBCnnInfo.connectionStringFromCfg = connectionStrings[i].ConnectionString;

                
                if (cnst_default_dbName.Equals(connectionStrings[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    // 获得user name，host等等
                    currentDBCnnInfo.userName   = appSettings[cnst_DB_USERID]??"";
                    currentDBCnnInfo.encodedPwd = appSettings[cnst_DB_PASSWORD]??"";
                    currentDBCnnInfo.hostName   = appSettings[cnst_DB_HOST]??"";
                    currentDBCnnInfo.port       = appSettings[cnst_DB_PORT]??"";
                    // 获得schema
                    currentDBCnnInfo.newSchema  = appSettings[cnst_Oracle_default_schema] ?? "";
                }
                else
                {
                    currentDBCnnInfo.userName   = appSettings[$"{connectionStrings[i].Name}.{cnst_DB_USERID}"] ?? "";
                    currentDBCnnInfo.encodedPwd = appSettings[$"{connectionStrings[i].Name}.{cnst_DB_PASSWORD}"] ?? "";
                    currentDBCnnInfo.hostName   = appSettings[$"{connectionStrings[i].Name}.{cnst_DB_HOST}"] ?? "";
                    currentDBCnnInfo.port       = appSettings[$"{connectionStrings[i].Name}.{cnst_DB_PORT}"] ?? "";
                    // 获得schema
                    currentDBCnnInfo.newSchema = appSettings[$"{connectionStrings[i].Name}{cnst_schema_endswith}"] ?? "";
                }
                try
                {
                    currentDBCnnInfo.decodedPwd = Mars.Securities.MarsEncodePwd.DecodeString(currentDBCnnInfo.encodedPwd);
                }catch(Exception e)
                {
                    currentDBCnnInfo.decodedPwd = currentDBCnnInfo.encodedPwd;
                }
                currentDBCnnInfo.createEntityConnectionStringBuilder();
            }
#endif
        }

    }
    public class BoHelper
    {

        private static MarsEntities gMarsEntites = null;

        private MarsEntities localMarsEntites = null;

        public static string CNST_SEQNAME_STORYBOARD_TESTRESULT = "SEQ_TESTRESULT_ID";

        public static MarsEntities GetMarsEntitiesInstance(EntityConnection objCnn)
        {
            try
            {
                gMarsEntites = new MarsEntities(objCnn);
                return gMarsEntites;
            }
            catch (Exception e)
            {
                Logger.Error("GetMarsEntitiesInstance", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        //private static string _Lock_forEntity_ = "_Lock_forEntity_";
        public static MarsEntities GetMarsEntitiesInstance(bool needReopen = true, 
            string strCurrentDB = MarsEntitiesExtends.cnst_default_dbName)
        {
            //Monitor.Enter(_Lock_forEntity_);
            try
            {
                //for test
                //needReopen = true;
                //end of test

                if (needReopen)
                {
                    //if (gMarsEntites != null)
                    //{
                    //    try
                    //    {
                    //        gMarsEntites.Database.Connection.Close();
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        Logger.Error("GetMarsEntitiesInstance", string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace), e);
                    //    }

                    //}
                    gMarsEntites = null;
                }
                if (gMarsEntites == null)
                {
                    if (string.IsNullOrEmpty(strCurrentDB))
                    {
                        Logger.Error("GetMarsEntitiesInstance", "no db index is passed");
                        return null;
                    }
                    string tmpCnnIds = string.Join(",", MarsEntitiesExtends.CachedConnectionStrings.Keys);
                    Console.WriteLine($"dbs:[{tmpCnnIds}]");
                    if ((!MarsEntitiesExtends.CachedConnectionStrings.ContainsKey(strCurrentDB))
                        ||(MarsEntitiesExtends.CachedConnectionStrings[strCurrentDB]==null)
                        )
                    {
                        Logger.Error("GetMarsEntitiesInstance", $"{strCurrentDB} not find in connectiontrings settings");
                        return null;
                    }
                    gMarsEntites = new MarsEntities(MarsEntitiesExtends.CachedConnectionStrings[strCurrentDB].GetEntityConnection()); 
                    //gMarsEntites = new MarsEntities();
                    //gMarsEntites = new MarsEntities(MarsEntitiesExtends.MarsEntityConnection);

                }
                //if (gMarsEntites.Database.Connection.State!= ConnectionState.Open)
                //{
                //    gMarsEntites.Database.Connection.Open(); 
                //}
            }
            catch (Exception e)
            {
                Logger.Error("GetMarsEntitiesInstance", string.Format("Exception:[{0}] track:[{1}]", e.Message, e.StackTrace), e);
                return null;
            } finally
            {
                //Monitor.Exit(_Lock_forEntity_); 
            }



            //try
            //{
            //    if (gMarsEntites.Database.Connection.State != ConnectionState.Open)
            //        gMarsEntites.Database.Connection.Open();
            //}
            //catch (Exception)
            //{


            //}

            return gMarsEntites;
        }


#region B_TEST_STEPS realted access methods

        // Get A list of B_TEST_STEPS by testCaseId
        public static List<B_TEST_STEPS> GetTestSteps(string strDBIdx, long testCaseId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<B_TEST_STEPS> testStepList = new List<B_TEST_STEPS>();
            //var testSteps = (from c in marsEntities.T_TEST_STEPS
            //                 where c.TEST_CASE_ID == testCaseId
            //                 orderby c.RUN_ORDER
            //                 select c); 
            var testSteps = (from c in marsEntities.T_TEST_STEPS
                             join d in marsEntities.TEST_DATA_SETTING on c.STEPS_ID equals d.STEPS_ID into stp_d
                             from s in stp_d.DefaultIfEmpty()
                             where c.TEST_CASE_ID == testCaseId
                             select
                                new {
                                    test_stp = c,
                                    data_setting = s
                                }
                             );
            Dictionary<T_TEST_STEPS, List<TEST_DATA_SETTING>> groupedRslt = testSteps.GroupBy(p => p.test_stp, p => p.data_setting).ToDictionary(p => p.Key, p => p == null ? null : p.ToList());
            foreach (T_TEST_STEPS tStep in groupedRslt.Keys)
            {
                B_TEST_STEPS newTeststeps = B_TEST_STEPS.ToBO(tStep);
                //new B_TEST_STEPS();
                //newTeststeps.STEPS_ID = tStep.STEPS_ID;
                //newTeststeps.KEY_WORD_ID = tStep.KEY_WORD_ID;
                //newTeststeps.OBJECT_ID = tStep.OBJECT_ID;
                //newTeststeps.COLUMN_ROW_SETTING = tStep.COLUMN_ROW_SETTING;
                //newTeststeps.RUN_ORDER = tStep.RUN_ORDER;
                //newTeststeps.IS_RUNNABLE = tStep.IS_RUNNABLE;
                //newTeststeps.VALUE_SETTING = tStep.VALUE_SETTING;
                //newTeststeps.COMMENT = tStep.COMMENT;
                //newTeststeps.TEST_DATA_SETTING_DATA_SETTING_ID =   LoadTestDataSettingIds(tStep.STEPS_ID);
                newTeststeps.ListDataSetting = groupedRslt[tStep];
                //newTeststeps.TEST_DATA_SETTING_DATA_SETTING_ID = groupedRslt[tStep];
                testStepList.Add(newTeststeps);
            }

            return new List<B_TEST_STEPS>(testStepList.OrderBy(p => p.RUN_ORDER));
        }

        public static bool CreateRelTCDataSummary(long dATA_SUMMARY_ID, long tEST_CASE_ID, DbCommand dbCmmd, ref string strError)
        {

            try
            {
                dbCmmd.Parameters.Clear();
                bool isOk = false;
                long relId = GetBussinessSeq("T_TEST_STEPS_SEQ", dbCmmd, ref strError, ref isOk);
                if (!isOk) return false;
                string strSql = string.Format("INSERT INTO REL_TC_DATA_SUMMARY (ID,DATA_SUMMARY_ID, TEST_CASE_ID) VALUES({0},{1},{2})", relId, dATA_SUMMARY_ID, tEST_CASE_ID);
                dbCmmd.Parameters.Clear();
                dbCmmd.CommandText = strSql;
                int iCnt = dbCmmd.ExecuteNonQuery();
                Logger.Info("CreateRelTCDataSummary", string.Format("total [{0}] recorders is created", iCnt));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateRelTCDataSummary", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        internal static bool CreateTCDataSetRel(long relId, long datasetId, DbCommand dbCmmd, long tEST_CASE_ID, ref string strError)
        {
            string strSql = string.Format("INSERT INTO REL_TC_DATA_SUMMARY(ID, DATA_SUMMARY_ID,TEST_CASE_ID) VALUES({0}, {1},{2})", relId, datasetId, tEST_CASE_ID);
            try
            {
                dbCmmd.Parameters.Clear();
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateTCDataSetRel", strError = string.Format("exception:[{0}] stacktrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        internal static MarsEntities GetMarsEntitiesInstanceByConn(DbConnection currentConnection)
        {
            Logger.logBegin("GetMarsEntitiesInstanceByConn");
            MarsEntities objEntities = new MarsEntities(currentConnection);
            return objEntities;
        }

#if !(_forWebSvc || _forWebClient)
        public static long GetLastestTestMarkIDByStoryBoardId(long l_currentStoryBoardId, string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        public long GetLastestTestMarkIDByStoryBoardId(long l_currentStoryBoardId,string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            Logger.Info("GetLastestTestMarkIDByStoryBoardId", string.Format("l_currentStoryBoardId:[{0}]", l_currentStoryBoardId));
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities == null) return -2;
            var queryForMaxLastMark = from lstMark in marsEntities.T_PROJ_TEST_RESULT
                                      from stb in marsEntities.T_STORYBOARD_SUMMARY
                                      from stb_dtl in marsEntities.T_PROJ_TC_MGR
                                      where (stb.STORYBOARD_ID == l_currentStoryBoardId)
                                         && (stb.STORYBOARD_ID == (long)(stb_dtl.STORYBOARD_ID ?? -1))
                                         && (stb_dtl.STORYBOARD_DETAIL_ID == lstMark.STORYBOARD_DETAIL_ID)
                                      orderby lstMark.LATEST_TEST_MARK_ID descending
                                      select lstMark;
            if (queryForMaxLastMark.FirstOrDefault() != null)
                return queryForMaxLastMark.FirstOrDefault().LATEST_TEST_MARK_ID ?? -1;
            return -1;
        }

        public static long GetApplicationIdByName(string strDBIdx, string applicationName)
        {
#if !_forWebClient
            long applicationId = 0;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            applicationId = (from application in marsEntities.T_REGISTERED_APPS
                             where application.APP_SHORT_NAME.Equals(applicationName)
                             select application.APPLICATION_ID).FirstOrDefault();
            return applicationId;
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            //long seqId = -1;
            string strError = "";
            bool isOk = false;
            long lAppId = clnt.GetApplicationByAppShortName(applicationName,ref isOk, ref strError);
            //isOk = clnt.GetBussinessSeq(ref seqId, ref isOk, ref strError, applicationName);
            if (!isOk) return -1;
            return lAppId;
#endif
        }

        public static long GetStoryboardByName(string storyboardName, long projectId, string strDBIdx)
        {
            long storyboardId = 0;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            storyboardId = (from storyboard in marsEntities.T_STORYBOARD_SUMMARY
                            where storyboard.STORYBOARD_NAME.Equals(storyboardName) && storyboard.ASSIGNED_PROJECT_ID == projectId
                            select storyboard.STORYBOARD_ID).FirstOrDefault();
            return storyboardId;
        }
        public static long GetProjectIdByName(string projectName,string strDBIdx)
        {
            long projectId = 0;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            projectId = (from proj in marsEntities.T_TEST_PROJECT
                         where proj.PROJECT_NAME.Equals(projectName)
                         select proj.PROJECT_ID).FirstOrDefault();
            return projectId;
        }

        public static string[] GetAllProjectNames(string strDBIdx)
        {

            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            string[] projectNames = (from proj in marsEntities.T_TEST_PROJECT
                                     select proj.PROJECT_NAME).ToArray();
            return projectNames;
        }
#if _forWebSvc
        public long GetLastestTestMarkID(string strDBIdx, MarsEntities objEntities = null)
#else
        public static long GetLastestTestMarkID(MarsEntities objEntities = null, string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            try
            {
                return GetIdBySeqName(CNST_SEQNAME_STORYBOARD_TESTRESULT, objEntities,strDBIdx);
            }
            catch (Exception e)
            {
                Logger.Error("GetLastestTestMarkID", string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
        }
#if _forWebSvc
        public int SaveStoryBoardTestResult(B_PROJ_TEST_RESULT storyBoardTestResult, ref string strError,
        string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        public static int SaveStoryBoardTestResult(B_PROJ_TEST_RESULT storyBoardTestResult, ref string strError, 
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            /// get Id by sequence
            /// 
            if (storyBoardTestResult == null) return 1;
            if (storyBoardTestResult.STORYBOARD_DETAIL_ID == null) return 1;
            try
            {
                storyBoardTestResult.HIST_ID = GetIdBySeqName(CNST_SEQNAME_STORYBOARD_TESTRESULT, strDBIdx:strDBIdx);

                MarsEntities marsEntities = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                //T_PROJ_TEST_RESULT objData = marsEntities.T_PROJ_TEST_RESULT.Attach(storyBoardTestResult.ToEntity());                
                marsEntities.T_PROJ_TEST_RESULT.Add(storyBoardTestResult.ToEntity());
                marsEntities.SaveChanges();
                return 1;
            }
            catch (Exception e)
            {
                Logger.Error("SaveStoryBoardTestResult", strError = string.Format("Exception:[{0}]", e.Message), e);
                return 0;
            }
        }

        public static List<T_TEST_STEPSDTO> GetTestStepByName(string strDBIdx, string strTestCaseName, MarsEntities marsdbCntx, ref string strError, ref bool isOk)
        {
            Logger.logBegin("GetTestStepByName", string.Format("Testcase Name:[{0}]", strTestCaseName));
            MarsEntities dbCntx = marsdbCntx == null ? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx) : marsdbCntx;
            try
            {
                var tst = from t in dbCntx.T_TEST_CASE_SUMMARY
                          from stp in dbCntx.T_TEST_STEPS
                          where t.TEST_CASE_NAME == strTestCaseName
                          && t.TEST_CASE_ID == stp.TEST_CASE_ID
                          select stp;
                isOk = true;
                return tst.OrderBy(p => p.RUN_ORDER).ToDTOs();
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetTestStepByName", strError = string.Format("Exception:[{0}]\r\n", e.Message, e.StackTrace), e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetTestStepByName");
            }
        }

        internal const short CNST_RUN_TYPE_EXE = 1; //EXECUTE
        internal const short CNST_RUN_TYPE_RUN = 2; //RUN
        internal const short CNST_RUN_TYPE_SKP = 4; //SKIP
        internal const short CNST_RUN_TYPE_DON = 8; //DONE
        internal const short CNST_RUN_TYPE_FAI = 16; //FAILUE

#if !_forWebSvc
        public static int UpdateStoryBoardTestResult(B_PROJ_TEST_RESULT storyBoardTestResult, ref string strError,string strDBIdx)
#else
        public int UpdateStoryBoardTestResult(B_PROJ_TEST_RESULT storyBoardTestResult, ref string strError,string strDBIdx)
#endif
        {
#if !_forWebClient
            Logger.Info("UpdateStoryBoardTestResult", string.Format("storyBoardTestResult.result INFO:[{0}], Id:[{1}]",
                storyBoardTestResult == null ? "NULL" : storyBoardTestResult.TEST_RESULT_IN_TEXT, 
                storyBoardTestResult == null ? -1 : storyBoardTestResult.HIST_ID));
            if (storyBoardTestResult == null) return 1;
            DbTransaction dbTrans = null;
            try
            {
                MarsEntities marsEntities = GetMarsEntitiesInstance(true,strCurrentDB:strDBIdx);
                DbConnection dbCnn = marsEntities.Database.Connection;
                if (dbCnn.State != ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                dbTrans = dbCnn.BeginTransaction();
                bool isOk = B_PROJ_TEST_RESULT.UpdateTestResultByConnection(dbCnn, storyBoardTestResult, ref strError);
                if (!isOk)
                {
                    Logger.Error("UpdateStoryBoardTestResult",$"Error:[{ strError}]");
                    try
                    {
                        dbTrans.Rollback();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("UpdateStoryBoardTestResult", $"\tException after B_PROJ_TEST_RESULT.UpdateTestResultByConnection:{e.Message}", e);
                    }
                    return 0;
                }
                isOk = B_PROJ_TC_MGR.UpdateStatusByConnection(dbCnn, storyBoardTestResult.STORYBOARD_DETAIL_ID ?? -1, ref strError);
                if (!isOk)
                {
                    try
                    {
                        dbTrans.Rollback();
                    }
                    catch (Exception)
                    {

                    }
                    return 0;
                }
                dbTrans.Commit();
                Logger.logEnd("UpdateStoryBoardTestResult");
                return 1;
            }
            catch (Exception e)
            {
                if (dbTrans != null)
                {
                    try
                    {
                        dbTrans.Rollback();
                    }
                    catch (Exception)
                    {

                    }

                }
                Logger.Error("UpdateStoryBoardTestResult", strError = string.Format("Exception when update storyboard result, [{0}]", e.Message), e);
                return 0;
            }
            finally
            {
                dbTrans = null;
                Logger.logEnd("UpdateStoryBoardTestResult");
            }

#region entityFrameworkversion
            /*
            try
            {
                MarsEntities marsEntities = GetMarsEntitiesInstance(true);
                
                T_PROJ_TEST_RESULT objRslt = marsEntities.T_PROJ_TEST_RESULT.FirstOrDefault(p => p.HIST_ID == storyBoardTestResult.HIST_ID);
                if ((storyBoardTestResult.TEST_RESULT_IN_TEXT ==null)||(string.IsNullOrEmpty(storyBoardTestResult.TEST_RESULT_IN_TEXT)))
                {
                    storyBoardTestResult.TEST_RESULT_IN_TEXT = storyBoardTestResult.TEST_RESULT != 1 ? "FAILED" : "SUCCESS"; 
                }
                storyBoardTestResult.cloneToEntityWithoutKey(objRslt);
                
                /// update T_PROJ_TC_MGR status
                /// 
                T_PROJ_TC_MGR objStoryboardDetail = marsEntities.T_PROJ_TC_MGR
                    .FirstOrDefault(p=>p.STORYBOARD_DETAIL_ID==storyBoardTestResult.STORYBOARD_DETAIL_ID);
                if((objStoryboardDetail==null)||(objStoryboardDetail==default(T_PROJ_TC_MGR)))
                {
                    strError = string.Format("Can't find test case with story board detail id:[{0}], it could be deleted.",storyBoardTestResult.STORYBOARD_DETAIL_ID);
                    return 0;
                }
                short sqOldRunType = objStoryboardDetail.RUN_TYPE ?? CNST_RUN_TYPE_FAI;
                
                if (objStoryboardDetail.RUN_TYPE == CNST_RUN_TYPE_RUN)
                {
                    if (storyBoardTestResult.TEST_RESULT == null ? false : storyBoardTestResult.TEST_RESULT == 1)
                    {
                        ///测试成功，转换run to done
                        objStoryboardDetail.RUN_TYPE = CNST_RUN_TYPE_DON;
                    }
                }
                Logger.Info("UpdateStoryBoardTestResult", "before save");
                marsEntities.SaveChanges();
                Logger.Info("UpdateStoryBoardTestResult", "after save");
                return 1;
                
            }
            catch (Exception e)
            {
                Logger.Error("UpdateStoryBoardTestResult", strError = string.Format("Exception:[{0}]", e.Message), e);
                return 0;
            }
            */
#endregion
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            int iRslt = clnt.boHelper_UpdateStoryBoardTestResult(storyBoardTestResult, ref strError);
            return iRslt;
#endif
        }
#if !_forWebClient
        public int CreateTestReportLog(B_TEST_REPORT objTestRpt, ref string strError, 
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        internal static int CreateTestReportLog(B_TEST_REPORT objTestRpt, ref string strError,
        string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            Logger.Info("CreateTestReportLog", string.Format("objTestRpt:[HIST_ID:{0}]", objTestRpt == null ? "NULL" : objTestRpt.HIST_ID + "-TCId:" + objTestRpt.TEST_CASE_ID));
            try
            {
                MarsEntities objEntities = GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

                objTestRpt.TEST_REPORT_ID = GetIdBySeqName(CNST_SEQNAME_STORYBOARD_TESTRESULT, strDBIdx:strDBIdx);
                objEntities.T_TEST_REPORT.Add(objTestRpt.ToEntity());
                objEntities.SaveChanges();
                return 1;
            }
            catch (Exception e)
            {
                Logger.Error("CreateTestReportLog", string.Format("Exceptions:[{0}]", e.Message), e);
                strError = e.Message;
                return -1;
            }

        }


        internal static long GetIdBySeqName(string strSeqName, MarsEntities objEntities = null, string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            MarsEntities objmarsEntities = null;
            if (objEntities == null)
                objmarsEntities = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            else
                objmarsEntities = objEntities;
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)objmarsEntities.GETNEXT_VAL(strSeqName, outparam);
            return long.Parse(outparam.Value.ToString());
        }

        internal static long GetIdBySeqName(string strSeqName, DbConnection objDBC)
        {
            Logger.logBegin("GetIdBySeqName");
            try
            {
                string strSql = @"select {0}.NEXTVAL NEXT_ID FROM DUAL";
                using (DbCommand dbCmd = objDBC.CreateCommand())
                {
                    dbCmd.CommandText = string.Format(strSql, strSeqName);
                    dbCmd.CommandType = CommandType.Text;
                    DbDataReader rd = dbCmd.ExecuteReader();
                    if (rd.Read())
                    {
                        return (long)((decimal)(rd["NEXT_ID"]));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("GetIdBySeqName", string.Format("Exception:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
            }

            return -1;
        }

        public static bool deleteTestSteps(List<long> stepIdsTobeDeleted, DbCommand dbCmmd, ref string strError)
        {
            try
            {
                string strIdsForSqlIn = string.Join(",", stepIdsTobeDeleted);
                ///删除 data report
                ///
                string strDelDataRpt = @"DELETE T_TEST_REPORT_STEPS WHERE STEPS_ID IN (" + strIdsForSqlIn + ")";
                dbCmmd.Parameters.Clear();
                dbCmmd.CommandText = strDelDataRpt;
                dbCmmd.ExecuteNonQuery();

                ///删除TEST_DATASETTING
                ///
                string strDelDataSetting = @"DELETE TEST_DATA_SETTING WHERE STEPS_ID IN (" + strIdsForSqlIn + ")";
                dbCmmd.CommandText = strDelDataSetting;
                dbCmmd.ExecuteNonQuery();

                ///删除t_test_steps
                ///
                string strDelTestSteps = @"DELETE T_TEST_STEPS WHERE STEPS_ID IN (" + strIdsForSqlIn + ")";
                dbCmmd.CommandText = strDelDataSetting;
                dbCmmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("deleteTestSteps", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public long getTestStepsId(string strDBIdx)
        {
            if (localMarsEntites == null)
                localMarsEntites = GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            return GetIdBySeqName("T_TEST_STEPS_SEQ", localMarsEntites);
        }
        public static long GetTestStepsId(MarsEntities marsDbCntx)
        {
            return GetIdBySeqName("T_TEST_STEPS_SEQ", marsDbCntx);
        }

        public static bool DeleteTestSteps(IEnumerable<long> iStepIds, DbConnection dbCnn, ref string strError)
        {
            Logger.Info("DeleteTestSteps", string.Format("By connection, Step Ids to be deleted:[{0}], connection:[{1}]", iStepIds, dbCnn));
            try
            {
                /// steps:
                /// 1, delete test data report
                /// 2, delete test data TEST_DATA_SETTING
                /// 3, delete test step
                /// 
                bool isOk = DeleteTestDataSettingByIds(iStepIds, dbCnn, ref strError);
                if (!isOk)
                {
                    Logger.Error("DeleteTestSteps", string.Format("DeleteTestDataSettingByIds with Error:[{0}]", strError));
                    return false;
                }

                isOk = DeleteTestDataReportSettingByIds(iStepIds, dbCnn, ref strError);
                if (!isOk)
                {
                    Logger.Error("DeleteTestSteps", string.Format("DeleteTestDataReportSettingByIds with Error:[{0}]", strError));
                    return false;
                }
                return true;


            }
            catch (Exception e)
            {
                Logger.Error("DeleteTestSteps", string.Format("By connection, Exception:[{0}]\r\nStachTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }
        private static bool DeleteTestDataSettingByIds(IEnumerable<long> iStepIds, DbConnection dbCnn, ref string strError)
        {
            Logger.Info("DeleteTestDataReportByIds", string.Format("by Connection, Step Ids to be deleted:[{0}], connection:[{1}]", iStepIds, dbCnn));
            try
            {
                if (iStepIds == null) return true;

                string strStpIds = string.Concat(",", iStepIds.ToArray());
                string strCommand = string.Format("DELETE {0} WHERE STEPS_ID IN ({1})", B_TEST_DATA_SETTING.TABLE_NAME, strStpIds);
                using (DbCommand dbCmmd = dbCnn.CreateCommand())
                {
                    dbCmmd.CommandText = strCommand;
                    int iCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestDataReportByIds", string.Format("Delete [{0}] records, required to Delte:[{1}]", iCnt, iStepIds.ToList().Count));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteTestDataReportByIds", string.Format("By connection, Exception:[{0}]\r\nStachTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }
#if !_forWebSvc && !_forWebClient
        public static bool ImportDataFromXmlObjForTestResult(string strDBIdx, long storyboardId, long? appId, Dictionary<long, List<V_TEST_DATA_REPORT_SUMMARYDTO>> lstRptObjs, long sTestMode, ref string strError)
        {
            Logger.logBegin("ImportDataFromXmlObjForTestResult", string.Format("storyboardId:[{0}] TestMode:[{1}]", storyboardId, sTestMode));
            try
            {
                ///算法：
                ///   1，create t_project_test record
                /// 
                MarsEntities objDBcntx = GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                List<KeyValuePair<long, long?>> LstDtlId_TCId = new List<KeyValuePair<long, long?>>();


                using (TransactionScope scope = new TransactionScope())
                {

                    long latest_test_mark_id = GetLastestTestMarkID(objDBcntx, strDBIdx);

                    var dtltc = from q in objDBcntx.T_PROJ_TC_MGR
                                where lstRptObjs.Keys.Contains(q.STORYBOARD_DETAIL_ID)
                                select new {
                                    dtl_id = q.STORYBOARD_DETAIL_ID,
                                    tc_id = q.TEST_CASE_ID
                                };
                    foreach (var d in dtltc)
                    {
                        if (d.tc_id == null) continue;
                        if (d.tc_id == -1) continue;
                        LstDtlId_TCId.Add(new KeyValuePair<long, long?>(d.dtl_id, d.tc_id));
                    }

                    foreach (long dtlId in lstRptObjs.Keys)
                    {

                        var tstIdId = LstDtlId_TCId.Where(p => p.Key == dtlId).FirstOrDefault();
                        if (tstIdId.Equals(default(KeyValuePair<long, long?>)))
                        {
                            Logger.Error("ImportDataFromXmlObjForTestResult", strError = string.Format("no such testcase id for storyboardId:[{0}]", dtlId));
                            return false;
                        }
                        B_PROJ_TEST_RESULT objProj = new B_PROJ_TEST_RESULT()
                        {
                            HIST_ID = GetIdBySeqName(CNST_SEQNAME_STORYBOARD_TESTRESULT, objDBcntx),
                            STORYBOARD_DETAIL_ID = dtlId,
                            TEST_RESULT = 1,
                            TEST_MODE = (short)sTestMode,
                            TEST_RESULT_IN_TEXT = "SUCCESS",
                            RESULT_DESC = string.Format("IMPORTED {0}", DateTime.Now),
                            CREATE_TIME = DateTime.Now,
                            LATEST_TEST_MARK_ID = latest_test_mark_id,
                            TEST_CASE_ID = tstIdId.Value,
                            TEST_BEGIN_TIME = DateTime.Now,
                            TEST_END_TIME = DateTime.Now
                        };
                        objDBcntx.T_PROJ_TEST_RESULT.Add(objProj.ToEntity());

                        ///添加report表
                        /// 
                        B_TEST_REPORT objRpt = new B_TEST_REPORT()
                        {
                            TEST_REPORT_ID = GetIdBySeqName(CNST_SEQNAME_STORYBOARD_TESTRESULT, objDBcntx),
                            APPLICATION_ID = appId,
                            TEST_CASE_ID = tstIdId.Value,
                            LOOP_ID = 1,
                            BEGIN_TIME = DateTime.Now,
                            END_TIME = DateTime.Now,
                            RUNNING_RESULT = 1,
                            RUNNING_RESULT_INFO = "IMPORT FROM XML FILE",
                            HIST_ID = objProj.HIST_ID,
                            TEST_MODE = (short)sTestMode
                        };
                        objDBcntx.T_TEST_REPORT.Add(objRpt.ToEntity());

                        foreach (var itm in lstRptObjs[dtlId])
                        {
                            if (itm == null) continue;
                            B_TEST_REPORT_STEPS objStpData = new B_TEST_REPORT_STEPS()
                            {
                                TEST_REPORT_STEP_ID = GetIdBySeqName(B_TEST_REPORT_STEPS.CNST_SEQ_IDNAME, objDBcntx),
                                TEST_REPORT_ID = objRpt.TEST_REPORT_ID,
                                STEPS_ID = itm.STEPS_ID,
                                BEGIN_TIME = DateTime.Now,
                                END_TIME = DateTime.Now,
                                RUNNING_RESULT = 1,
                                RETURN_VALUES = itm.RETURN_VALUES,
                                RUNNING_RESULT_INFO = "IMPORT FROM XML FILE",
                                INPUT_VALUE_SETTING = itm.INPUT_VALUE_SETTING,
                                ACTUAL_INPUT_DATA = itm.ACTUAL_INPUT_DATA,                                
                            };
                            objDBcntx.T_TEST_REPORT_STEPS.Add(objStpData.ToEntity());
                        }
                    }


                    objDBcntx.SaveChanges();
                    scope.Complete();
                }
                return true;
            } catch (Exception e)
            {
                Logger.Error("ImportDataFromXmlObjForTestResult", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("ImportDataFromXmlObjForTestResult");
            }
        }
#endif
        public static bool CreateTestCaseDataSetRelation(List<T_TEST_DATA_SUMMARYDTO> lstNewDataSetCreated, long lCaseId, DbCommand dbCmmd, ref string strError)
        {
            Logger.logBegin("CreateTestCaseDataSetRelation", string.Format("Test case Id:[{0}]", lCaseId));
            try
            {
                bool isOk = false;
                string strSql = @"INSERT INTO REL_TC_DATA_SUMMARY(ID, DATA_SUMMARY_ID, TEST_CASE_ID) VALUES(:ID, :DATA_SUMMARY_ID, :TEST_CASE_ID)";
                for (int i = 0; i < lstNewDataSetCreated.Count; i++)
                {
                    dbCmmd.Parameters.Clear();
                    if (lstNewDataSetCreated[i] == null) continue;
                    long lId = GetBussinessSeq("T_TEST_STEPS_SEQ", dbCmmd, ref strError, ref isOk);
                    if (!isOk) return false;
                    DbParameter paraId = new Oracle.ManagedDataAccess.Client.OracleParameter("ID", lId);
                    DbParameter paraDATA_SUMMARY_ID = new Oracle.ManagedDataAccess.Client.OracleParameter("DATA_SUMMARY_ID", lstNewDataSetCreated[i].DATA_SUMMARY_ID);
                    DbParameter paraTEST_CASE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter("TEST_CASE_ID", lCaseId);
                    dbCmmd.CommandText = strSql;
                    dbCmmd.Parameters.Add(paraId);
                    dbCmmd.Parameters.Add(paraDATA_SUMMARY_ID);
                    dbCmmd.Parameters.Add(paraTEST_CASE_ID);

                    dbCmmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateTestCaseDataSetRelation", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        private static bool DeleteTestDataReportSettingByIds(IEnumerable<long> iStepIds, DbConnection dbCnn, ref string strError)
        {
            Logger.Info("DeleteTestDataReportSettingByIds", string.Format("by Connection, Step Ids to be deleted:[{0}], connection:[{1}]", iStepIds, dbCnn));
            try
            {
                if (iStepIds == null) return true;

                string strStpIds = string.Concat(",", iStepIds.ToArray());
                string strCommand = string.Format("DELETE {0} WHERE STEPS_ID IN ({1})", B_TEST_DATA_SETTING.TABLE_NAME, strStpIds);
                using (DbCommand dbCmmd = dbCnn.CreateCommand())
                {
                    dbCmmd.CommandText = strCommand;
                    int iCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestDataReportSettingByIds", string.Format("Delete [{0}] records, required to Delte:[{1}]", iCnt, iStepIds.ToList().Count));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteTestDataReportSettingByIds", string.Format("By connection, Exception:[{0}]\r\nStachTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static void DeleteTestSteps(string strDBIdx, IEnumerable<long> iStepIds, MarsEntities objDbCntx = null)
        {
            Logger.Info("DeleteTestSteps", string.Format("Try to delete stepIds :[{0}] dbCntx:[{1}]", iStepIds.ToList(), objDbCntx));

            /// delete data by transactions
            ///             
            MarsEntities marsEntities = objDbCntx ?? GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

            Logger.Info("DeleteTestSteps", "got marsEntities");
            var lstRptToDel = marsEntities.T_TEST_REPORT_STEPS.Where(p => iStepIds.Contains(p.STEPS_ID ?? -1));
            foreach (var delItmInRpt in lstRptToDel)
            {
                marsEntities.T_TEST_REPORT_STEPS.Remove(delItmInRpt);
            }

#region added on 05/24/2017
            //marsEntities = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            Logger.Info("DeleteTestSteps", "to delete data setting");

            /// Delete test_DAta_Setting
            /// 
            var lstTestDataSetting2BeDelted = marsEntities.TEST_DATA_SETTING.Where(p => iStepIds.Contains(p.STEPS_ID));
            foreach (var item in lstTestDataSetting2BeDelted)
            {
                marsEntities.TEST_DATA_SETTING.Remove(item);
            }
#endregion //added on 05/24/2017

            var lstStepsToDel = marsEntities.T_TEST_STEPS.Where(p => iStepIds.Contains(p.STEPS_ID));
            Logger.Info("DeleteTestSteps", "to delete steps");
            foreach (var delItmInStep in lstStepsToDel)
            {
                marsEntities.T_TEST_STEPS.Remove(delItmInStep);
            }
        }

        public static void DeleteTestStep(string strDBIdx, long stepId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            var testStep = marsEntities.T_TEST_STEPS.FirstOrDefault(x => x.STEPS_ID == stepId);
            if (testStep != null)
            {
                var dataSettins = marsEntities.TEST_DATA_SETTING.Where(x => x.STEPS_ID == testStep.STEPS_ID);

                foreach (var testDataSetting in dataSettins)
                {
                    marsEntities.TEST_DATA_SETTING.Remove(testDataSetting);
                }
                marsEntities.T_TEST_STEPS.Remove(testStep);
            }
        }

        public static void DeleteStoryboard(long storyboardId, MarsEntities dbCntx)
        {
            MarsEntities marsEntities = dbCntx;
            var storyboard = marsEntities.T_PROJ_TC_MGR.FirstOrDefault(x => x.STORYBOARD_DETAIL_ID == storyboardId);
            if (storyboard != null)
            {
                var dataSettings = marsEntities.T_STORYBOARD_DATASET_SETTING.Where(x => x.STORYBOARD_DETAIL_ID == storyboard.STORYBOARD_DETAIL_ID);

                foreach (var storyboardDataSetting in dataSettings)
                {
                    marsEntities.T_STORYBOARD_DATASET_SETTING.Remove(storyboardDataSetting);
                }
                marsEntities.T_PROJ_TC_MGR.Remove(storyboard);
            }
        }
        public static T_TEST_STEPS GetTestStepEntByID(long stepNo, MarsEntities objCntx)
        {

            //MarsEntities marsEntities = objCntx??BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            T_TEST_STEPS tTestStep = objCntx.T_TEST_STEPS.FirstOrDefault(x => x.STEPS_ID == stepNo);
            return tTestStep;
        }

        public static B_TEST_STEPS GetTestStepByID(string strDBIdx, long stepNo, MarsEntities objCntx = null)
        {
            B_TEST_STEPS testStep = null;
            MarsEntities marsEntities = objCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            T_TEST_STEPS tTestStep = marsEntities.T_TEST_STEPS.FirstOrDefault(x => x.STEPS_ID == stepNo);

            if (tTestStep != null)
            {
                //testStep = (B_TEST_STEPS)T_TEST_STEPSAssembler.ToDTO(tTestStep);
                testStep = B_TEST_STEPS.ToBO(tTestStep);
            }

            return testStep;
        }

        public static B_PROJ_TC_MGR GetStoryboardByID(string strDBIdx, long storyboardId, MarsEntities objCntx = null)
        {
            B_PROJ_TC_MGR storyboardRow = null;
            try
            {
                MarsEntities marsEntities = objCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

                T_PROJ_TC_MGR tStoryboardRow = marsEntities.T_PROJ_TC_MGR.FirstOrDefault(x => x.STORYBOARD_DETAIL_ID == storyboardId);

                if (tStoryboardRow != null)
                {
                    //testStep = (B_TEST_STEPS)T_TEST_STEPSAssembler.ToDTO(tTestStep);
                    storyboardRow = B_PROJ_TC_MGR.ToBO(tStoryboardRow);
                }

                return storyboardRow;
            }
            catch (Exception e)
            {
                Logger.Error("GetStoryboardByID", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }

        }

#endregion

#region methods related to TEST_DATA_SETTING

        public static long GetDataSettingId(string strDBIdx, MarsEntities objDbCntx = null)
        {
            MarsEntities marsEntities = objDbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL(B_TEST_DATA_SETTING.ID_SEQ, outparam);
            return long.Parse(outparam.Value.ToString());
        }

        private static List<long> LoadTestDataSettingIds(string strDBIdx, long stepsId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<long> dataSettingIds = new List<long>();
            var testDataSettings = (from c in marsEntities.TEST_DATA_SETTING
                                    where c.STEPS_ID == stepsId
                                    orderby c.STEPS_ID
                                    select c);

            foreach (TEST_DATA_SETTING tStep in testDataSettings)
            {
                dataSettingIds.Add(tStep.DATA_SETTING_ID);
            }
            return dataSettingIds;
        }

        public static List<TEST_DATA_SETTING> LoadTestDataSettings(string strDBIdx, long stepsId, MarsEntities objDbCntx = null)
        {
            MarsEntities marsEntities = objDbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<TEST_DATA_SETTING> tTestDataSettingList = new List<TEST_DATA_SETTING>();
            var testDataSettings = (from c in marsEntities.TEST_DATA_SETTING
                                    where c.STEPS_ID == stepsId
                                    orderby c.STEPS_ID
                                    select c);
            if (testDataSettings != null && testDataSettings.Count() > 0)
            {
                foreach (TEST_DATA_SETTING tStep in testDataSettings)
                {
                    tTestDataSettingList.Add(tStep);
                }
            }
            return tTestDataSettingList;
        }


        public static List<TEST_DATA_SETTING> LoadTestDataSettings(List<long> testStepIds, MarsEntities dbCntx,string strDBIdx = "MarsEntities")
        {
            MarsEntities marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<TEST_DATA_SETTING> tTestDataSettingList = new List<TEST_DATA_SETTING>();

            foreach (long stepsId in testStepIds)
            {
                var testDataSettings = (from c in marsEntities.TEST_DATA_SETTING
                                        where c.STEPS_ID == stepsId
                                        orderby c.STEPS_ID
                                        select c);
                if (testDataSettings != null && testDataSettings.Count() > 0)
                {
                    foreach (TEST_DATA_SETTING tStep in testDataSettings)
                    {
                        tTestDataSettingList.Add(tStep);
                    }
                }
            }

            return tTestDataSettingList;
        }



        public static List<TEST_DATA_SETTING> LoadTestDataSettings(string strDBIdx, long stepsId, long summaryId, MarsEntities objDbCntx)
        {
            MarsEntities marsEntities = objDbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<TEST_DATA_SETTING> tTestDataSettingList = new List<TEST_DATA_SETTING>();
            var testDataSettings = (from c in marsEntities.TEST_DATA_SETTING
                                    where c.STEPS_ID == stepsId && c.DATA_SUMMARY_ID == summaryId
                                    orderby c.STEPS_ID
                                    select c);
            if (testDataSettings != null && testDataSettings.Count() > 0)
            {
                foreach (TEST_DATA_SETTING tStep in testDataSettings)
                {
                    tTestDataSettingList.Add(tStep);
                }
            }
            return tTestDataSettingList;
        }

        public static List<TEST_DATA_SETTING> LoadTestDataSettingsBySummaryId(long summaryId, MarsEntities dbCntx)
        {
            MarsEntities marsEntities = dbCntx;// BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<TEST_DATA_SETTING> tTestDataSettingList = new List<TEST_DATA_SETTING>();
            var testDataSettings = (from c in marsEntities.TEST_DATA_SETTING
                                    where c.DATA_SUMMARY_ID == summaryId
                                    orderby c.STEPS_ID
                                    select c);
            if (testDataSettings != null && testDataSettings.Count() > 0)
            {
                foreach (TEST_DATA_SETTING tStep in testDataSettings)
                {
                    tTestDataSettingList.Add(tStep);
                }
            }
            return tTestDataSettingList;
        }

        //public static List<B_TEST_DATA_SETTING> LoadBOTestDataSettings(long stepsId, MarsEntities objDbCntx)
        //{

        //    List<TEST_DATA_SETTING> tTestDataSettingList = LoadTestDataSettings(strDBIdx, stepsId, objDbCntx);
        //    List<B_TEST_DATA_SETTING> bTestDataSettingList = new List<B_TEST_DATA_SETTING>();

        //    foreach (TEST_DATA_SETTING tTestDataSetting in tTestDataSettingList)
        //    {
        //        //B_TEST_DATA_SETTING bTestDataSetting = (B_TEST_DATA_SETTING)TEST_DATA_SETTINGAssembler.ToDTO(tTestDataSetting);
        //        B_TEST_DATA_SETTING bTestDataSetting = B_TEST_DATA_SETTING.ToBO(tTestDataSetting);
        //        bTestDataSettingList.Add(bTestDataSetting);
        //    }

        //    return bTestDataSettingList;
        //}

        public static List<B_TEST_DATA_SETTING> LoadBOTestDataSettings(string strDBIdx, long stepsId, long summaryId, MarsEntities objDbCntx)
        {

            List<TEST_DATA_SETTING> tTestDataSettingList = LoadTestDataSettings(strDBIdx, stepsId, summaryId, objDbCntx);

            List<B_TEST_DATA_SETTING> bTestDataSettingList = new List<B_TEST_DATA_SETTING>();

            foreach (TEST_DATA_SETTING tTestDataSetting in tTestDataSettingList)
            {
                //B_TEST_DATA_SETTING bTestDataSetting = (B_TEST_DATA_SETTING)TEST_DATA_SETTINGAssembler.ToDTO(tTestDataSetting);
                B_TEST_DATA_SETTING bTestDataSetting = B_TEST_DATA_SETTING.ToBO(tTestDataSetting);
                bTestDataSettingList.Add(bTestDataSetting);
            }

            return bTestDataSettingList;
        }


        public static List<B_TEST_DATA_SETTING> LoadBOTestDataSettings(List<long> testStepIds, MarsEntities dbCntx = null)
        {

            List<TEST_DATA_SETTING> tTestDataSettingList = LoadTestDataSettings(testStepIds, dbCntx);

            List<B_TEST_DATA_SETTING> bTestDataSettingList = new List<B_TEST_DATA_SETTING>();

            foreach (TEST_DATA_SETTING tTestDataSetting in tTestDataSettingList)
            {
                //B_TEST_DATA_SETTING bTestDataSetting = (B_TEST_DATA_SETTING)TEST_DATA_SETTINGAssembler.ToDTO(tTestDataSetting);
                B_TEST_DATA_SETTING bTestDataSetting = B_TEST_DATA_SETTING.ToBO(tTestDataSetting);
                bTestDataSettingList.Add(bTestDataSetting);
            }

            return bTestDataSettingList;
        }

        public static List<B_TEST_DATA_SETTING> LoadBOTestDataSettingsBySummaryId(long summaryId, long lTestCaseId, MarsEntities dbCntx, ref bool isOk, 
            ref string strError)
        {
            try
            {
                var l = (from d in dbCntx.TEST_DATA_SETTING
                         from s in dbCntx.T_TEST_STEPS
                         where s.TEST_CASE_ID == lTestCaseId
                         && d.STEPS_ID == s.STEPS_ID
                         && d.DATA_SUMMARY_ID == summaryId
                         select d).ToList();
                if (l != null)
                {
                    List<B_TEST_DATA_SETTING> rslt = new List<B_TEST_DATA_SETTING>();
                    l.ForEach(p => {
                        if (p != null)
                        {
                            rslt.Add(B_TEST_DATA_SETTING.ToBO(p));
                        }
                    });
                    isOk = true;
                    return rslt;
                }
                else
                {
                    isOk = true;
                    return new List<B_TEST_DATA_SETTING>();
                }

            }
            catch (Exception e)
            {
                Logger.Error("LoadBOTestDataSettingsBySummaryId", strError = string.Format("Exception:[{0}]", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
        }


        public static List<B_TEST_DATA_SETTING> LoadBOTestDataSettingsBySummaryId(long summaryId, MarsEntities dbCntx)
        {

            List<TEST_DATA_SETTING> tTestDataSettingList = LoadTestDataSettingsBySummaryId(summaryId, dbCntx);

            List<B_TEST_DATA_SETTING> bTestDataSettingList = new List<B_TEST_DATA_SETTING>();

            foreach (TEST_DATA_SETTING tTestDataSetting in tTestDataSettingList)
            {
                //B_TEST_DATA_SETTING bTestDataSetting = (B_TEST_DATA_SETTING)TEST_DATA_SETTINGAssembler.ToDTO(tTestDataSetting);
                B_TEST_DATA_SETTING bTestDataSetting = B_TEST_DATA_SETTING.ToBO(tTestDataSetting);
                bTestDataSettingList.Add(bTestDataSetting);
            }

            return bTestDataSettingList;
        }


        public static void DeleteDataSettings(string strDBIdx, long stepID, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            var testDataSetting = (from r in marsEntities.TEST_DATA_SETTING
                                   where r.STEPS_ID == stepID
                                   select r);

            foreach (var r in testDataSetting)
            {
                marsEntities.TEST_DATA_SETTING.Remove(r);
            }
        }

        public static void DeleteDataSettings(string strDBIdx, List<long> stepIDList, MarsEntities dbCntx)
        {
            foreach (var stepId in stepIDList)
                DeleteDataSettings(strDBIdx,stepId, dbCntx);
        }

        public static void DeleteRelTestCaseDataSummary(string strDBIdx, long testCaseId, long dataSetId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            var relTcDataSummary = (from rt in marsEntities.REL_TC_DATA_SUMMARY
                                    where rt.TEST_CASE_ID == testCaseId && rt.DATA_SUMMARY_ID == dataSetId
                                    select rt).FirstOrDefault();
            if (relTcDataSummary != null)
                marsEntities.REL_TC_DATA_SUMMARY.Remove(relTcDataSummary);
        }


#endregion

#region General

        public static int SaveTestStepsAndData(string strDBIdx, List<B_TEST_STEPS> bTestStepsList, List<B_TEST_DATA_SETTING> bTestDataSettingList)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            foreach (B_TEST_STEPS bTestStep in bTestStepsList)
            {
                T_TEST_STEPS tTestStep = T_TEST_STEPSAssembler.ToEntity((T_TEST_STEPSDTO)bTestStep);
                marsEntities.T_TEST_STEPS.Add(tTestStep);
            }

            foreach (B_TEST_DATA_SETTING bTestDataSetting in bTestDataSettingList)
            {
                TEST_DATA_SETTING tTesDataSetting = TEST_DATA_SETTINGAssembler.ToEntity((TEST_DATA_SETTINGDTO)bTestDataSetting);
                marsEntities.TEST_DATA_SETTING.Add(tTesDataSetting);
            }

            return SaveChanges(strDBIdx);
        }

        public static int SetToSaveTestSteps(List<B_TEST_STEPS> bTestStepsList, MarsEntities objDbCntx)
        {
            //MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            objDbCntx.Set<T_TEST_STEPS>();
            foreach (B_TEST_STEPS bTestStep in bTestStepsList)
            {
                T_TEST_STEPS tTestStep = T_TEST_STEPSAssembler.ToEntity((T_TEST_STEPSDTO)bTestStep);
                objDbCntx.T_TEST_STEPS.Add(tTestStep);
            }
            // no saving here for Transaction                      
            //return SaveChanges();

            return 1;
        }

        public static int SaveDataSettings(string strDBIdx, List<B_TEST_DATA_SETTING> bTestDataSettingList, MarsEntities objDbCntx)
        {
            MarsEntities marsEntities = objDbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            TEST_DATA_SETTING tTesDataSetting;
            foreach (B_TEST_DATA_SETTING bTestDataSetting in bTestDataSettingList)
            {
                tTesDataSetting = (from t in marsEntities.TEST_DATA_SETTING
                                   where t.DATA_SETTING_ID == bTestDataSetting.DATA_SETTING_ID
                                   select t).FirstOrDefault();

                if (tTesDataSetting == null)
                {
                    tTesDataSetting = TEST_DATA_SETTINGAssembler.ToEntity((TEST_DATA_SETTINGDTO)bTestDataSetting);
                    marsEntities.TEST_DATA_SETTING.Add(tTesDataSetting);
                }
                else
                {
                    tTesDataSetting.DATA_VALUE = bTestDataSetting.DATA_VALUE;
                    tTesDataSetting.POOL_ID = bTestDataSetting.POOL_ID;
                    tTesDataSetting.DATA_DIRECTION = bTestDataSetting.DATA_DIRECTION;
                }
            }
            try
            {
                if (objDbCntx == null)
                    return marsEntities.SaveChanges();
                return 1;
                //return SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Error("SaveDataSettings", string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }

        }

        public int SaveChangesByInst(string strDBIdx)
        {
            if (localMarsEntites == null)
                localMarsEntites = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            int iCnt = localMarsEntites.SaveChanges();
            return iCnt;
        }

        public static int SaveChanges(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            int count = marsEntities.SaveChanges();
            Console.WriteLine("SaveChanges count = " + count);
            return count;
        }
#endregion


#region Tiger_Framework Service        
        private static MLogger Logger = MLogger.GetLogger(typeof(BoHelper));
        public static List<V_STB_PROJ_APP_FULLVISION> GetStoryBoardFullVision(string strDBIdx, Int64? iStoryBoardId = null)
        {
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (objEntities == null)
            {
                Logger.Warnning("GetStoryBoardFullVision", "No Entities from MarsEntities.GetInstance returns");
                return null;
            }
            //bool isGetAllList = false;
            //if (iStoryBoardId==null)
            //{
            //    Logger.Info("GetStoryBoardFullVision", "Get all List");
            //    isGetAllList = true;
            //}

            var objResult = objEntities.V_STB_PROJ_APP_FULLVISION.Where(objStory => objStory.STORYBOARD_ID == iStoryBoardId).FirstOrDefault();

            List<V_STB_PROJ_APP_FULLVISION> lstResult = new List<V_STB_PROJ_APP_FULLVISION>();
            lstResult.Add((V_STB_PROJ_APP_FULLVISION)objResult);
            return lstResult;
        }

        public static int DeleteHistDataByStoryBoardDetailId(string strDBIdx, long storyboardDetailId, bool? isDelBaseline, bool? isDelNoneBaseline, ref string strError)
        {
            Logger.Info("DeleteHistDataByStoryBoardDetailId", string.Format("StoryBoardDetailId:[{0}], isDelBaseLine?:[{1}],isDelNoneBaseLine?:[{2}]", storyboardDetailId, isDelBaseline, isDelNoneBaseline));
            ///Steps:
            /// 1, get Enties Instance
            /// 2, if (isDelBaseline) then del base line
            /// 3, if (isDelNodeBaseLine) then del None base line
            /// 
            /// 1, get Enties Instance
            /// 
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            System.Data.Common.DbTransaction objTrans = null;
            int iResult = 1;
            strError = "";
            try
            {
                if (objEntities.Database.Connection.State == ConnectionState.Closed)
                    objEntities.Database.Connection.Open();
                objTrans = objEntities.Database.Connection.BeginTransaction();
                /// 2, if (isDelBaseline) then del base line
                /// 
                if ((isDelBaseline != null) && ((bool)isDelBaseline))
                {
                    iResult = DeleteHistDataByStoryBoardDetailId(objEntities.Database.Connection, storyboardDetailId, true, ref strError);
                }

                if ((isDelNoneBaseline != null) && ((bool)isDelNoneBaseline))
                {
                    iResult = DeleteHistDataByStoryBoardDetailId(objEntities.Database.Connection, storyboardDetailId, false, ref strError);
                }

                objTrans.Commit();
                return iResult;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteHistDataByStoryBoardDetailId", strError = string.Format("Exception:[{0}]", e.Message), e);
                try
                {
                    if (objTrans != null)
                        objTrans.Rollback();
                }
                catch (Exception ex)
                {
                    Logger.Error("DeleteHistDataByStoryBoardDetailId", strError += string.Format("\r\nException:[Ex:{0}]", ex.Message), ex);
                }
                return -1;
            }

        }

        private static int DeleteHistDataByStoryBoardDetailId(DbConnection connection, long storyboardDetailId, bool isBaseLine, ref string strError)
        {
            Logger.Info("DeleteHistDataByStoryBoardDetailId", string.Format("Connection,storyBoardId:[{0}], isBaseLine:[{1}]", storyboardDetailId, isBaseLine));
            /// steps:
            /// 1, delete from T_TEST_REPORT_STEPS
            /// 2, delete from T_TEST_REPORT
            /// 3, delete from T_PROJ_TEST_RESULT
            /// 
            /// 1, delete from T_TEST_REPORT_STEPS
            /// 
            int iCnt = -1;
            DbCommand dbCmd = connection.CreateCommand();
            {
                dbCmd.CommandText = string.Format(@"Delete from T_TEST_REPORT_STEPS where TEST_REPORT_ID in (
                select distinct b.TEST_REPORT_ID
                from T_PROJ_TEST_RESULT a
                     , T_TEST_REPORT b
                Where a.STORYBOARD_DETAIL_ID = {0}
                  and a.HIST_ID = B.HIST_ID
                  and a.TEST_MODE={1}
                )", storyboardDetailId, isBaseLine ? 1 : 0);
                iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("DeleteHistDataByStoryBoardDetailId", string.Format("delete from T_TEST_REPORT_STEPS, count:{0}", iCnt));
            }
            /// 2, delete from T_TEST_REPORT
            dbCmd = connection.CreateCommand();
            {
                dbCmd.CommandText = string.Format(@"Delete from T_TEST_REPORT where HIST_ID in (
                select distinct a.HIST_ID
                from T_PROJ_TEST_RESULT a                    
                Where a.STORYBOARD_DETAIL_ID = {0}
                  and a.TEST_MODE={1}
                )", storyboardDetailId, isBaseLine ? 1 : 0);
                iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("DeleteHistDataByStoryBoardDetailId", string.Format("delete from T_TEST_REPORT, count:{0}", iCnt));
            }

            /// 3, delete from T_PROJ_TEST_RESULT
            dbCmd = connection.CreateCommand();
            {
                dbCmd.CommandText = string.Format(@"Delete from T_PROJ_TEST_RESULT a               
                Where a.STORYBOARD_DETAIL_ID = {0}
                  and a.TEST_MODE={1}
                ", storyboardDetailId, isBaseLine ? 1 : 0);
                iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("DeleteHistDataByStoryBoardDetailId", string.Format("delete from T_PROJ_TEST_RESULT, count:{0}", iCnt));
            }
            return 1;
        }

        public static int DirectDeleteRunner(
            string strDBIdx,
            string tableName,
            string idFieldName,
            long id,
            ref string strError)
        {

            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            System.Data.Common.DbTransaction objTrans = null;
            int iResult = 1;
            strError = "";
            try
            {
                if (objEntities.Database.Connection.State == ConnectionState.Closed)
                    objEntities.Database.Connection.Open();
                objTrans = objEntities.Database.Connection.BeginTransaction();

                iResult = DirectDeleteById(objEntities.Database.Connection,
                                           tableName,
                                           idFieldName,
                                           id,
                                           ref strError);

                objTrans.Commit();
                return iResult;
            }
            catch (Exception e)
            {
                Logger.Error("DirectDeletRunner", strError = string.Format("Exception:[{0}]", e.Message), e);
                try
                {
                    if (objTrans != null)
                        objTrans.Rollback();
                }
                catch (Exception ex)
                {
                    Logger.Error("DirectDeletRunner", strError += string.Format("\r\nException:[Ex:{ 0}]", ex.Message), ex);
                }
                return -1;
            }

        }

        private static int DirectDeleteById(DbConnection connection,
                                            string tableName,
                                            string idFieldName,
                                            long id,
                                            ref string strError)
        {
            Logger.Info("DirectDeleteById", string.Format("Connection,tableName:[{0}], idFieldName:[{1}], id:[{2}]",
                                                            tableName,
                                                            idFieldName,
                                                            id));
            int iCnt = -1;
            DbCommand dbCmd = connection.CreateCommand();
            {
                dbCmd.CommandText = string.Format(@"Delete from {0} where {1} = {2}",
                                                    tableName,
                                                    idFieldName,
                                                    id);
                iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("DirectDeleteById", dbCmd.CommandText + " Count:" + iCnt);
            }

            return 1;
        }


#region V_STB_PROJ_APP_FULLVISION 
        public static IList<V_STB_PROJ_APP_FULLVISIONDTO> GetStoryBoardById(string strDBIdx, Int64? iId)
        {
            return B_V_STB_PROJ_APP_FULLVISION.GetAllByIds(iId,strDBIdx);
        }
#endregion //V_STB_PROJ_APP_FULLVISIONDTO

#region V_STORYBOARD_TEST_FULLVISION
        //public static IList<V_STORYBOARD_TEST_FULLVISIONDTO> GetTestCasesByStoryBoardAndRunTypes(string strPrjID, int[] arr_iRunTypeFilter)
#endregion  V_STORYBOARD_TEST_FULLVISION
#region V_STORYBOARD_TEST_FULLVISION
        public static List<V_STORYBOARD_TEST_FULLVISIONDTO> GetTestCasesByStoryBoardAndRunTypes(string strPrjID, int?[] arr_iRunTypeFilter)
        {
            return B_V_STORYBOARD_TEST_FULLVISION.GetTestCasesByStoryBoardAndRunTypes(strPrjID, arr_iRunTypeFilter);
        }
#endregion  //V_STORYBOARD_TEST_FULLVISION

#if !v_useNameId
        public static IList<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseID(Int64 iTestCaseID,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            return B_V_TEST_STEPS_FULLVISIONDTO.GetTestStepsByTestCaseID(iTestCaseID, strDBIdx);
#else
        public static IList<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseID(Int64 iTestCaseID, long lTargetAppId, string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
#endif
#if _forWebSvc
            return (new B_V_TEST_STEPS_FULLVISIONDTO()).GetTestStepsByTestCaseID(iTestCaseID, lTargetAppId);
#else
            return B_V_TEST_STEPS_FULLVISIONDTO.GetTestStepsByTestCaseID(iTestCaseID, lTargetAppId, strDBIdx);
#endif
        }


        public static T_TEST_DATA_SUMMARYDTO GetDataSummaryByStoryBoardIdTestCaseIDRunorder(string strDBIdx,long iStoryBoardId, long iTestCaseId, long iRunOrder)
        {
            return B_T_TEST_DATA_SUMMARYDTO.GetDataSummaryByStoryBoardIdTestCaseIDRunorder(iStoryBoardId, iTestCaseId, iRunOrder, strDBIdx);
        }

        public static List<B_SHARED_OBJECT_POOL> GetSharedObjectPoolInfoByDataSummaryId(
            string strDBIdx, 
            long dATA_SUMMARY_ID, MarsEntities dbCntx = null)
        {
            Logger.Info("GetSharedObjectPoolInfoByDataSummaryId", string.Format("DataSummaryId:[{0}]", dATA_SUMMARY_ID));
            MarsEntities objEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from objPool in objEntities.T_SHARED_OBJECT_POOL
                        where objPool.DATA_SUMMARY_ID == dATA_SUMMARY_ID
                        select objPool;

            List<B_SHARED_OBJECT_POOL> objectList = new List<B_SHARED_OBJECT_POOL>();

            foreach (var pool in query)
            {
                if (pool == null) continue;
                objectList.Add(B_SHARED_OBJECT_POOL.ToBo(pool));
            }

            return objectList;

        }

        public static List<B_SHARED_OBJECT_POOL> GetSharedObjectPoolInfoByDataByPoolIdList(string strDBIdx, List<long> poolIdList, MarsEntities dbCntx = null)
        {
            Logger.Info("GetSharedObjectPoolInfoByDataSummaryId", "");
            MarsEntities objEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

            var query = from pool in objEntities.T_SHARED_OBJECT_POOL
                        where poolIdList.Contains(pool.OBJECT_POOL_ID)
                        select pool;

            List<B_SHARED_OBJECT_POOL> objectList = new List<B_SHARED_OBJECT_POOL>();

            foreach (var pool in query)
            {
                objectList.Add(B_SHARED_OBJECT_POOL.ToBo(pool));
            }

            return objectList;
        }

        public static List<B_SHARED_OBJECT_POOL> GetSharedObjectPool(string strDBIdx, 
            long dataSetId, string objectName, long objectOrder)
        {
            Logger.Info("GetSharedObjectPool", "");
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            var query = from pool in objEntities.T_SHARED_OBJECT_POOL
                        where pool.DATA_SUMMARY_ID == dataSetId &&
                              pool.OBJECT_NAME.Equals(objectName) && pool.OBJECT_ORDER == objectOrder
                        select pool;

            List<B_SHARED_OBJECT_POOL> objectList = new List<B_SHARED_OBJECT_POOL>();

            foreach (var pool in query)
            {
                objectList.Add(B_SHARED_OBJECT_POOL.ToBo(pool));
            }

            return objectList;
        }

        public static IList<KeyValuePair<TEST_DATA_SETTINGDTO, string>> GetAssignedTestDataByTestCaseID(string strDBIdx, long tEST_CASE_ID, long dATA_SUMMARY_ID)
        {
            return B_TEST_DATA_SETTING.GetAssignedTestDataByTestCaseID(tEST_CASE_ID, dATA_SUMMARY_ID,strDBIdx);
        }

#endregion // Tiger_Framework Service


        public static List<B_STORYBOARD_SUMMARY> GetAllStoryboardRows(string strDBIdx, long projectId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<B_STORYBOARD_SUMMARY> storyBoardRowList = new List<B_STORYBOARD_SUMMARY>();

            var storyboardRows = (from c in marsEntities.T_STORYBOARD_SUMMARY
                                  where c.ASSIGNED_PROJECT_ID == projectId
                                  orderby c.STORYBOARD_NAME
                                  select c);

            foreach (T_STORYBOARD_SUMMARY storyEnt in storyboardRows)
            {
                B_STORYBOARD_SUMMARY b_STORYBOARD_SUMMARY = B_STORYBOARD_SUMMARY.ToBo(storyEnt);

                storyBoardRowList.Add(b_STORYBOARD_SUMMARY);
            }

            return storyBoardRowList;
        }
        internal class Tmp_cmpResult {
            internal string returnValue1;
            internal string returnValue2;
            internal long? STORYBOARD_DETAIL_ID;
            internal long? STORYBOARD_ID;
            internal bool isResultRight;
            internal string InputValueSetting1;
            internal string InputValueSetting2;
            internal int steps_run_ord;

            public override string ToString()
            {
                return string.Format("return:[{0}-{1}], Input:[{2}-{3}], isResultRigt:[{4}]", returnValue1, returnValue2, InputValueSetting1, InputValueSetting2,
                    isResultRight);
            }
            internal void CheckValue()
            {
                isResultRight = returnValue1 == null ? (returnValue2 == null ? true : false) : returnValue2 == null ? false : string.Compare(CorrectResultData(returnValue1), CorrectResultData(returnValue2), true) == 0;
                //if (!isResultRight)
                //    Logger.Info("Tmp_cmpResult",string.Format("value1:[{0}],value2:[{1}],storyBoard_detail_id:[{2}], inputValue1:[{3}], InputValue2:[{4}]", returnValue1,returnValue2,this.STORYBOARD_DETAIL_ID,
                //        this.InputValueSetting1,this.InputValueSetting2 ));
            }
            internal void CheckValue(string strFunc)
            {
                if (string.IsNullOrEmpty(strFunc))
                {
                    CheckValue();
                    return;
                }
                //TEST_PROGRAMS__[Fallback Schema Definition]	INQUIRY		FALSE

                MarsBasicToleranceFunc objFunc = MarsBasicToleranceFunc.FromFuncStringWithPreFix(strFunc);
                if (objFunc == null)
                {
                    CheckValue();
                    return;
                }
                isResultRight = false;
                string strError = "";

                objFunc.CompareDataAsString(returnValue1, returnValue2, ref isResultRight, ref strError);
                //BoHelper.Logger.Info("CheckValue",string.Format("function-[{0}] value:[{1}-{2}] result:[{3}]",
                //    strFunc, returnValue1, returnValue2, isResultRight));
            }
        }


        public static string CorrectResultData(string strSrc)
        {
            if (string.IsNullOrEmpty(strSrc)) return strSrc;
            double d;
            strSrc = strSrc.TrimEnd(new char[] { '\r', '\n', '\t', ' ' });
            if (double.TryParse(strSrc, out d))
            {
                if (d == double.MaxValue) return "0";
                if (d == double.MinValue) return "0";
                return strSrc;
            }
            else
            {
                if (strSrc == "1.79769313486232E+308") return "0";
                if (strSrc == "-1.79769313486232E+308") return "0";
            }
            int i;
            if (int.TryParse(strSrc, out i))
            {
                if (i == int.MaxValue) return "0";
                if (i == int.MinValue) return "0";
            }
            else
            {
                if (strSrc == int.MaxValue.ToString()) return "0";
                if (strSrc == int.MinValue.ToString()) return "0";
            }
            if ((string.Compare("1/1/0001", strSrc, true) == 0)
                || (string.Compare("01/01/0001", strSrc, true) == 0)
                || (string.Compare("1-1-0001", strSrc, true) == 0)
                || (string.Compare("01/01/0001", strSrc, true) == 0))
                return "";
            return strSrc;
        }
        internal class MarsThreadDealResult {
            Thread dealingThrd = null;
            internal List<Tmp_cmpResult> lstOTmp = new List<Tmp_cmpResult>();
        }

        public static List<B_STORYBOARD_TEST_FULLVISION> GetStoryboardRows(string strDBIdx, long storyboarId)
        {
            Logger.logBegin("GetStoryboardRows", string.Format("Storyboard id :[{0}]", storyboarId));
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(true,strDBIdx);
            List<B_STORYBOARD_TEST_FULLVISION> storyBoardRowList = new List<B_STORYBOARD_TEST_FULLVISION>();
#region trash
            /*
                        var storyBoardRows = (from c in marsEntities.V_STORYBOARD_TEST_FULLVISION
                                              where c.PROJECT_NAME.Equals(projectName) || projectName == null && c.STORYBOARD_NAME.Equals(storyboardName) || storyboardName  == null                                 orderby c.RUN_ORDER
                                              select c);
            */


            /// Data report for 
            /// Sql:
            /// 
            //SELECT
            //  distinct decode(bs.RETURN_VALUES, NON_B.RETURN_VALUES, 'TRUE', 'FALSE') RSLT,
            //  bs.STORYBOARD_DETAIL_ID
            //FROM
            //  V_TEST_DATARPT_CAPTURE bs
            //left join V_TEST_DATARPT_CAPTURE non_b
            //on bs.STORYBOARD_DETAIL_ID = non_b.STORYBOARD_DETAIL_ID
            //and bs.LATEST_TEST_MARK_ID = non_b.LATEST_TEST_MARK_ID
            //and bs.OBJECT_HAPPY_NAME = NON_B.OBJECT_HAPPY_NAME
            //AND bs.KEY_WORD_NAME = non_b.KEY_WORD_NAME
            //AND bs.INPUT_VALUE_SETTING = non_b.INPUT_VALUE_SETTING
            //AND bs.TEST_MODE = 0
            //AND NON_B.TEST_MODE = 1
            //and bs.LOOP_ID = non_b.LOOP_ID
#endregion
#region entities way
            //var bsTmp = from bs in marsEntities.V_TEST_DATARPT_CAPTURE
            //            where bs.TEST_MODE == 0
            //            && bs.STORYBOARD_ID == storyboarId
            //            select bs;
            //var bsLst = bsTmp.ToList();
            //var nonBsTmp = from non_b in marsEntities.V_TEST_DATARPT_CAPTURE
            //               where non_b.TEST_MODE == 1
            //               && non_b.STORYBOARD_ID == storyboarId
            //               select non_b;
            //var non_bLst = nonBsTmp.ToList();
#endregion

            bool isOk = false;
            string strError = "";
            try
            {
                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstStoryboardInfo = B_V_STORYBOARD_TEST_FULLVISION.GetStoryboardsAllMode(strDBIdx,storyboarId);
                Logger.Info("\t","time log 1");
                Dictionary<int, List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>> dicData = GetStoryboardFullVisionByCnn(strDBIdx,storyboarId, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("GetStoryboardRows", strError);
                    return new List<B_STORYBOARD_TEST_FULLVISION>();
                }

                //data, step_id
                List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>> bsLst    = dicData.ContainsKey(1) ? dicData[1] : new List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>();
                List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>> non_bLst = dicData.ContainsKey(0) ? dicData[0] : new List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>();

                bsLst = bsLst.OrderBy(p => p.Key.INPUT_VALUE_SETTING)
                             .ThenBy(p => p.Key.RUN_ORDER)
                             .ToList();
                non_bLst = non_bLst.OrderBy(p => p.Key.INPUT_VALUE_SETTING)
                             .ThenBy(p => p.Key.RUN_ORDER)
                             .ToList();
                Logger.Info("\t", "time log 2, blsLst to Tlist cost");
#region trash
                //bsLst = bsLst.OrderBy(p => p.Key == null ? -1 : p.Key.STORYBOARD_DETAIL_ID)
                //    .ThenBy(p => p.Key == null ? "" : p.Key.OBJECT_HAPPY_NAME)
                //    .ThenBy(p => p.Key == null ? "" : p.Key.KEY_WORD_NAME)
                //    .ThenBy(p => p.Key == null ? "" : string.IsNullOrEmpty(p.Key.INPUT_VALUE_SETTING) ? p.Key.RETURN_VALUES : p.Key.INPUT_VALUE_SETTING)
                //    .ToList();
                //non_bLst = non_bLst.OrderBy(p => p.Key == null ? -1 : p.Key.STORYBOARD_DETAIL_ID)
                //    .ThenBy(p => p.Key == null ? "" : p.Key.OBJECT_HAPPY_NAME)
                //    .ThenBy(p => p.Key == null ? "" : p.Key.KEY_WORD_NAME)
                //    .ThenBy(p => p.Key == null ? "" : string.IsNullOrEmpty(p.Key.INPUT_VALUE_SETTING) ? p.Key.RETURN_VALUES : p.Key.INPUT_VALUE_SETTING)
                //    .ToList();
                //int ibsCnt = bsLst == null ? -1 : bsLst.Count;
                //int inonbsCnt = non_bLst == null ? -1 : non_bLst.Count;
                //int iLoopCnt = Math.Max(ibsCnt, inonbsCnt);
#endregion

                Dictionary<long, List<T_TEST_STEPSDTO>> dictStepsWithStoryboardDetailId = B_TEST_STEPS.GetStepsByStoryboardId(strDBIdx, storyboarId);
                Logger.Info("\t", "time log,GetStepsByStoryboardId ");
                ///处理同一storyboard detail的重名对象
                ///
                List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>> lstBSNew = new List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>();


                //List<V_TEST_DATARPT_CAPTUREDTO> bsLst = GetStoryboardFullVisionByCnn(0, storyboarId, ref isOk, ref strError);
                //Logger.Info("GetStoryboardRows", string.Format("Get Base:[{0}]", bsLst.Count));
                //List<V_TEST_DATARPT_CAPTUREDTO> non_bLst = GetStoryboardFullVisionByCnn(1, storyboarId, ref isOk, ref strError);

                Logger.Info("GetStoryboardRows", string.Format("Get non-Base:[{0}],base count:[{1}]", non_bLst.Count, bsLst.Count));

                //List<Tmp_cmpResult> lstRslt = new List<Tmp_cmpResult>();
                //bool isToListFinished = false;
                //int iIdxBs = 0, iIdxNonBs = 0;
                //while ((iIdxBs<ibsCnt)&&(iIdxNonBs<inonbsCnt))
                //{
                //    var tmpBsObj = bsLst[iIdxBs];
                //    var tmpNonObj = non_bLst[iIdxNonBs];
                //    Tmp_cmpResult tmpResult = new Tmp_cmpResult();
                //    if (tmpBsObj.Key.STORYBOARD_DETAIL_ID)
                //}
                

                var storyBoardResult = from bs in bsLst                                       
                                       join non_b in non_bLst
                                       on new
                                       {                                           
                                           bs.Key.STORYBOARD_DETAIL_ID,
                                           bs.Key.OBJECT_HAPPY_NAME,
                                           bs.Key.KEY_WORD_NAME,
                                           vSetting = string.IsNullOrEmpty(bs.Key.INPUT_VALUE_SETTING) ? CorrectResultData(bs.Key.RETURN_VALUES) : CorrectResultData(bs.Key.INPUT_VALUE_SETTING),
                                           bs.Key.LOOP_ID
                                       }
                                       equals new
                                       {
                                           non_b.Key.STORYBOARD_DETAIL_ID,
                                           non_b.Key.OBJECT_HAPPY_NAME,
                                           non_b.Key.KEY_WORD_NAME,
                                           vSetting = string.IsNullOrEmpty(non_b.Key.INPUT_VALUE_SETTING) ? CorrectResultData(non_b.Key.RETURN_VALUES) : CorrectResultData(non_b.Key.INPUT_VALUE_SETTING),
                                           non_b.Key.LOOP_ID
                                       } into dtRpt
                                       from rsltDat in dtRpt.DefaultIfEmpty()
                                       select new
                                       {
                                           value1 = bs.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>))? "" : bs.Key.RETURN_VALUES,
                                           value2 = rsltDat.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? "":rsltDat.Key.RETURN_VALUES,
                                           storyBoardDtl_Id = bs.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? -1:bs.Key.STORYBOARD_DETAIL_ID,
                                           STORYBOARD_ID = bs.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? -1:bs.Key.STORYBOARD_ID,
                                           INPUT_VALUE_SETTING1 = bs.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? "":bs.Key.INPUT_VALUE_SETTING,
                                           INPUT_VALUE_SETTING2 = rsltDat.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? "" : rsltDat.Key.INPUT_VALUE_SETTING,

                                           Steps_Id = bs.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ?-1:bs.Value
                                       };

                var storyBoardResult2 = from non_b in non_bLst
                                        join bs in bsLst
                                       on new
                                       {
                                           non_b.Key.STORYBOARD_DETAIL_ID,
                                           non_b.Key.OBJECT_HAPPY_NAME,
                                           non_b.Key.KEY_WORD_NAME,
                                           vSetting = string.IsNullOrEmpty(non_b.Key.INPUT_VALUE_SETTING) ? CorrectResultData(non_b.Key.RETURN_VALUES) : CorrectResultData(non_b.Key.INPUT_VALUE_SETTING),
                                           non_b.Key.LOOP_ID
                                       }
                                       equals new
                                       {
                                           bs.Key.STORYBOARD_DETAIL_ID,
                                           bs.Key.OBJECT_HAPPY_NAME,
                                           bs.Key.KEY_WORD_NAME,
                                           vSetting = string.IsNullOrEmpty(bs.Key.INPUT_VALUE_SETTING) ? CorrectResultData(bs.Key.RETURN_VALUES) : CorrectResultData(bs.Key.INPUT_VALUE_SETTING),
                                           bs.Key.LOOP_ID
                                       } into dtRpt
                                       from rsltDat in dtRpt.DefaultIfEmpty()
                                       select new
                                       {
                                           value1 = non_b.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? "" : non_b.Key.RETURN_VALUES,
                                           value2 = rsltDat.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? "" : rsltDat.Key.RETURN_VALUES,
                                           storyBoardDtl_Id = non_b.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? -1 : non_b.Key.STORYBOARD_DETAIL_ID,
                                           STORYBOARD_ID = non_b.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? -1 : non_b.Key.STORYBOARD_ID,
                                           INPUT_VALUE_SETTING1 = non_b.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? "" : non_b.Key.INPUT_VALUE_SETTING,
                                           INPUT_VALUE_SETTING2 = rsltDat.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? "" : rsltDat.Key.INPUT_VALUE_SETTING,

                                           Steps_Id = non_b.Equals(default(KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>)) ? -1 : non_b.Value
                                       };


#region db sql mode

                //var storyBoardResult = from bs in marsEntities.V_TEST_DATARPT_CAPTURE
                //                       where bs.TEST_MODE==0
                //                       && bs.STORYBOARD_ID == storyboarId
                //                       join non_b in marsEntities.V_TEST_DATARPT_CAPTURE                                 
                //                       on new { bs.STORYBOARD_DETAIL_ID, bs.OBJECT_HAPPY_NAME, bs.KEY_WORD_NAME,
                //                           vSetting = string.IsNullOrEmpty(bs.INPUT_VALUE_SETTING)?bs.RETURN_VALUES: bs.INPUT_VALUE_SETTING, bs.LOOP_ID }
                //                       equals new { non_b.STORYBOARD_DETAIL_ID, non_b.OBJECT_HAPPY_NAME, non_b.KEY_WORD_NAME,
                //                           vSetting= string.IsNullOrEmpty(non_b.INPUT_VALUE_SETTING) ? non_b.RETURN_VALUES : non_b.INPUT_VALUE_SETTING, non_b.LOOP_ID } into dtRpt
                //                       from rsltDat in dtRpt.DefaultIfEmpty()
                //                       where
                //                         bs.TEST_MODE == 0
                //                         && rsltDat.TEST_MODE == 1
                //                         && bs.STORYBOARD_ID == storyboarId
                //                         && rsltDat.STORYBOARD_ID == storyboarId

                //                       select new {
                //                           value1=bs.RETURN_VALUES,
                //                           value2=rsltDat.RETURN_VALUES,
                //                           storyBoardDtl_Id= bs.STORYBOARD_DETAIL_ID,
                //                           STORYBOARD_ID = bs.STORYBOARD_ID,
                //                           INPUT_VALUE_SETTING1=bs.INPUT_VALUE_SETTING,
                //                           INPUT_VALUE_SETTING2= rsltDat.INPUT_VALUE_SETTING
                //                       };
                //Logger.Info("GetStoryboardRows", string.Format("sql:\r\n{0}", storyBoardResult.ToString()));

#endregion
                List<Tmp_cmpResult> lstcmpRslt = new List<Tmp_cmpResult>();
                List<Tmp_cmpResult> lstcmpRslt2 = new List<Tmp_cmpResult>();
                if (storyBoardResult == null)
                {
                    Logger.Info("GetStoryboardRows", string.Format("No Left join result, base count:[{0}] NoneBase count:[{1}]", bsLst.Count, non_bLst.Count));
                    return storyBoardRowList;
                }
                Logger.Info("\t", "log time storyBoardResult.ToList");

                //var groupedStorbyard = storyBoardResult.GroupBy(x => x.storyBoardDtl_Id).ToDictionary(g=>g.Key, l=>l.ToList());
                //Logger.Info("\t", groupedStorbyard.ToString());

                //foreach(var itm in groupedStorbyard.Keys)
                //{
                //    if (itm == null) continue;
                //    var lstItm = groupedStorbyard[itm];
                //    if (lstItm == null) continue;
                //    foreach(var oItm in lstItm)
                //    {
                //        if (oItm == null) continue;
                //        Tmp_cmpResult oTmp = new Tmp_cmpResult();
                //        oTmp.returnValue1 = CorrectResultData(oItm.value1);
                //        oTmp.returnValue2 = CorrectResultData(oItm.value2);
                //        oTmp.STORYBOARD_DETAIL_ID = oItm.storyBoardDtl_Id;
                //        oTmp.STORYBOARD_ID = oItm.STORYBOARD_ID;

                //        oTmp.InputValueSetting1 = oItm.INPUT_VALUE_SETTING1;
                //        oTmp.InputValueSetting2 = oItm.INPUT_VALUE_SETTING2;
                //    }
                //}
                Dictionary<long, List<Tmp_cmpResult>> dicTmpRslt1 = new Dictionary<long, List<Tmp_cmpResult>>();
                Dictionary<long, List<Tmp_cmpResult>> dicTmpRslt2 = new Dictionary<long, List<Tmp_cmpResult>>();

                foreach (var oItm in storyBoardResult.ToList())
                {
                    if (oItm.storyBoardDtl_Id == null) continue;

                    Tmp_cmpResult oTmp = new Tmp_cmpResult();
                    oTmp.returnValue1 = CorrectResultData(oItm.value1);
                    oTmp.returnValue2 = CorrectResultData(oItm.value2);
                    oTmp.STORYBOARD_DETAIL_ID = oItm.storyBoardDtl_Id;
                    oTmp.STORYBOARD_ID = oItm.STORYBOARD_ID;                    

                    oTmp.InputValueSetting1 = oItm.INPUT_VALUE_SETTING1;
                    oTmp.InputValueSetting2 = oItm.INPUT_VALUE_SETTING2;

                    if ((dictStepsWithStoryboardDetailId!=null)&&(oTmp.STORYBOARD_DETAIL_ID!=null))
                    {
                        if (dictStepsWithStoryboardDetailId.ContainsKey((long)oTmp.STORYBOARD_DETAIL_ID))
                        {
                            List<T_TEST_STEPSDTO> lst = dictStepsWithStoryboardDetailId[(long)oTmp.STORYBOARD_DETAIL_ID];
                            if (lst != null)
                            {
                                var objStp = lst.Where(p => p.STEPS_ID == oItm.Steps_Id).FirstOrDefault();
                                if (objStp == null) continue;
                                //string strFunc = lst.Where(p => p.STEPS_ID == oItm.Steps_Id).Select(p => p.COMMENT).FirstOrDefault();
                                string strFunc = objStp.COMMENT;  //lst.Where(p => p.STEPS_ID == oItm.Steps_Id).Select(p => p.COMMENT).FirstOrDefault();
                                oTmp.CheckValue(strFunc);
                                oTmp.steps_run_ord = (int)(objStp.RUN_ORDER??-1);
                                
                            } else
                                oTmp.CheckValue();
                        }
                        else
                            oTmp.CheckValue();
                    }else
                        oTmp.CheckValue();
                    List<Tmp_cmpResult> lstFromDicCmp = null;
                    long lIdxOfDtl = oItm.storyBoardDtl_Id ?? -1;
                    if (!dicTmpRslt1.ContainsKey(lIdxOfDtl))
                    {
                        dicTmpRslt1.Add(lIdxOfDtl, lstFromDicCmp = new List<Tmp_cmpResult>());
                    }
                    lstFromDicCmp = dicTmpRslt1[lIdxOfDtl];

                    //var oExist = lstcmpRslt.Where(p => (p.STORYBOARD_DETAIL_ID == oItm.storyBoardDtl_Id))
                    //        .Where(p => (string.Compare(p.InputValueSetting1, oTmp.InputValueSetting1) == 0))
                    //        .Where(p => string.Compare(p.InputValueSetting2, oTmp.InputValueSetting2) == 0)
                    //        .FirstOrDefault();
                    var oExist = lstFromDicCmp.Where(p => (string.Compare(p.InputValueSetting1, oTmp.InputValueSetting1) == 0))
                            .Where(p => string.Compare(p.InputValueSetting2, oTmp.InputValueSetting2) == 0)
                            .FirstOrDefault();

                    //var oExist = lstcmpRslt.Where(p => (string.Compare(p.InputValueSetting1, oTmp.InputValueSetting1)==0)
                    //    &&(string.Compare(p.InputValueSetting2, oTmp.InputValueSetting2)==0)
                    //    &&(p.STORYBOARD_DETAIL_ID== oItm.storyBoardDtl_Id)
                    //    ).FirstOrDefault();
                    if (oExist == null)
                    {
                        lstFromDicCmp.Add(oTmp);
                        //lstcmpRslt.Add(oTmp);
                    }
                    else
                    {
                        //说明有重复的，需要保留run-order 在后面的数据                        
                        if (oExist.steps_run_ord < oTmp.steps_run_ord)
                        {
                            //lstcmpRslt.Remove(oExist);
                            //lstcmpRslt.Add(oTmp);
                            lstFromDicCmp.Remove(oExist);
                            lstFromDicCmp.Add(oTmp);
                        }
                    }
                }
                
                Logger.Info("GetStoryboardRows", string.Format("log time storyBoardResult2, data count:[{0}]", dicTmpRslt1.Count));
                
                foreach (var oItm in storyBoardResult2.ToList())
                {
                    if (oItm.storyBoardDtl_Id == null) continue;
                    Tmp_cmpResult oTmp = new Tmp_cmpResult();
                    oTmp.returnValue1 = CorrectResultData(oItm.value1);
                    oTmp.returnValue2 = CorrectResultData(oItm.value2);
                    oTmp.STORYBOARD_DETAIL_ID = oItm.storyBoardDtl_Id;
                    oTmp.STORYBOARD_ID = oItm.STORYBOARD_ID;

                    oTmp.InputValueSetting1 = oItm.INPUT_VALUE_SETTING1;
                    oTmp.InputValueSetting2 = oItm.INPUT_VALUE_SETTING2;

                    if ((dictStepsWithStoryboardDetailId != null) && (oTmp.STORYBOARD_DETAIL_ID != null))
                    {
                        if (dictStepsWithStoryboardDetailId.ContainsKey((long)oTmp.STORYBOARD_DETAIL_ID))
                        {
                            List<T_TEST_STEPSDTO> lst = dictStepsWithStoryboardDetailId[(long)oTmp.STORYBOARD_DETAIL_ID];
                            if (lst != null)
                            {
                                var objStp = lst.Where(p => p.STEPS_ID == oItm.Steps_Id).FirstOrDefault();
                                if (objStp == null) continue;
                                //string strFunc = lst.Where(p => p.STEPS_ID == oItm.Steps_Id).Select(p => p.COMMENT).FirstOrDefault();
                                string strFunc = objStp.COMMENT;  //lst.Where(p => p.STEPS_ID == oItm.Steps_Id).Select(p => p.COMMENT).FirstOrDefault();
                                oTmp.CheckValue(strFunc);
                                oTmp.steps_run_ord = (int)(objStp.RUN_ORDER ?? -1);
                            }
                            else
                                oTmp.CheckValue();
                        }
                        else
                            oTmp.CheckValue();
                    }
                    else
                        oTmp.CheckValue();

                    List<Tmp_cmpResult> lstFromDicCmp = null;
                    long lIdxOfDtl = oItm.storyBoardDtl_Id ?? -1;
                    if (!dicTmpRslt2.ContainsKey(lIdxOfDtl))
                    {
                        dicTmpRslt2.Add(lIdxOfDtl, lstFromDicCmp = new List<Tmp_cmpResult>());
                    }
                    lstFromDicCmp = dicTmpRslt2[lIdxOfDtl];

                    var oExist = lstFromDicCmp.Where(p => string.Compare(p.InputValueSetting2, oTmp.InputValueSetting2) == 0)
                                .Where(p => string.Compare(p.InputValueSetting1, oTmp.InputValueSetting1) == 0)
                                .FirstOrDefault();
                    //var oExist = lstcmpRslt2.Where(p => p.STORYBOARD_DETAIL_ID == oTmp.STORYBOARD_DETAIL_ID)
                    //            .Where(p => string.Compare(p.InputValueSetting2, oTmp.InputValueSetting2) == 0)
                    //            .Where(p => string.Compare(p.InputValueSetting1, oTmp.InputValueSetting1) == 0)
                    //            .FirstOrDefault();

                    //var oExist = lstcmpRslt.Where(p => (string.Compare(p.InputValueSetting1, oTmp.InputValueSetting1) == 0)
                    //    && (string.Compare(p.InputValueSetting2, oTmp.InputValueSetting2) == 0)
                    //    && (p.STORYBOARD_DETAIL_ID==oTmp.STORYBOARD_DETAIL_ID)).FirstOrDefault();
                    if (oExist == null)
                    {
                        //lstcmpRslt2.Add(oTmp);
                        lstFromDicCmp.Add(oTmp);
                    }
                    else
                    {
                        //说明有重复的，需要保留run-order 在后面的数据                        
                        if (oExist.steps_run_ord < oTmp.steps_run_ord)
                        {
                            //lstcmpRslt2.Remove(oExist);
                            //lstcmpRslt2.Add(oTmp);

                            lstFromDicCmp.Remove(oExist);
                            lstFromDicCmp.Add(oTmp);
                        }
                    }
                }

                Logger.Info("GetStoryboardRows", "storyBoardResult returns ");
                var storyBoardRows = (from c in marsEntities.V_STORYBOARD_TEST_FULLVISION
                                          //from d in marsEntities.V_TEST_DATARPT_CAPTURE
                                      where
                                            (c.STORYBOARD_ID == storyboarId)                                       
                                      //&& c.STORYBOARD_DETAIL_ID == d.STORYBOARD_DETAIL_ID
                                      //&& 
                                      orderby c.RUN_ORDER
                                      orderby c.STORYBOARD_NAME
                                      orderby c.TEST_CASE_END_TIME descending
                                      select c);
                var lstStoryBordRows = storyBoardRows.ToList();
                Logger.Info("GetStoryboardRows", string.Format("storyBoardRows build {0}", lstStoryBordRows.Count));
                foreach (V_STORYBOARD_TEST_FULLVISION storyBoardRow in lstStoryBordRows)
                {
                    if (storyBoardRowList.Where(p => p.RUN_ORDER == storyBoardRow.RUN_ORDER).FirstOrDefault() != null) continue;
                    B_STORYBOARD_TEST_FULLVISION newStoryboardRow = new B_STORYBOARD_TEST_FULLVISION();
                    newStoryboardRow.STORYBOARD_DETAIL_ID = storyBoardRow.STORYBOARD_DETAIL_ID;
                    newStoryboardRow.PROJECT_ID = storyBoardRow.PROJECT_ID;
                    newStoryboardRow.PROJECT_NAME = storyBoardRow.PROJECT_NAME;
                    newStoryboardRow.PROJECT_DESCRIPTION = storyBoardRow.PROJECT_DESCRIPTION;
                    newStoryboardRow.TEST_CASE_NAME = storyBoardRow.TEST_CASE_NAME;
                    newStoryboardRow.TEST_CASE_ID = storyBoardRow.TEST_CASE_ID;
                    newStoryboardRow.TEST_STEP_DESCRIPTION = storyBoardRow.TEST_STEP_DESCRIPTION;
                    newStoryboardRow.TEST_SUITE_ID = storyBoardRow.TEST_SUITE_ID;
                    newStoryboardRow.TEST_SUITE_NAME = storyBoardRow.TEST_SUITE_NAME;
                    newStoryboardRow.TEST_SUITE_DESCRIPTION = storyBoardRow.TEST_SUITE_DESCRIPTION;
                    newStoryboardRow.RUN_ORDER = storyBoardRow.RUN_ORDER;
                    newStoryboardRow.DEPENDS_ON = storyBoardRow.DEPENDS_ON;
                    newStoryboardRow.ALIAS_NAME = storyBoardRow.ALIAS_NAME;
                    newStoryboardRow.DISPLAY_NAME = storyBoardRow.DISPLAY_NAME;
                    newStoryboardRow.TEST_RUN_VALUE = storyBoardRow.TEST_RUN_VALUE;
                    newStoryboardRow.LATEST_TEST_MARK_ID = storyBoardRow.LATEST_TEST_MARK_ID;
                    newStoryboardRow.HIST_LATEST_TEST_MARK_ID = storyBoardRow.HIST_LATEST_TEST_MARK_ID;
                    newStoryboardRow.HIST_ID = storyBoardRow.HIST_ID;
                    newStoryboardRow.HIST_TEST_ID = storyBoardRow.HIST_TEST_ID;
                    newStoryboardRow.TEST_CASE_BEGIN_TIME = storyBoardRow.TEST_CASE_BEGIN_TIME;
                    newStoryboardRow.TEST_CASE_END_TIME = storyBoardRow.TEST_CASE_END_TIME;
                    newStoryboardRow.HIST_TEST_RESULT_IN_TEXT = storyBoardRow.HIST_TEST_RESULT_IN_TEXT;
                    newStoryboardRow.HIST_TEST_MODE = storyBoardRow.HIST_TEST_MODE;
                    newStoryboardRow.HIST_RESULT = storyBoardRow.HIST_RESULT;
                    newStoryboardRow.PARENT_ALIAS_NAME = storyBoardRow.PARENT_ALIAS_NAME;
                    newStoryboardRow.STORYBOARD_NAME = storyBoardRow.STORYBOARD_NAME;
                    newStoryboardRow.STORYBOARD_ID = storyBoardRow.STORYBOARD_ID;
                    newStoryboardRow.DATA_SET_ALIAS_NAME = storyBoardRow.DATA_SET_ALIAS_NAME;
                    newStoryboardRow.DATA_SETTING_ID = storyBoardRow.DATA_SETTING_ID;
                    newStoryboardRow.DATA_SUMMARY_ID = storyBoardRow.DATA_SUMMARY_ID;
                    newStoryboardRow.DATASET_DESCRIPTION = storyBoardRow.DATASET_DESCRIPTION;

#region for testing
                    //if (newStoryboardRow.RUN_ORDER == 17)
                    //{
                    //    var lst = lstcmpRslt.Where(p => p.STORYBOARD_DETAIL_ID == newStoryboardRow.STORYBOARD_DETAIL_ID).ToList();
                    //    Logger.Info("GetStoryboardRows", string.Format("runOrder:[{0}], detail id:[{1}], data count:[{2}]", newStoryboardRow.RUN_ORDER, newStoryboardRow.STORYBOARD_DETAIL_ID,
                    //        lst.Count
                    //        ));
                    //    foreach (var itm in lst)
                    //    {
                    //        if (itm == null) continue;
                    //        Logger.Info("GetStoryboardRows", itm.ToString());
                    //    }
                    //}
#endregion
                    


                    if (newStoryboardRow.HIST_RESULT != 2)
                    {
                        List<Tmp_cmpResult> tmpCmp1 = dicTmpRslt1.ContainsKey(newStoryboardRow.STORYBOARD_DETAIL_ID) ?dicTmpRslt1[newStoryboardRow.STORYBOARD_DETAIL_ID]:null;
                        List<Tmp_cmpResult> tmpCmp2 = dicTmpRslt2.ContainsKey(newStoryboardRow.STORYBOARD_DETAIL_ID) ?dicTmpRslt2[newStoryboardRow.STORYBOARD_DETAIL_ID]:null;
                        var leftErrorMark = tmpCmp1==null?null:tmpCmp1.Where(p => p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID)
                            .Where(p => !p.isResultRight)
                            .FirstOrDefault();
                        var rightErrorMark = tmpCmp2==null?null:tmpCmp2.Where(p => p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID)
                            .Where(p => !p.isResultRight)
                            .FirstOrDefault();
                        //var rightErrorMar = 
                        //var leftErrorMark = lstcmpRslt
                        //    .Where(p => (p.STORYBOARD_DETAIL_ID == newStoryboardRow.STORYBOARD_DETAIL_ID))
                        //    .Where(p => p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID)
                        //    .Where(p => !p.isResultRight)
                        //    .FirstOrDefault();
                        //    //FirstOrDefault(p => (p.STORYBOARD_DETAIL_ID == newStoryboardRow.STORYBOARD_DETAIL_ID) && (p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID) && (!p.isResultRight));
                        //var rightErrorMark = lstcmpRslt2
                        //    .Where(p => (p.STORYBOARD_DETAIL_ID == newStoryboardRow.STORYBOARD_DETAIL_ID))
                        //    .Where(p => p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID)
                        //    .Where(p => !p.isResultRight)
                        //    .FirstOrDefault();
                        //.FirstOrDefault(p => (p.STORYBOARD_DETAIL_ID == newStoryboardRow.STORYBOARD_DETAIL_ID) && (p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID) && (!p.isResultRight));
                        //var objTmpReviseObj = lstcmpRslt.FirstOrDefault(p => (p.STORYBOARD_DETAIL_ID == newStoryboardRow.STORYBOARD_DETAIL_ID) && (p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID) && (!p.isResultRight));
                        //storyBoardResult.FirstOrDefault(p => (p.storyBoardDtl_Id == newStoryboardRow.STORYBOARD_DETAIL_ID) && (p.STORYBOARD_ID == newStoryboardRow.STORYBOARD_ID) && (p.returnValue1 == false));
                        //if (objTmpReviseObj != null)
                        //System.Windows.Forms.MessageBox.Show(string.Format("{0}-{1}", leftErrorMark==null?"N/A BASE":leftErrorMark.returnValue1,
                        //    rightErrorMark == null ? "N/A BASE" : rightErrorMark.returnValue1));

                        if ((leftErrorMark != null) || (rightErrorMark != null))
                        {

                            //if ((leftErrorMark == null) || (rightErrorMark == null))
                            //{

                            //    //至少一个没有运行过
                            //    newStoryboardRow.HIST_RESULT = 1;
                            //}
                            //else
                            //{
                                var errorInfo = leftErrorMark == null ? rightErrorMark : leftErrorMark;
                                //var errorInfo = objTmpReviseObj ;
                                Logger.Warnning("GetStoryboardRows", string.Format("storyboard runorder:[{0}] warnning data:[{1}]-[{2}]",
                                    storyBoardRow.RUN_ORDER,
                                    errorInfo.InputValueSetting1 + "," + errorInfo.returnValue1,
                                    errorInfo.InputValueSetting2 + "," + errorInfo.returnValue2));
                                newStoryboardRow.HIST_RESULT = errorInfo.isResultRight ? newStoryboardRow.HIST_RESULT : 3;
                            if (newStoryboardRow.HIST_RESULT==3)
                            {
                                int tstCount = lstStoryboardInfo.Count(p => p.STORYBOARD_DETAIL_ID == newStoryboardRow.STORYBOARD_DETAIL_ID);
                                if (tstCount!=2)
                                {
                                    Logger.Info("-------",string.Format("not matched, run_order:[{0}], count:[{1}] storyboard:[{2}]", newStoryboardRow.RUN_ORDER, tstCount, newStoryboardRow.STORYBOARD_ID));
                                    newStoryboardRow.HIST_RESULT = 1;
                                }
                            }

                            //}
                        }

                    }

                    storyBoardRowList.Add(newStoryboardRow);
                }
                Logger.Info("GetStoryboardRows", string.Format("storyBoardRows returns rows:[{0}]", storyBoardRowList.Count));
                Logger.logEnd("GetStoryboardRows");
                return storyBoardRowList;
            }
            catch (Exception e)
            {
                Logger.Error("GetStoryboardRows",string.Format("Exception:[{0}] stackTrace:\r\n[{1}]",e.Message,e.StackTrace),e);
                return null;                
            }
        }

        private static Dictionary<int, List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>> GetStoryboardFullVisionByCnn(string strDBIdx,long storyboarId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetStoryboardFullVisionByCnn", string.Format("storyboard:[{0}],all mode]-0-base, 1-none base ", storyboarId));
            DbConnection dbCnn = null;
            List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>> result = new List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>();
            try
            {
                MarsEntities dbCntx = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if ((dbCnn = dbCntx.Database.Connection).State != ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                DbCommand dbCmmd = dbCnn.CreateCommand();
                string strSql = string.Format(@"SELECT a.*,b.STEPS_ID FROM V_TEST_DATARPT_CAPTURE a, T_TEST_REPORT_STEPS b
                                                WHERE a.TEST_MODE in (0,1) AND a.STORYBOARD_ID={0} 
                                                AND b.TEST_REPORT_STEP_ID=a.TEST_REPORT_STEP_ID", storyboarId);
                dbCmmd.CommandText = strSql;
                DbDataReader dbRd = dbCmmd.ExecuteReader();

                if (!dbRd.HasRows)
                {
                    isOk = true;
                    return new Dictionary<int, List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>>();
                }

                Dictionary<int, List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>> resultDic = new Dictionary<int, List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>>();
                while (dbRd.Read())
                {
                    V_TEST_DATARPT_CAPTUREDTO storyboardInfo = new V_TEST_DATARPT_CAPTUREDTO();
                    storyboardInfo.KEY_WORD_NAME = (dbRd["KEY_WORD_NAME"] == DBNull.Value ? null : (string)dbRd["KEY_WORD_NAME"]);
                    storyboardInfo.OBJECT_HAPPY_NAME = dbRd["OBJECT_HAPPY_NAME"] == DBNull.Value ? null : (string)dbRd["OBJECT_HAPPY_NAME"];
                    storyboardInfo.INPUT_VALUE_SETTING = dbRd["INPUT_VALUE_SETTING"] == DBNull.Value ? null : (string)dbRd["INPUT_VALUE_SETTING"];
                    storyboardInfo.RETURN_VALUES = dbRd["RETURN_VALUES"] == DBNull.Value ? null : (string)dbRd["RETURN_VALUES"];
                    storyboardInfo.ACTUAL_INPUT_DATA = dbRd["ACTUAL_INPUT_DATA"] == DBNull.Value ? null : (string)dbRd["ACTUAL_INPUT_DATA"];
                    storyboardInfo.LATEST_TEST_MARK_ID = dbRd["LATEST_TEST_MARK_ID"] == DBNull.Value ? null : (Nullable<Int64>)dbRd["LATEST_TEST_MARK_ID"];
                    storyboardInfo.STORYBOARD_DETAIL_ID = dbRd["STORYBOARD_DETAIL_ID"] == DBNull.Value ? null : (Nullable<Int64>)dbRd["STORYBOARD_DETAIL_ID"];
                    storyboardInfo.LOOP_ID = dbRd["LOOP_ID"] == DBNull.Value ? null : (Nullable<Decimal>)dbRd["LOOP_ID"];
                    storyboardInfo.RUN_ORDER = dbRd["RUN_ORDER"] == DBNull.Value ? null : (Nullable<Decimal>)dbRd["RUN_ORDER"];
                    storyboardInfo.TEST_MODE = dbRd["TEST_MODE"] == DBNull.Value ? null : (Nullable<Int16>)dbRd["TEST_MODE"];
                    storyboardInfo.TEST_REPORT_STEP_ID = (Int64)dbRd["TEST_REPORT_STEP_ID"];
                    storyboardInfo.STORYBOARD_ID = dbRd["STORYBOARD_ID"] == DBNull.Value ? null : (Nullable<Int64>)dbRd["STORYBOARD_ID"];

                    long step_id = dbRd["STEPS_ID"] == DBNull.Value ? -1 : (long)dbRd["STEPS_ID"];

                    if (storyboardInfo.TEST_MODE == null) continue;
                    if (resultDic.ContainsKey((int)storyboardInfo.TEST_MODE))
                    {
                        result = resultDic[(int)storyboardInfo.TEST_MODE];
                    }
                    else
                    {
                        resultDic.Add((int)storyboardInfo.TEST_MODE,
                            result = new List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>());
                    }

                    result.Add(new KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>(storyboardInfo, step_id));
                }
                isOk = true;
                return resultDic;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetStoryboardFullVisionByCnn", strError = string.Format("Exception:[{0}]", e.Message), e);
                return new Dictionary<int, List<KeyValuePair<V_TEST_DATARPT_CAPTUREDTO, long>>>();
            }
            finally
            {
                if (dbCnn != null)
                {
                    try
                    {
                        dbCnn.Close();
                    }
                    catch (Exception)
                    {

                    }
                }
                Logger.logEnd("GetStoryboardFullVisionByCnn");
            }
        }


        private static List<V_TEST_DATARPT_CAPTUREDTO> GetStoryboardFullVisionByCnn(string strDBIdx, int testMode, long storyboarId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetStoryboardFullVisionByCnn",string.Format("storyboard:[{0}]-Mode:[{1}]-0-base, 1-none base ",storyboarId, testMode));
            DbConnection dbCnn = null;
            List<V_TEST_DATARPT_CAPTUREDTO> result = new List<V_TEST_DATARPT_CAPTUREDTO>();
            try
            {
                MarsEntities dbCntx = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if ((dbCnn=dbCntx.Database.Connection).State!= ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                DbCommand dbCmmd = dbCnn.CreateCommand();
                string strSql = string.Format("SELECT * FROM V_TEST_DATARPT_CAPTURE WHERE TEST_MODE={0} AND STORYBOARD_ID={1}", testMode, storyboarId);
                dbCmmd.CommandText = strSql;
                DbDataReader dbRd = dbCmmd.ExecuteReader();
                if (!dbRd.HasRows) return new List<V_TEST_DATARPT_CAPTUREDTO>();

                while(dbRd.Read())
                {
                    V_TEST_DATARPT_CAPTUREDTO storyboardInfo = new V_TEST_DATARPT_CAPTUREDTO();
                    storyboardInfo.KEY_WORD_NAME = (dbRd["KEY_WORD_NAME"] == DBNull.Value?null: (string)dbRd["KEY_WORD_NAME"]);
                    storyboardInfo.OBJECT_HAPPY_NAME = dbRd["OBJECT_HAPPY_NAME"] == DBNull.Value ? null : (string)dbRd["OBJECT_HAPPY_NAME"];
                    storyboardInfo.INPUT_VALUE_SETTING = dbRd["INPUT_VALUE_SETTING"] == DBNull.Value ? null : (string)dbRd["INPUT_VALUE_SETTING"]; 
                    storyboardInfo.RETURN_VALUES = dbRd["RETURN_VALUES"] == DBNull.Value ? null : (string)dbRd["RETURN_VALUES"];
                    storyboardInfo.ACTUAL_INPUT_DATA = dbRd["ACTUAL_INPUT_DATA"] == DBNull.Value ? null : (string)dbRd["ACTUAL_INPUT_DATA"];
                    storyboardInfo.LATEST_TEST_MARK_ID = dbRd["LATEST_TEST_MARK_ID"] == DBNull.Value ? null : (Nullable<Int64>)dbRd["LATEST_TEST_MARK_ID"];
                    storyboardInfo.STORYBOARD_DETAIL_ID = dbRd["STORYBOARD_DETAIL_ID"] == DBNull.Value ? null : (Nullable<Int64>)dbRd["STORYBOARD_DETAIL_ID"];
                    storyboardInfo.LOOP_ID = dbRd["LOOP_ID"] == DBNull.Value ? null : (Nullable<Decimal>)dbRd["LOOP_ID"];
                    storyboardInfo.RUN_ORDER = dbRd["RUN_ORDER"] == DBNull.Value ? null : (Nullable<Decimal>)dbRd["RUN_ORDER"];
                    storyboardInfo.TEST_MODE = dbRd["TEST_MODE"] == DBNull.Value ? null : (Nullable<Int16>)dbRd["TEST_MODE"];
                    storyboardInfo.TEST_REPORT_STEP_ID = (Int64)dbRd["TEST_REPORT_STEP_ID"];                    
                    storyboardInfo.STORYBOARD_ID = dbRd["STORYBOARD_ID"] == DBNull.Value ? null : (Nullable<Int64>)dbRd["STORYBOARD_ID"];
                    result.Add(storyboardInfo);
                }
                isOk = true;
                return result;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetStoryboardFullVisionByCnn",strError = string.Format("Exception:[{0}]", e.Message), e);
                return new List<V_TEST_DATARPT_CAPTUREDTO>();
            }
            finally
            {
                if (dbCnn!=null)
                {
                    try
                    {
                        dbCnn.Close();
                    }
                    catch (Exception)
                    {
                        
                    }
                }
                Logger.logEnd("GetStoryboardFullVisionByCnn");
            }
        }

        public static bool GetIFVariableInfo(string strDBIdx, string strIfVarIdx, short sStatus, ref string strError, ref string strResult)
        {
            Logger.Info("GetIFVariableInfo", string.Format("strIfVarIdx:[{0}] sValue?:[{1}]", strIfVarIdx, sStatus));
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                if (objSysLook.IsNotItemExists(strDBIdx,B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_IF, strIfVarIdx, null, sStatus))
                {
                    objSysLook.CreateEmptyItem(strDBIdx,B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_IF, strIfVarIdx, ref strError, null, sStatus);
                }
                List<B_SYSTEM_LOOKUP> lstObjs = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_IF, strIfVarIdx);
                if ((lstObjs == null) || (lstObjs.Count == 0))
                {
                    Logger.Error("GetIFVariableInfo", strError = string.Format("Can't find IFVAR variable,not regiested:[{0}]", strIfVarIdx));
                    return false;
                }
                var q = lstObjs.Where(p => p.STATUS == sStatus).FirstOrDefault();
                if (q == null)
                {
                    strResult = "";
                    strError = string.Format("Can't find IFVAR value for [{0}] ", strIfVarIdx);
                    return false;
                }
                strResult = q.DISPLAY_NAME;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetIFVariableInfo", strError = string.Format("Exception when Get if variable:[{0}]\r\n StackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static bool GetModalVariableInfo(string strModalVarIdx, short sStatus, ref string strError, ref string strResult,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.Info("GetModalVariableInfo",string.Format("strModalVarIdx:[{0}] sValue?:[{1}] dbIdx:[{2}]", strModalVarIdx, sStatus, strDBIdx));
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                List<B_SYSTEM_LOOKUP> lstModalVars = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL, strModalVarIdx, strDBIdx);
                if ((lstModalVars==null)||(lstModalVars.Count==0))
                {
#if !_forWebClient
                    objSysLook.createModualvar(new List<string> { strModalVarIdx }, ref strError, strDBIdx);
#else
                    objSysLook.createModualvar(strDBIdx,new List<string> { strModalVarIdx }, ref strError);
#endif
                }
                else
                {
                    if (lstModalVars.Count>2)
                    {
                        Logger.Error("GetModalVariableInfo", strError = string.Format("There are [{0}] modal variables -[{1}], only two for one variable name are allowed, \r\nOne for baseline, one Non-Baseline, \r\nplease remove unused variable from tools->Edit Variables."
                            , lstModalVars.Count, strModalVarIdx));
                        return false;
                    }
                    if (lstModalVars.Count == 1)
                    {
                        short shrtStsForCreating = -1;
                        if (lstModalVars[0].STATUS == sStatus)
                        {
                            shrtStsForCreating = (short)(sStatus == 1 ? 2 : 1);
                        }
                        else
                        {
                            shrtStsForCreating = sStatus;
                        }
#if !_forWebClient
                        objSysLook.createModualvar(new List<string> { strModalVarIdx }, ref strError, strDBIdx, null, shrtStsForCreating);
#else
                        objSysLook.createModualvar(strDBIdx,new List<string> { strModalVarIdx }, ref strError, shrtStsForCreating);
#endif
                    }
                }
                
                List<B_SYSTEM_LOOKUP> lstObjs = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL, strModalVarIdx,strDBIdx);
                if ((lstObjs == null) || (lstObjs.Count == 0))
                {
                    Logger.Error("GetModalVariableInfo", strError = string.Format("Can't find modula variable,not registed:[{0}]", strModalVarIdx));
                    return false;
                }
                var q = lstObjs.Where(p => p.STATUS == sStatus).FirstOrDefault();
                if (q==null)
                {
                    strResult = "";
                    strError = string.Format("Can't find modual value for [{0}] ", strModalVarIdx);
                    return false;
                }
                strResult = q.DISPLAY_NAME;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetModalVariableInfo",strError = string.Format("Exception when Get Modual variable:[{0}]\r\n StackTrace:[{1}]", e.Message,e.StackTrace), e);
                return false;
            }
        }
#if !(_forWebSvc || _forWebClient)
        public static bool GetBussinessSeq(ref int iN, ref string strError,string strSeqName="T_KEYWORD_SEQ", string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        public bool GetBussinessSeq(ref int iN, ref string strError, string strSeqName = "T_KEYWORD_SEQ", string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            Logger.Info("GetBussinessSeq",string.Format("SeqName:[{0}] dbIdx:[{1}]",strSeqName,strDBIdx));
            try
            {
                //MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                iN = (int)GetIdBySeqName( strSeqName, strDBIdx:strDBIdx);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetBussinessSeq", string.Format("Exception:[{0}] stackTrace:[{1}]", strError=e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static bool GetModalVariableInfo(string strDBIdx,List<string> lstModalVarIdx, short sStatus, ref string strError, ref Dictionary<string, string> dicResult)
        {
            string strModalVarsWithComma = "";
            Logger.Info("GetModalVariableInfo", string.Format("strModalVarIdx:[{0}] sValue?:[{1}]", strModalVarsWithComma=string.Join(",", lstModalVarIdx), sStatus));
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                
                List<B_SYSTEM_LOOKUP> lstModalVars = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL, lstModalVarIdx,strDBIdx);                
                if ((lstModalVars == null) )
                {
                    Logger.Error("GetModalVariableInfo", strError = string.Format("Can't find modula variable,not regiested:[{0}]", strModalVarsWithComma));
                    return false;
                }
                //find variables aren't exists in db
                var lstVarNeedToBeCreate = lstModalVarIdx.Where(p => !lstModalVars.Any(z => string.Compare(p, z.FIELD_NAME) == 0)).ToList();
                if ((lstVarNeedToBeCreate != null)&&(lstVarNeedToBeCreate.Count>0))
                {
                    // create all modal varibelse
                    if (!objSysLook.createModualvar(strDBIdx:strDBIdx, lstName: lstVarNeedToBeCreate, strError:ref strError))
                    {
                        Logger.Error("GetModalVariableInfo", strError =string.Format("No such variables exists and can't created with error :{1}", string.Join(",",lstVarNeedToBeCreate), strError));
                        return false;
                    }
                    //re-get from db
                    lstModalVars = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL, lstModalVarIdx,strDBIdx);
                }

                var q = lstModalVars.Where(p => p.STATUS == sStatus);
                if (q == null)
                {
                    dicResult.Clear();
                    strError = string.Format("Can't find modual value for [{0}] ", strModalVarsWithComma);
                    return false;
                }
                foreach(var v in q)
                {
                    if (v == null) continue;
                    if (dicResult.Keys.Contains(v.FIELD_NAME))
                    {
                        dicResult[v.FIELD_NAME] = v.DISPLAY_NAME;
                    }
                    else
                    {
                        dicResult.Add(v.FIELD_NAME, v.DISPLAY_NAME);
                    }

                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetModalVariableInfo", strError = string.Format("Exception when Get Modual variable:[{0}]\r\n StackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }
                           
        public static bool GetLoopVaribleInfo(string strLoopVarIdx, ref string strError, ref string strResult,string strDBIdx)
        {
            Logger.logBegin("GetLoopVaribleInfo", string.Format("try to get vars by idx:[{0}]", strLoopVarIdx));
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                List<B_SYSTEM_LOOKUP> lstLoopVars = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, strLoopVarIdx,strDBIdx);
                if (lstLoopVars == null)
                {
                    strResult = "";
                    return true;
                }
                B_SYSTEM_LOOKUP       oTarget     = lstLoopVars.FirstOrDefault(p => p != null)                                            ;
                if (oTarget == null)
                {
                    strError = string.Format("no such loop var in database:[{0}]", strLoopVarIdx);
                    return false;
                }
                strResult = oTarget.DISPLAY_NAME;
                return true;
                
            }catch(Exception e)
            {
                Logger.Error("\t",strError = string.Format("Exception:[{0}]",e.Message ),e);
                return false;
            }
            finally
            {
                Logger.logEnd("GetLoopVaribleInfo");
            }
        }

        public static bool GetLocalVariableInfo(string strDBIdx,List<string> lstLocalVarIndex, ref string strError, ref Dictionary<string, string> dicResult)
        {
            Logger.Info("GetLocalVariableInfo", string.Format("Get LocalVars info by list:[{0}]", lstLocalVarIndex));
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                List<B_SYSTEM_LOOKUP> lstLocalVars = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOCAL, lstLocalVarIndex, strDBIdx);
                if (dicResult == null) dicResult = new Dictionary<string, string>();
                if (lstLocalVars == null) return true;
                for (int i = 0; i < lstLocalVars.Count; i++)
                {
                    var itm = lstLocalVars[i];
                    if (dicResult.Keys.Contains(itm.FIELD_NAME))
                    {
                        dicResult[itm.FIELD_NAME] = itm.DISPLAY_NAME;
                    }
                    else
                    {
                        dicResult.Add(itm.FIELD_NAME, itm.DISPLAY_NAME);
                    }
                };
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetLocalVariableInfo", strError = string.Format("Exception :[{0}]", e.Message), e);
                return false;
            }
        }

        public static bool GetGlobalVariableInfo(string strDBIdx,List<string> lstGlobeVarIndex, ref string strError, ref Dictionary<string,string> dicResult)
        {
            Logger.Info("GetGlobalVariableInfo", string.Format("Get GlobalVars info by list:[{0}]", lstGlobeVarIndex));
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                List<B_SYSTEM_LOOKUP> lstGlobarVars = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, lstGlobeVarIndex,strDBIdx);
                if (dicResult == null) dicResult = new Dictionary<string, string>();
                if (lstGlobarVars == null) return true;

                var lstVarNeedToBeCreate = lstGlobeVarIndex.Where(p => !lstGlobarVars.Any(z => string.Compare(p, z.FIELD_NAME) == 0)).ToList();

                if ((lstVarNeedToBeCreate != null) && (lstVarNeedToBeCreate.Count > 0))
                {
                    // create all modal varibelse
                    if (!objSysLook.createGlobalVar(strDBIdx,lstVarNeedToBeCreate, ref strError))
                    {
                        Logger.Error("GetModalVariableInfo", strError = string.Format("No such variables exists and can't created with error :{1}", string.Join(",", lstVarNeedToBeCreate), strError));
                        return false;
                    }
                    //re-get from db
                    lstGlobarVars = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, lstGlobeVarIndex,strDBIdx);
                }

                for (int i=0;i<lstGlobarVars.Count;i++)
                {
                    var itm = lstGlobarVars[i];
                    if (dicResult.Keys.Contains(itm.FIELD_NAME))
                    {
                        dicResult[itm.FIELD_NAME] = itm.DISPLAY_NAME;
                    }
                    else
                    {
                        dicResult.Add(itm.FIELD_NAME,itm.DISPLAY_NAME);
                    }
                }

                /// 

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetGlobalVariableInfo", strError = string.Format("Exception :[{0}]", e.Message), e);
                return false;
            }
        }

#if !_forWebSvc
        public static bool GetLoopVariableInfo(string strLoopVarIdx, ref string strError, ref string strResult, 
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        public bool GetLoopVariableInfo(string strLoopVarIdx, ref string strError, ref string strResult,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            Logger.Info("GetLoopVariableInfo", string.Format("Get GlobalVars info :[{0}]", strLoopVarIdx));
#if !_forWebClient
            //MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                if (objSysLook.IsNotItemExists(strDBIdx,B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, strLoopVarIdx))
                {
                    objSysLook.CreateEmptyItem(strDBIdx,B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, strLoopVarIdx, ref strError);
                }
                List<B_SYSTEM_LOOKUP> lstObjs = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, strLoopVarIdx, strDBIdx);

                if ((lstObjs == null) || (lstObjs.Count == 0))
                {
                    if (objSysLook.CreateOrUpdateLoopVar(strLoopVarIdx ,"", ref strError,strDBIdx))
                    {
                        strResult = "";
                        return true;
                    }
                    else
                    {
                        Logger.Error("GetGlobalVariableInfo", strError = string.Format("Can't find Loop,not regiested:[{0}]", strLoopVarIdx));
                        return false;
                    }
                }
                strResult = lstObjs[0].DISPLAY_NAME;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetGlobalVariableInfo", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
#else
            try
            {
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                bool isOk = false;
                List<B_SYSTEM_LOOKUP> lstLoopVar = clnt.GetVariableInfo(strLoopVarIdx, 
                    ref isOk, ref strError, ref strResult, 
                    B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP);
                if (!isOk) return false;
                if ((lstLoopVar == null) || (lstLoopVar.Count <= 0) || (lstLoopVar[0] == null))
                {
                    strError = string.Format("no such [{0}] global variable or is created", strLoopVarIdx);
                    return false;
                }
                strResult = lstLoopVar[0].DISPLAY_NAME ?? "";
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetGlobalVariableInfo", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
#endif
        }

#if _forWebSvc
        public bool GetGlobalVariableInfo(string strGlobeVarIndex, ref string strError, ref string strResult, string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#else
        public static bool GetGlobalVariableInfo(string strGlobeVarIndex, ref string strError, ref string strResult, string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            Logger.Info("GetGlobalVariableInfo",string.Format("Get GlobalVars info :[{0}], dbIdx:[{1}]", strGlobeVarIndex, strDBIdx));
#if !_forWebClient
            //MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                B_SYSTEM_LOOKUP objSysLook = new B_SYSTEM_LOOKUP();
                if (objSysLook.IsNotItemExists(strDBIdx,B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, strGlobeVarIndex))
                {
                    objSysLook.CreateEmptyItem(strDBIdx, B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, strGlobeVarIndex,ref strError);
                }
                List<B_SYSTEM_LOOKUP> lstObjs = objSysLook.GetSystemLookup(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, strGlobeVarIndex,strDBIdx:strDBIdx);

                if ((lstObjs==null)||(lstObjs.Count==0))
                {
                    if (objSysLook.createGlobalVar(strDBIdx: strDBIdx,new List<string>() { strGlobeVarIndex }, ref strError)) {
                        strResult = "";
                        return true;
                    }
                    else
                    {
                        Logger.Error("GetGlobalVariableInfo", strError = string.Format("Can't find globVar,not regiested:[{0}]", strGlobeVarIndex));
                        return false;
                    }
                }
                strResult = lstObjs[0].DISPLAY_NAME;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetGlobalVariableInfo",strError=string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
#else
            try
            {
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                bool isOk = false;
                List<B_SYSTEM_LOOKUP> lstGlbVar = clnt.GetVariableInfo(strGlobeVarIndex,ref isOk, ref strError, ref strResult);
                if (!isOk) return false;
                if ((lstGlbVar == null) || (lstGlbVar.Count <= 0) ||(lstGlbVar[0]==null))
                {
                    strError = string.Format("no such [{0}] global variable or is created", strGlobeVarIndex);
                    return false;
                }
                strResult = lstGlbVar[0].DISPLAY_NAME??"";
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetGlobalVariableInfo", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
#endif
        }

        public static List<B_PROJ_TS_TC_FULLVISION> GetProjectData(string strDBIdx, long  projectId)
        {
            List<B_PROJ_TS_TC_FULLVISION> projectDataList = new List<B_PROJ_TS_TC_FULLVISION>();

            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            var projectDataEntities = (from c in marsEntities.V_PROJ_TS_TC_FULLVISION
                                       where c.PROJECT_ID == projectId
                                  select c);

            //foreach (var dd in projectDataEntities)
            //    Console.WriteLine("BO - " + dd.TEST_CASE_NAME);

            // V_PROJ_TS_TC_FULLVISION

            foreach ( var projectDataRow in projectDataEntities)
            {
                B_PROJ_TS_TC_FULLVISION newProjectDataRow = B_PROJ_TS_TC_FULLVISION.ToBO(projectDataRow);
                projectDataList.Add(newProjectDataRow);
            } 
            return projectDataList;
        }


        public static long CreateTestDataSummary(string name, MarsEntities objDBCntx,ref bool isOk, ref string strError)
        {
            try
            {
                long id = GetTestStepsId(objDBCntx);
                T_TEST_DATA_SUMMARY bTestDataSummary = new T_TEST_DATA_SUMMARY();
                bTestDataSummary.DATA_SUMMARY_ID = id;
                bTestDataSummary.ALIAS_NAME = name;
                bTestDataSummary.DESCRIPTION_INFO = "DataSheet" + id;
                bTestDataSummary.CREATE_TIME = new DateTime();
                bTestDataSummary.STATUS = 0;
                //MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                //marsEntities.T_TEST_DATA_SUMMARY.Add(bTestDataSummary);
                objDBCntx.T_TEST_DATA_SUMMARY.Add(bTestDataSummary);

                //marsEntities.SaveChanges();
                isOk = true;
                return id;
            }
            catch (Exception e)
            {
                Logger.Error("CreateTestDataSummary",strError = string.Format("Exception:[{0}] stackTrace:\r\n{1} ",e.Message,e.StackTrace),e);
                isOk = false;
                return -1;
            }
        }

        public static long CreateSharedTestDataSummary(string strDBIdx, 
            string name, string dataSetDescription, MarsEntities objDbCntx = null )
        {
            long id = GetTestStepsId(objDbCntx);
            T_TEST_DATA_SUMMARY bTestDataSummary = new T_TEST_DATA_SUMMARY();
            bTestDataSummary.DATA_SUMMARY_ID = id;
            bTestDataSummary.ALIAS_NAME = name;
            bTestDataSummary.DESCRIPTION_INFO = dataSetDescription;
            bTestDataSummary.CREATE_TIME = new DateTime();
            bTestDataSummary.STATUS = 0;
            bTestDataSummary.SHARE_MARK = 1;

            MarsEntities marsEntities = objDbCntx?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            marsEntities.T_TEST_DATA_SUMMARY.Add(bTestDataSummary);

            if (objDbCntx==null)
                marsEntities.SaveChanges();
            return id;
        }

        public static void CreateRelTCDataSummary(string strDBIdx, 
            long summaryId, long testCaseId, MarsEntities objDbCntx = null)
        {
            long id = GetTestStepsId(objDbCntx);

            MarsEntities marsEntities = objDbCntx??BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            REL_TC_DATA_SUMMARY relTcDataSummary = new REL_TC_DATA_SUMMARY();
            relTcDataSummary.DATA_SUMMARY_ID = summaryId;
            relTcDataSummary.ID = id;
            relTcDataSummary.TEST_CASE_ID = testCaseId;
            relTcDataSummary.CREATE_TIME = new DateTime();
            marsEntities.REL_TC_DATA_SUMMARY.Add(relTcDataSummary);

            if (objDbCntx==null)
              marsEntities.SaveChanges();
        }

        public static void RemoveRelTCDataSummary(string strDBIdx, long id, long summaryId, long testCaseId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            REL_TC_DATA_SUMMARY relTcDataSummary = marsEntities.REL_TC_DATA_SUMMARY.FirstOrDefault(x=> x.ID == id);
            /*
            relTcDataSummary.DATA_SUMMARY_ID = summaryId;
            relTcDataSummary.ID = id;
            relTcDataSummary.TEST_CASE_ID = testCaseId;
            relTcDataSummary.CREATE_TIME = new DateTime();
            */
            if (relTcDataSummary != null)
                marsEntities.REL_TC_DATA_SUMMARY.Remove(relTcDataSummary);
            //marsEntities.SaveChanges();
        }


        public static IList<KeyValuePair<Int64, string>> GetDataSheetNames(string strDBIdx, long projectId, long testSuiteId, long testCaseId)
        {
           MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

           var data = (from c in marsEntities.V_PROJ_TS_TC_FULLVISION
                                                    where c.PROJECT_ID == projectId && 
                                                          c.TEST_SUITE_ID == testSuiteId && 
                                                          c.TEST_CASE_ID == testCaseId 
                                                    select c
                                                    ).ToList();

           IList<KeyValuePair<Int64, string>> dataSheets = new List<KeyValuePair<Int64, string>>();

           foreach (var dataItem in data)
           {
               KeyValuePair<Int64, string> item = new KeyValuePair<long, string>(dataItem.DATA_SUMMARY_ID, dataItem.DATA_ALIAS);
               dataSheets.Add(item);
           }
          
           return dataSheets;
        }

        public static long GetDataSheetId(string strDBIdx, long projectId, long testSuiteId, long testCaseId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            long dataId = ( from c in marsEntities.V_PROJ_TS_TC_FULLVISION
                            where c.PROJECT_ID == projectId && 
                                  c.TEST_SUITE_ID == testSuiteId && 
                                  c.TEST_CASE_ID == testCaseId
                            select c
                            ).FirstOrDefault().DATA_SUMMARY_ID;
            return dataId;
        }

        public static KeyValuePair<long, string> GetDataSheetInfo(string strDBIdx, long projectId, long testSuiteId, long testCaseId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            V_PROJ_TS_TC_FULLVISION dataId = (from c in marsEntities.V_PROJ_TS_TC_FULLVISION
                                              where c.PROJECT_ID == projectId &&
                                                    c.TEST_SUITE_ID == testSuiteId &&
                                                    c.TEST_CASE_ID == testCaseId
                                              select c
                            ).OrderBy(p=>p.DATA_ALIAS).FirstOrDefault();

            if (dataId == null) return default(KeyValuePair<long, string>);
            if (dataId.DATA_SUMMARY_ID <= 0) return default(KeyValuePair<long, string>);

            return new KeyValuePair<long, string>(dataId.DATA_SUMMARY_ID, dataId.DATA_ALIAS);
        }

        public static string GetDataSheetNameById(string strDBIdx, long dataSheetId)
        {
            string dataSheetName = "";
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            
            var dataSheet = (from c in marsEntities.T_TEST_DATA_SUMMARY
                             where c.DATA_SUMMARY_ID == dataSheetId
                             select c
                            ).OrderBy(p=>p.ALIAS_NAME).FirstOrDefault();

            if (dataSheet != null)
                dataSheetName = dataSheet.ALIAS_NAME;
            return dataSheetName;
        }

        public static List<B_LINKED_DATA_SHEET> GetLinkedDataSheet(string strDBIdx, long projectId, long testSuiteId, long testCaseId)
        {
            List<B_LINKED_DATA_SHEET> sheetList = new List<B_LINKED_DATA_SHEET>();
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            var allData = (from  c in marsEntities.V_PROJ_TS_TC_FULLVISION
                           where c.DATA_ALIAS != null && 
                                 c.DATA_SUMMARY_ID != -1 &&
                                 c.PROJECT_ID == projectId &&
                                 c.TEST_SUITE_ID == testSuiteId
                                 select c
                            );

            foreach (var data in allData)
            {
                B_LINKED_DATA_SHEET linkedDataSheet;

                linkedDataSheet = sheetList.FirstOrDefault(x=> x.Id == data.DATA_SUMMARY_ID);
                if (linkedDataSheet == null)
                {
                    linkedDataSheet = new B_LINKED_DATA_SHEET();

                    linkedDataSheet.DataItemName = data.DATA_ALIAS;
                    linkedDataSheet.Id = data.DATA_SUMMARY_ID;
                    linkedDataSheet.DataItemDescription = data.DATASET_DESCRIPTION;
                    linkedDataSheet.IsSelected = false;
 
                    sheetList.Add(linkedDataSheet);
                }

                if (data.PROJECT_ID == projectId &&
                        data.TEST_SUITE_ID == testSuiteId &&
                        data.TEST_CASE_ID == testCaseId)
                {
                    linkedDataSheet.IsSelected = true;
                }
            }
            

            return sheetList.OrderBy(x=> x.DataItemName.ToString()).ToList();
        }

        public static List<REL_TC_DATA_SUMMARY> GetRelTcDataSummary(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<REL_TC_DATA_SUMMARY> list = new List<REL_TC_DATA_SUMMARY>();

            var allData = (from c in marsEntities.REL_TC_DATA_SUMMARY
                           
                           select c
                           ).ToList();
            list = allData;

            return list;
               
        }

        public void SaveStoryboardSummaryByInst(string strDBIdx, B_STORYBOARD_SUMMARY summary,MarsEntities objDBCntx=null)
        {
            MarsEntities dbcntx = objDBCntx ?? (localMarsEntites??(localMarsEntites= GetMarsEntitiesInstance(strCurrentDB:strDBIdx)));
            
            SaveStoryboardSummary(strDBIdx, summary, dbcntx);
        }

        public static MarsEntities SaveStoryboardSummary(string strDBIdx, B_STORYBOARD_SUMMARY summary, MarsEntities objDbCntx=null)
        {
            MarsEntities marsEntities = objDbCntx ==null? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx): objDbCntx;
            T_STORYBOARD_SUMMARY sbSummary = T_STORYBOARD_SUMMARYAssembler.ToEntity((T_STORYBOARD_SUMMARYDTO)summary);
            marsEntities.T_STORYBOARD_SUMMARY.Add(sbSummary);
            if (objDbCntx==null)
            {
                marsEntities.SaveChanges();
            }
            return marsEntities;
        }

        
        public void SaveStoryboardRowsByInst(string strDBIdx, List<B_PROJ_TC_MGR> storyBoardRowList, MarsEntities objDbCntx )
        {
            MarsEntities dbCntx = objDbCntx ?? (localMarsEntites??(localMarsEntites = GetMarsEntitiesInstance(strCurrentDB:strDBIdx)));            
            SaveStoryboardRows(strDBIdx,storyBoardRowList, dbCntx);
        }

        public static void SaveStoryboardRows(string strDBIdx, List<B_PROJ_TC_MGR> storyBoardRowList, MarsEntities objDBCntx=null)
        {
            string action;
            MarsEntities marsEntities = objDBCntx == null?BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx): objDBCntx;
            foreach(B_PROJ_TC_MGR mgr in storyBoardRowList)
            {
                T_PROJ_TC_MGR sbMgr = (from s in marsEntities.T_PROJ_TC_MGR
                                       where s.STORYBOARD_DETAIL_ID == mgr.STORYBOARD_DETAIL_ID
                                       select s ).FirstOrDefault();
                if (sbMgr == null)
                {
                    action = "insert";
                    sbMgr = T_PROJ_TC_MGRAssembler.ToEntity((T_PROJ_TC_MGRDTO)mgr);
                    marsEntities.T_PROJ_TC_MGR.Add(sbMgr);
                }
                else
                {
                    action = "update";
                    sbMgr.STORYBOARD_DETAIL_ID = mgr.STORYBOARD_DETAIL_ID;
                    sbMgr.PROJECT_ID = mgr.PROJECT_ID;
                    sbMgr.TEST_CASE_ID = mgr.TEST_CASE_ID;
                    sbMgr.STORYBOARD_ID = mgr.STORYBOARD_ID;
                    sbMgr.RUN_TYPE = mgr.RUN_TYPE;
                    sbMgr.DEPENDS_ON = mgr.DEPENDS_ON;
                    sbMgr.RUN_ORDER = mgr.RUN_ORDER;
                    sbMgr.LATEST_TEST_MARK_ID = mgr.LATEST_TEST_MARK_ID;
                    sbMgr.RECORD_VERSION = mgr.RECORD_VERSION;
                    sbMgr.ALIAS_NAME = mgr.ALIAS_NAME;
                    sbMgr.TEST_SUITE_ID = mgr.TEST_SUITE_ID;
                }

                Logger.Info("SaveStoryboardRows", "===\n" + action + 
                                           " STORYBOARD_DETAIL_ID=" + sbMgr.STORYBOARD_DETAIL_ID +
                                           " STORYBOARD_ID=" + sbMgr.STORYBOARD_ID + 
                                           " RUN_ORDER=" + sbMgr.RUN_ORDER +
                                           " PROJECT_ID=" + sbMgr.PROJECT_ID +
                                           " TEST_SUITE_ID=" + sbMgr.TEST_SUITE_ID +
                                           " TEST_CASE_ID=" + sbMgr.TEST_CASE_ID +

                                           " RUN_TYPE=" + sbMgr.RUN_TYPE +
                                           " DEPENDS_ON=" + sbMgr.DEPENDS_ON +
                                           " ALIAS_NAME=" + sbMgr.ALIAS_NAME +
                                           " TEST_CASE_ID=" + sbMgr.TEST_CASE_ID                                                      
                                           );
            }
           
        }

        public static string GetDynamicDataByStoryBoardInfoAndObjectName(string strDBIdx, long l_storyBoardDetailId, long? lDataSettingId, string strObjectName, int iBaseLineId, int iLoop,ref bool isRight)
        {
            Logger.Info("GetDynamicDataByStoryBoardInfoAndObjectName",string.Format("StoryboardId :[{0}] DatasettingId:[{1}] strObjectName:[{2}]", l_storyBoardDetailId,lDataSettingId, strObjectName));
            ///数据获取思路和原则
            /// 1，必须是相同的dataset
            /// 2，最近的一次数据
            /// 
#region Sql to get data
            //SELECT STB.STORYBOARD_NAME,RPT_STP.RETURN_VALUES,RPT_STP.INPUT_VALUE_SETTING,RPT_STP.TEST_REPORT_STEP_ID,RPT.TEST_MODE,RPT.LOOP_ID,STB.DATA_SUMMARY_ID,STB.DATA_SET_ALIAS_NAME
            //FROM V_STORYBOARD_TEST_FULLVISION STB,
            //     V_STORYBOARD_TEST_FULLVISION STB1,
            //    T_TEST_STEPS STP,
            //    --GET REPROT DATA
            //    T_TEST_REPORT_STEPS RPT_STP,
            //    --MAKE SURE THERE ARE BELONG TO THE SAME TEST STORYBOARD
            //    T_TEST_REPORT RPT,
            //    T_PROJ_TEST_RESULT HIST
            //WHERE STB.STORYBOARD_DETAIL_ID = 4440
            //AND STB.STORYBOARD_ID = STB1.STORYBOARD_ID
            //--AND STB.DATA_SUMMARY_ID = STB.DATA_SUMMARY_ID
            //AND STP.TEST_CASE_ID = STB1.TEST_CASE_ID --
            //AND RPT_STP.STEPS_ID = STP.STEPS_ID  --
            //AND RPT_STP.INPUT_VALUE_SETTING = 'FIND_TRADE_ID'
            //AND RPT.TEST_REPORT_ID = RPT_STP.TEST_REPORT_ID
            //AND HIST.HIST_ID = RPT.HIST_ID
            //AND HIST.STORYBOARD_DETAIL_ID = STB1.STORYBOARD_DETAIL_ID
            //ORDER BY RPT_STP.TEST_REPORT_STEP_ID DESC;
#endregion //Sql to get data
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(true,strDBIdx);
            var query = from STB in marsEntities.V_STORYBOARD_TEST_FULLVISION
                        from STB1 in marsEntities.V_STORYBOARD_TEST_FULLVISION
                        from STP in marsEntities.T_TEST_STEPS
                        from RPT_STP in marsEntities.T_TEST_REPORT_STEPS
                        from RPT in marsEntities.T_TEST_REPORT
                        from HIST in marsEntities.T_PROJ_TEST_RESULT
                        where STB.STORYBOARD_DETAIL_ID == l_storyBoardDetailId
                          && STB.STORYBOARD_ID == STB1.STORYBOARD_ID
                          && STP.TEST_CASE_ID == STB1.TEST_CASE_ID
                          && RPT_STP.STEPS_ID == STP.STEPS_ID  //
                          && RPT_STP.INPUT_VALUE_SETTING == strObjectName
                          && RPT.TEST_REPORT_ID == RPT_STP.TEST_REPORT_ID
                          && HIST.HIST_ID == RPT.HIST_ID
                          && HIST.STORYBOARD_DETAIL_ID == STB1.STORYBOARD_DETAIL_ID
                          && RPT.TEST_MODE == iBaseLineId
                          && RPT.LOOP_ID == iLoop
                        orderby RPT_STP.TEST_REPORT_STEP_ID descending
                        select new
                        {
                            StoryBoardName = STB.STORYBOARD_NAME,
                            RETURN_VALUES = RPT_STP.RETURN_VALUES,
                            INPUT_VALUE_SETTING = RPT_STP.INPUT_VALUE_SETTING,
                            TEST_REPORT_STEP_ID = RPT_STP.TEST_REPORT_STEP_ID,
                            TEST_MODE = RPT.TEST_MODE,
                            LOOP_ID = RPT.LOOP_ID
                        };

            var objFirst = query.FirstOrDefault();
            if (objFirst == null)
            {
                Logger.Error("GetDynamicDataByStoryBoardInfoAndObjectName", "Can't get data from DB.");
                isRight = false;
                return "";
            }
            isRight = true;
            return objFirst.RETURN_VALUES??"";

        }

        public void SaveStoryboardRowDataSettingsByInst(string strDBIdx, List<B_STORYBOARD_DATASET_SETTING> storyBoardRowDataSettingList, MarsEntities objDbCntx)
        {
            MarsEntities dbCntx = objDbCntx ?? (localMarsEntites ?? (localMarsEntites = GetMarsEntitiesInstance(strCurrentDB:strDBIdx)));    
            SaveStoryboardRowDataSettings(strDBIdx,storyBoardRowDataSettingList, dbCntx);
        }

        public static void SaveStoryboardRowDataSettings(string strDBIdx, 
            List<B_STORYBOARD_DATASET_SETTING> storyBoardRowDataSettingList, MarsEntities dbCntx=null)
        {
            MarsEntities marsEntities = dbCntx==null?BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx):dbCntx;

            foreach (B_STORYBOARD_DATASET_SETTING row in storyBoardRowDataSettingList)
            {
                if(row.SETTING_ID == -1)
                {
                     T_STORYBOARD_DATASET_SETTING data = (from d in marsEntities.T_STORYBOARD_DATASET_SETTING
                                                          where d.STORYBOARD_DETAIL_ID == row.STORYBOARD_DETAIL_ID
                                                          select d ).FirstOrDefault();
                    if (data == null) continue;
                    data.DATA_SUMMARY_ID = row.DATA_SUMMARY_ID;
                }
                else
                {
                    T_STORYBOARD_DATASET_SETTING data = T_STORYBOARD_DATASET_SETTINGAssembler.ToEntity((T_STORYBOARD_DATASET_SETTINGDTO)row);
                    marsEntities.T_STORYBOARD_DATASET_SETTING.Add(data);
                }
            }
        }

        private static Dictionary<string, List<SYSTEM_LOOKUP>> systemLookUpCache = new Dictionary<string, List<SYSTEM_LOOKUP>>();

        
        public static string GetRunTypeStringFromSystemLookup(string strDBIdx, short iValue)
        {
            if ((systemLookUpCache==null)||(systemLookUpCache["T_PROJ_TC_MGR"]==null))
            {
                InitializeSystemLookupCache(strDBIdx, "T_PROJ_TC_MGR", "RUN_TYPE");
            }
            if (systemLookUpCache["T_PROJ_TC_MGR"] == null) return "N/A";

            var s = systemLookUpCache["T_PROJ_TC_MGR"].Where(p => p.VALUE == iValue).FirstOrDefault();
            if (s == null) return "N/A";
            return s.DISPLAY_NAME;
        }

        private static void InitializeSystemLookupCache(string strDBIdx, string strTableName, string fieldName, MarsEntities objDbCntx =null)
        {
            Logger.logBegin("InitializeSystemLookupCache", string.Format("Table Name:[{0}] FieldName:[{1}]", strTableName, fieldName));
            MarsEntities db = objDbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var c = from d in db.SYSTEM_LOOKUP
                    where d.TABLE_NAME == strTableName
                    && d.FIELD_NAME == fieldName
                    select d;
            if (c.FirstOrDefault()!=null)
            {
                systemLookUpCache.Add(strTableName, c.ToList());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="dispalyName"></param>
        /// <param name="dbCntx"></param>
        /// <param name="isGetDB">给代码重用用。无须获取数据库连接，以免破坏connection等的直接使用</param>
        /// <returns></returns>
        public static short? GetSystemLookupValue(string strDBIdx, string tableName, string fieldName, string dispalyName,MarsEntities dbCntx=null, bool isGetDB=true)
        {
            MarsEntities marsEntities = dbCntx;
            if (isGetDB)
                marsEntities = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            bool isGetFromCache = false, isReget = false;
            if ((string.Compare(tableName, "T_PROJ_TC_MGR",true )==0)&&(string.Compare(fieldName, "RUN_TYPE",true)==0))
            {
                isGetFromCache = true;                
            }

            if(isGetFromCache)
            {
                if (!systemLookUpCache.Keys.Contains(tableName)) isReget = true;
                if (isReget)
                {
                    var d = from c in marsEntities.SYSTEM_LOOKUP
                            where c.TABLE_NAME == tableName &&
                                    c.FIELD_NAME == fieldName 
                            select c;
                    if (d.FirstOrDefault() == null) return null;
                    systemLookUpCache.Add(tableName, d.ToList());
                }

                var o = systemLookUpCache[tableName].FirstOrDefault(p => (string.Compare(p.FIELD_NAME, fieldName, true) == 0) && (string.Compare(p.DISPLAY_NAME, dispalyName, true) == 0));
                if (o == null) return null;
                return o.VALUE;

            }

            var data = (from c in marsEntities.SYSTEM_LOOKUP
                        where c.TABLE_NAME == tableName && 
                                c.FIELD_NAME == fieldName && 
                                c.DISPLAY_NAME == dispalyName
                        select c.VALUE).FirstOrDefault();

            return data;
        }

        public static bool CleanDuplicatedObjects(string strDBIdx, ref int iCnt, ref string strError)
        {
            DbCommand dbCmmd = null;
            DbTransaction dbTrans = null;
            try
            {
                MarsEntities dbCntx = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                DbConnection dbConnection = null;
                if ((dbConnection=dbCntx.Database.Connection).State!= ConnectionState.Open)
                {
                    dbConnection.Open();
                }
                dbTrans = dbConnection.BeginTransaction();
                dbCmmd = dbConnection.CreateCommand();

                bool isOk = true;
                int iTmpCnt = 0;
#region clean duplidatec Name_id table records
                List<T_OBJECT_NAMEINFODTO> lstDuplicatedObjNames= B_OBJECT_NAMEINFO.GetDuplicatedObjectNames(dbCmmd, ref isOk, ref strError);
                if (!isOk)
                {
                    dbTrans.Rollback();
                    return false;
                }
                List<long> NameIdsToBeDelete = new List<long>();
                if (lstDuplicatedObjNames.Count > 0) { 
                
                    Dictionary<string,List<T_OBJECT_NAMEINFODTO>>  dicDuplictedName = lstDuplicatedObjNames.GroupBy(p => p.OBJECT_HAPPY_NAME)
                        .ToDictionary(p => p.Key, r => r.OrderBy(x => x.OBJECT_NAME_ID)
                        .ToList());
                    foreach (var k in dicDuplictedName.Keys)
                    {
                        List<T_OBJECT_NAMEINFODTO> lstObjNames = dicDuplictedName[k];
                        if (dicDuplictedName[k] == null) continue;
                        if (dicDuplictedName[k].Count == 0) continue;
                        long minId = dicDuplictedName[k][0].OBJECT_NAME_ID;
                        List<long> toUpDate = dicDuplictedName[k].Where(p => p.OBJECT_NAME_ID != minId).Select(p => p.OBJECT_NAME_ID).ToList();

                        isOk = B_REGISTED_OBJECT.UpdateNameIdToSpecByCnn(dbConnection, minId, toUpDate,ref strError);
                        if (!isOk)
                        {
                            dbTrans.Rollback();
                            return false;
                        }

                        iTmpCnt = B_TEST_STEPS.UpdateNameIdToSpecByCnn(dbConnection, minId, toUpDate,ref isOk, ref strError);
                        if (!isOk)
                        {
                            dbTrans.Rollback();
                            return false;
                        }
                        iCnt += iTmpCnt;

                        NameIdsToBeDelete.AddRange(toUpDate);
                    }
                    if ((NameIdsToBeDelete != null) && (NameIdsToBeDelete.Count > 0))
                    {
                        isOk = B_OBJECT_NAMEINFO.DeleteNameObjectsViaList(dbConnection, NameIdsToBeDelete, ref strError);
                        if (!isOk)
                        {
                            dbTrans.Rollback();
                            return false;
                        }
                    }
                }
#endregion

#region clean duplicated object records
                List<T_REGISTED_OBJECTDTO> lstDuplicatedObjects = B_REGISTED_OBJECT.GetDuplicatedAppObjects(dbConnection, ref isOk, ref strError);
                if (!isOk)
                {
                    dbTrans.Rollback();
                    return false;
                }
                if ((lstDuplicatedObjects != null) && (lstDuplicatedObjects.Count > 0))
                {
                    var dicInfo = lstDuplicatedObjects.GroupBy(p => new { p.OBJECT_NAME_ID, p.OBJECT_TYPE, p.APPLICATION_ID })
                                    .ToDictionary(p => p.Key, x => x.ToList());
                    foreach (var k in dicInfo.Keys)
                    {
                        if (k == null) continue;
                        long minObjId = dicInfo[k].Min(p=>p.OBJECT_ID);
                        List<long> lstToUpdate = dicInfo[k].Where(p => p.OBJECT_ID != minObjId)
                                                    .Select(p => p.OBJECT_ID)
                                                    .ToList();
                        if (!B_TEST_STEPS.UpdatObjIdToSpecByCnn(dbConnection, minObjId, lstToUpdate, ref strError, ref iTmpCnt))
                        {
                            dbTrans.Rollback();
                            return false;
                        }

                        iTmpCnt = B_REGISTED_OBJECT.DeleteObjectById(dbConnection, lstToUpdate, ref strError, ref isOk );
                        if (!isOk)
                        {
                            dbTrans.Rollback();
                            return false;
                        }
                        iCnt += iTmpCnt;
                    }
                }
#endregion
                ///find all duplicated objects nameinfo
                ///
                //isOk = B_REGISTED_OBJECT.DeleteAllDuplicatedObjectsWithoutUsed(dbCmmd,ref iCnt, ref strError);
                
                if (!isOk)
                {
                    dbTrans.Rollback();
                }
                else
                {
                    dbTrans.Commit();

                    //创建object Name 的唯一索引
                    isOk = B_OBJECT_NAMEINFO.CreateUniqueIndex(dbConnection, ref strError);

                    if (!B_REGISTED_OBJECT.RecompareObjectsMV(dbConnection, ref strError));
                }   

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CleanDuplicatedObjects",strError = string.Format("Exception:[{0}]\r\n{1}",e.Message,e.StackTrace),e);
                try
                {
                    if (dbTrans != null)
                        dbTrans.Rollback();                    
                }
                catch (Exception)
                {

                }
                
                return false;
            }
        }

        public static List<T_REGISTERED_APPSDTO> GetApplicationById(string strDBIdx,long lAppID)
        {
            Logger.logBegin("GetApplicationById");

            T_REGISTERED_APPSDTO objAppDto = (new B_REGISTERED_APPS()).GetApplicationById(lAppID, strDBIdx);
            List<T_REGISTERED_APPSDTO> lstResult = new List<T_REGISTERED_APPSDTO>();

            if (objAppDto != null)
                lstResult.Add(objAppDto);
            Logger.Info("GetApplicationById", string.Format("Try to get one Item from DB by ID:[{0}] and count :[{1}] returned", lAppID, lstResult.Count));
            Logger.logEnd("GetApplicationById");
            return lstResult;
        }

        public static int DeleteStoryboardAndDependents(string strDBIdx, long? storyboardId, ref string strError)
        {
            MarsEntities marsEntities =BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            
            var storyBoardSummary = (from s in marsEntities.T_STORYBOARD_SUMMARY
                                     where s.STORYBOARD_ID == storyboardId
                                     select s).FirstOrDefault();
            if (storyBoardSummary == null)
                return 0;

            marsEntities.T_STORYBOARD_SUMMARY.Remove(storyBoardSummary);

            var storyboardDetails = (from s in marsEntities.T_PROJ_TC_MGR
                                     where s.STORYBOARD_ID == storyboardId
                                     select s);

            foreach (var storyboardDetail in storyboardDetails)
            {
                
                var storyboardDetailId = storyboardDetail.STORYBOARD_DETAIL_ID;
                marsEntities.T_PROJ_TC_MGR.Remove(storyboardDetail);
                var storyboardDataSetting = (from s in marsEntities.T_STORYBOARD_DATASET_SETTING
                                             where s.STORYBOARD_DETAIL_ID == storyboardDetailId
                                             select s).FirstOrDefault();

                if (storyboardDataSetting != null)
                {
                    marsEntities.T_STORYBOARD_DATASET_SETTING.Remove(storyboardDataSetting);
                }
            }

            try
            {
                return marsEntities.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Error("DeleteStoryboardAndDependents",strError = string.Format("Exceptions:[{0}]", e.Message),e);
                return -1;
            }finally
            {
                Logger.logEnd("DeleteStoryboardAndDependents");
            }

            //return SaveChanges();
        }

        public static int SaveObjectPool(string strDBIdx, List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList,MarsEntities dbCntx=null)
        {
            MarsEntities marsEntities = dbCntx??BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            T_SHARED_OBJECT_POOL tSharedObjectPool;
            foreach (B_SHARED_OBJECT_POOL bTestDataSetting in bSharedObjectPoolList)
            {
                tSharedObjectPool = (from t in marsEntities.T_SHARED_OBJECT_POOL
                                     where t.OBJECT_NAME == bTestDataSetting.OBJECT_NAME &&
                                     t.OBJECT_ORDER == bTestDataSetting.OBJECT_ORDER &&
                                     t.LOOP_ID == bTestDataSetting.LOOP_ID &&
                                     t.DATA_SUMMARY_ID == bTestDataSetting.DATA_SUMMARY_ID 

                                   select t).FirstOrDefault();

                if (tSharedObjectPool == null)
                {
                    tSharedObjectPool = T_SHARED_OBJECT_POOLAssembler.ToEntity((T_SHARED_OBJECT_POOLDTO)bTestDataSetting);
                    marsEntities.T_SHARED_OBJECT_POOL.Add(tSharedObjectPool);
                }
                else
                    tSharedObjectPool.DATA_VALUE = bTestDataSetting.DATA_VALUE;
            }
            if (dbCntx == null)
                return SaveChanges(strDBIdx);
            else
                return 1;
        }

        
        public static T_SHARED_OBJECT_POOL LoadSharedObjectPool(string strDBIdx, long stepId, long dataSheetId, string objectName, int loopId, int objectOrder)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            var sharedObjectPool = (from x in marsEntities.T_SHARED_OBJECT_POOL
                                            where x.DATA_SUMMARY_ID == dataSheetId && 
                                                  x.OBJECT_NAME == objectName &&
                                                  x.LOOP_ID == loopId &&
                                                  x.OBJECT_ORDER == objectOrder
                                            select x).FirstOrDefault();

            return sharedObjectPool;
        }

        public static void DeleteObjectPool(string strDBIdx, List<long> poolIdList)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            foreach (var poolId in poolIdList)
            {
                var pool = (from p in marsEntities.T_SHARED_OBJECT_POOL
                            where p.OBJECT_POOL_ID == poolId
                            select p).FirstOrDefault();

                if (pool != null)
                    marsEntities.T_SHARED_OBJECT_POOL.Remove(pool);
            }
        }

        public static string GetObjectNameById(string strDBIdx, long objectId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            string name = (from x in marsEntities.V_OBJECT_SNAPSHOT
                           where x.OBJECT_ID == objectId
                           select x).FirstOrDefault().OBJECT_HAPPY_NAME;
#else
            string name = (from x in marsEntities.T_REGISTED_OBJECT
                           where x.OBJECT_ID == objectId
                           select x).FirstOrDefault().OBJECT_HAPPY_NAME;
#endif
            return name;
        }



        public static string GetKeywordNameById(string strDBIdx, long keywordId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            string name = (from x in marsEntities.T_KEYWORD
                           where x.KEY_WORD_ID == keywordId
                           select x).FirstOrDefault().KEY_WORD_NAME;
            return name;
        }



        
        public static List<T_REGISTERED_APPSDTO> GetAppNames(string strDBIdx, long testCaseId)
        {
            string apps = "";
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

            var relatedApps = from a in marsEntities.T_REGISTERED_APPS
                              join t in marsEntities.REL_APP_TESTCASE on a.APPLICATION_ID equals t.APPLICATION_ID
                              where t.TEST_CASE_ID == testCaseId
                              select a;

            //foreach (var s in relatedApps)
             //   apps += s + ",";

            //apps = string.Join(",", relatedApps);
            
            return relatedApps.ToDTOs();
        }

        public static List<long> GetAppIds(string strDBIdx, long testCaseId)
        {
            List<long> appIdList = new List<long>();
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

            var qry = from a in marsEntities.T_REGISTERED_APPS
                              join t in marsEntities.REL_APP_TESTCASE on a.APPLICATION_ID equals t.APPLICATION_ID
                              where t.TEST_CASE_ID == testCaseId
                              orderby a.APPLICATION_ID
                              select a.APPLICATION_ID;

            appIdList = qry.ToList();

            return appIdList;
        }

        public static List<B_REGISTERED_APPS> GetAllApps(string strDBIdx, List<long> lstAppIds)
        {
            ObservableCollection<B_REGISTERED_APPS> lstApps = B_REGISTERED_APPS.GetCacheApps(strDBIdx);
            return lstApps.Where(p => lstAppIds.Contains(p.APPLICATION_ID)).ToList();
        }
        public static Dictionary<string, long> GetAllApps(string strDBIdx)
        {
           
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            Dictionary<string, long> appsDict = new Dictionary<string, long>();

            var qry = from a in marsEntities.T_REGISTERED_APPS
                      select a;

            foreach (var a in qry)
            {
                appsDict.Add(a.APP_SHORT_NAME, a.APPLICATION_ID);
            }

            return appsDict;
        }

        public static bool isProjectNameExist(string strDBIdx, string name)
        {
            bool rc = false;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities.T_TEST_PROJECT.Any(x => x.PROJECT_NAME.Equals(name)))
                rc = true;
            return rc;
        }

        public static bool isTestSuiteNameExist(string strDBIdx, string name)
        {
            bool rc = false;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities.T_TEST_SUITE.Any(x => x.TEST_SUITE_NAME.Equals(name)))
                rc = true;
            return rc;
        }

        public static bool isTestCaseNameExist(string strDBIdx, string name)
        {
            bool rc = false;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities.T_TEST_CASE_SUMMARY.Any(x => x.TEST_CASE_NAME.Equals(name)))
                rc = true;
            return rc;
        }

        public static bool isDataSetNameExist(string strDBIdx,string name, long id)
        {
            bool rc = false;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities.T_TEST_DATA_SUMMARY.Any(x => x.ALIAS_NAME.Equals(name) && x.DATA_SUMMARY_ID != id))
                rc = true;
            return rc;
        }

        public static bool isStoryboardNameExist(string strDBIdx, string name, long projectId)
        {
            bool rc = false;
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            if (marsEntities.T_STORYBOARD_SUMMARY.Any(x => x.STORYBOARD_NAME.Equals(name) && x.ASSIGNED_PROJECT_ID == projectId))
                rc = true;
            return rc;
        }  

        public static void UpdateProjectName(string strDBIdx, long id,  string name)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities.T_TEST_PROJECT.Any(e => e.PROJECT_ID == id))
            {
                T_TEST_PROJECT project = marsEntities.T_TEST_PROJECT.FirstOrDefault(e => e.PROJECT_ID == id);
                project.PROJECT_NAME = name;
                marsEntities.SaveChanges();
            }
        }

        public static void UpdateTestSuiteName(string strDBIdx, long id, string name)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities.T_TEST_SUITE.Any(e => e.TEST_SUITE_ID == id))
            {
                T_TEST_SUITE testSuite = marsEntities.T_TEST_SUITE.FirstOrDefault(e => e.TEST_SUITE_ID == id);
                testSuite.TEST_SUITE_NAME = name;
                marsEntities.SaveChanges();
            }
        }

        public static void UpdateTestCaseName(string strDBIdx, long id, string name)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            if (marsEntities.T_TEST_CASE_SUMMARY.Any(e => e.TEST_CASE_ID == id))
            {
                T_TEST_CASE_SUMMARY testCase = marsEntities.T_TEST_CASE_SUMMARY.FirstOrDefault(e => e.TEST_CASE_ID == id);
                testCase.TEST_CASE_NAME = name;
                marsEntities.SaveChanges();
            }
        }

        public static bool UpdateDataSetName(string strDBIdx, long id, string name, string description,ref string strError)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            try
            {
                if (marsEntities.T_TEST_DATA_SUMMARY.Any(e => e.DATA_SUMMARY_ID == id))
                {
                    T_TEST_DATA_SUMMARY dataSet = marsEntities.T_TEST_DATA_SUMMARY.FirstOrDefault(e => e.DATA_SUMMARY_ID == id);
                    dataSet.ALIAS_NAME = name;
                    dataSet.DESCRIPTION_INFO = description;
                    
                    marsEntities.SaveChanges();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateDataSetName", strError = string.Format("Exceptions when update Dataset,\r\n[{0}]",e.Message),e);
                return false;
            }
            
        }

        public static void UpdateStoryboardName(string strDBIdx, long id, string name)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            if (marsEntities.T_STORYBOARD_SUMMARY.Any(e => e.STORYBOARD_ID == id))
            {
                T_STORYBOARD_SUMMARY storyboard = marsEntities.T_STORYBOARD_SUMMARY.FirstOrDefault(e => e.STORYBOARD_ID == id);
                storyboard.STORYBOARD_NAME = name;
                marsEntities.SaveChanges();
            }
        }

#if !Mars_Sqlserver
        public static List<T_OBJECT_CHILDDTO> GetObjectChildList(string strDBIdx,long objectId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<T_OBJECT_CHILDDTO> dtoList = new List<T_OBJECT_CHILDDTO>();

            List<T_OBJECT_CHILD> entList = (from o in marsEntities.T_OBJECT_CHILD
                                            where o.OBJECT_ID == objectId
                                            select o).ToList();


            dtoList = T_OBJECT_CHILDAssembler.ToDTOs(entList);

            return dtoList;
        }
#endif

        public static List<T_DATA_SOURCE> GetDataSourceData(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            List<T_DATA_SOURCE> list = (from o in marsEntities.T_DATA_SOURCE
                                        orderby o.DATA_SOURCE_NAME
                                        select o).ToList();

            return list;
        }

        public static void UpdateDataSource(string strDBIdx, string id, string data, short dataType,MarsEntities dbCntx=null)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            var dataSource = (from o in marsEntities.T_DATA_SOURCE
                              where o.DATA_SOURCE_NAME == id 
                              && o.DATA_SOURCE_TYPE == dataType
                                        select o).FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = new T_DATA_SOURCE();
                dataSource.DATA_SOURCE_NAME = id;
                dataSource.DATA_SOURCE_ID = GetTestStepsId(dbCntx);
                dataSource.DATA_SOURCE_TYPE = dataType;
                marsEntities.T_DATA_SOURCE.Add(dataSource);
            }
            dataSource.DETAILS = data;
            marsEntities.SaveChanges();
        }
        public static void DeleteDataSource(string strDBIdx, string id, short dataType)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            var dataSource = (from o in marsEntities.T_DATA_SOURCE
                              where o.DATA_SOURCE_NAME == id
                              && o.DATA_SOURCE_TYPE == dataType
                              select o).FirstOrDefault();
            if (dataSource != null)
            {  
                marsEntities.T_DATA_SOURCE.Remove(dataSource);
                marsEntities.SaveChanges();
            }
        }
        public static string GetVariableValue(string strDBIdx, string variableLocalOrGlobal, string variableName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            var data = (from c in marsEntities.SYSTEM_LOOKUP
                        where c.TABLE_NAME == variableLocalOrGlobal &&
                                c.FIELD_NAME == variableName 
                           select c.DISPLAY_NAME).First();
            return data;
        }
#if v_useNameId

        public static string GetObjectNameByNameId(string strDBIdx, long lObjNameId, MarsEntities objDBCntx = null)
        {
            MarsEntities marsEntities = objDBCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx) : objDBCntx;
            var query = from x in marsEntities.T_OBJECT_NAMEINFO
                        where x.OBJECT_NAME_ID == lObjNameId
                        select x;
            var objNameInfo = query.FirstOrDefault();
            if (objNameInfo == null) return "";
            return objNameInfo.OBJECT_HAPPY_NAME;
        }

        /*
         * SELECT * FROM T_OBJECT_NAMEINFO O,
T_REGISTED_OBJECT RO,
T_REGISTERED_APPS RA
WHERE
 O.OBJECT_HAPPY_NAME='SWAP_TRADE' 
AND RO.OBJECT_NAME_ID=O.OBJECT_NAME_ID 
AND RO.APPLICATION_ID=RA.APPLICATION_ID
AND RA.application_id = 1
         * 
         */

        public static long GetObjectNameIdByNameAndAppId(string strDBIdx, string objectName, long appId)
        {
            long id = -1;

            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(true, strCurrentDB: strDBIdx);

            var query = from o in marsEntities.T_OBJECT_NAMEINFO
                        from ro in marsEntities.T_REGISTED_OBJECT
                        where o.OBJECT_HAPPY_NAME.Equals(objectName)
                           && ro.OBJECT_NAME_ID == o.OBJECT_NAME_ID
                           && ro.APPLICATION_ID == appId
                        select o;
            var obj = query.FirstOrDefault();

            if (obj != null)
                id = obj.OBJECT_NAME_ID;

            return id;
        }

#endif

        public static bool DeleteApplicationById(string strDBIdx, long lApplicationId, ref string strError)
        {
            Logger.Info("DeleteApplicationById",string.Format("Try to delete Applicatoin by Id:[{0}]", lApplicationId));
            try
            {
                
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(true, strCurrentDB: strDBIdx);
                T_REGISTERED_APPS appsToBeDeleted = marsEntities.T_REGISTERED_APPS.Where(p => p.APPLICATION_ID == lApplicationId).FirstOrDefault();
                if (appsToBeDeleted == null)
                {
                    strError = string.Format("No such application with Id [{0}]", lApplicationId);
                    return false;
                }
                marsEntities.T_REGISTERED_APPS.Remove(appsToBeDeleted);
                marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteApplicationById",strError = string.Format("Exception [{0}] when delete application:[{1}]",e.Message,lApplicationId),e);
                return false;
            }
            
        }

        public static T_TESTER_INFODTO VerifyUserLogin(string strUserName, string strEncodedPassword,
            string strDBIdx, 
            ref string strError)
        {
            Logger.Info("VerifyUserLogin", string.Format("UserName:[{0}] Password:[{1}], dbIdx:[{2}]", strUserName, strEncodedPassword,strDBIdx));
            //try
            //{
                MarsEntities marsEntity = GetMarsEntitiesInstance(true, strDBIdx);
            var userInfox = (from u in marsEntity.T_TESTER_INFO
                             where u.TESTER_LOGIN_NAME == strUserName
                             && u.TESTER_PWD == strEncodedPassword
                             select u);
            Logger.Info("VerifyUserLogin",userInfox.ToString());
            var userInfo = userInfox.FirstOrDefault();
                if (userInfo==null)
                {
                    strError = string.Format("Can't find such user [{0}]", strUserName);
                    return null;
                }
                return T_TESTER_INFOAssembler.ToDTO(userInfo);
            //}
            //catch (Exception e)
            //{
            //    Logger.Error("VerifyUserLogin",strError = string.Format("Exception:[{0}]",e.Message),e);
            //    return null;
            //}
        }

        public static DbConnection GetDBConnectionFromEntityFramework(string strDBIdx, ref string strError, ref bool isOk)
        {
            MarsEntities dbCntx = GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            try
            {
                if (dbCntx.Database.Connection.State != ConnectionState.Open)
                    dbCntx.Database.Connection.Open();
                isOk = true;
                return dbCntx.Database.Connection;
            }
            catch (Exception e)
            {
                Logger.Error("GetDBConnectionFromEntityFramework",strError=string.Format("Exeption:[{0}]", e.Message),e);
                isOk = false;
                return null;
            }
        }

        public static long GetBussinessSeq(string seqName, DbCommand dbCmmd, ref string strError, ref bool isOk)
        {
            string strSql = string.Format("SELECT {0}.NEXTVAL FROM DUAL", seqName);
            
            try
            {
                dbCmmd.Parameters.Clear();
                dbCmmd.CommandText = strSql;
                isOk = true;
                return long.Parse(dbCmmd.ExecuteScalar().ToString());
            }
            catch (Exception e)
            {
                Logger.Error("GetBussinessSeq", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                isOk = false;
                return -1;
            }

        }

        public static List<KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>> LoadTestStepAndItsDataByTCIdAndDtaId(string strDBIdx, long testCaseId, 
            long dataSheetId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("LoadTestStepAndItsDataByTCIdAndDtaId",string.Format("tcid:[{0}] data sheetid:[{1}]", testCaseId, dataSheetId));
            try
            {
                MarsEntities dbCntx = GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var rsltOrg = (from q in dbCntx.T_TEST_STEPS
                            where q.TEST_CASE_ID == testCaseId
                            join d in dbCntx.TEST_DATA_SETTING.Where(o => o.DATA_SUMMARY_ID == dataSheetId) on q.STEPS_ID equals d.STEPS_ID into qd
                            from x in qd.DefaultIfEmpty()
                            select new { k = q, v = qd})
                            .OrderBy(p=>p.k.RUN_ORDER)                                                        
                            ;
                Logger.Info("LoadTestStepAndItsDataByTCIdAndDtaId", rsltOrg.ToString());
                var grp = rsltOrg
                    .GroupBy(p => p.k)
                    .ToDictionary(p => p.Key, z => z.ToList());
                Logger.Info("LoadTestStepAndItsDataByTCIdAndDtaId", "Query finished");
                List<KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>> rslt = new List<KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>>();
                foreach (var k in grp.Keys)
                {
                    if (k == null) continue;
                    T_TEST_STEPSDTO tmpStp = k.ToDTO();
                    
                    var tmpLst = grp[k].FirstOrDefault();
                    TEST_DATA_SETTING tmpData = grp[k] == null ? null : (tmpLst==null?null:tmpLst.v.OrderByDescending(x=>x.DATA_SETTING_ID).FirstOrDefault());

                    //for debugging
                    //if ((tmpStp != null) && ((tmpStp.RUN_ORDER >= 32) && (tmpStp.RUN_ORDER <= 34)))
                    //    Logger.Info("LoadTestStepAndItsDataByTCIdAndDtaId", string.Format("step id:[{0}] object id:[{1}] name id:[{2}],\r\n\t{3}",
                    //        tmpStp.STEPS_ID, tmpStp.OBJECT_ID, tmpStp.OBJECT_NAME_ID, tmpData == null ? null:(tmpData.DATA_VALUE+", "+tmpData.DATA_SETTING_ID)));

                    rslt.Add(new KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>(tmpStp, tmpData == null ? null : tmpData.ToDTO()));
                }
                isOk = true;
                return rslt.OrderBy(p=>p.Key.RUN_ORDER).ToList();
                /*
                var rslt = rsltg.OrderBy(p=>p.v.)    
                List<KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>> lstRslt = new List<KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>>();
                for (int i=0; i<(rslt==null?-1:rslt.Count);i++)
                {
                    if (rslt[i].k == null) continue;
                    lstRslt.Add(new KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>(rslt[i].k.ToDTO(), rslt[i].v == null ? null : rslt[i].v.ToDTO()));
                }
                isOk = true ;        
                return lstRslt;
                */
            }
            catch (Exception e)
            {
                Logger.Error("LoadTestStepAndItsDataByTCIdAndDtaId",strError = string.Format("Exception:[{0}]",e .Message),e);
                return null;
            }
        }
    }



}
