using Mars.message.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.message.DataLayer.multipleDBSupport;
using Mars.Properties;
//using Mars.message.Properties;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mars.message.DatasourceMgr
{
    public class DatasourceManament
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(DatasourceManament));
        public MarsEngineDBSourceRoot PharseDbsource(string qUERY_DESC, ref bool isOk, ref string strError)
        {
            Logger.logBegin("PharseDbsource");
            try
            {
                // webDataSourceReturnedData.DataSourceInfo.QUERY_DESC is not null, which contains MarsEngineDBSource
                // convert QUERY_DESC into regular object
                System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(MarsEngineDBSourceRoot));
                MarsEngineDBSourceRoot currentQueryInfo = null;
                using (StringReader reader = new StringReader(qUERY_DESC))
                {
                    currentQueryInfo = (MarsEngineDBSourceRoot)xmlSerializer.Deserialize(reader);
                }
                if (currentQueryInfo == null)
                {
                    strError = "Code 04B:Please make sure that the datasource info is right.";
                    isOk = false;
                    Logger.Error("GetQueryByQueryName", strError);

                    return null;
                }
                return currentQueryInfo;
            }
            finally
            {
                Logger.logEnd("PharseDbsource", $"returns|{isOk}|");
            }
        }

        public bool IsDatasourceQueryMatchToPara(MarsEngineDBSourceRoot dbsourceData,
            DBQueryDataSettingRoot queryParaSettingInfo, ref string strError)
        {
            if (dbsourceData == null)
            {
                Logger.Error("IsDatasourceQueryMatchToPara", strError = DataLayerResources.datalayer_no_datasource);
                return false;
            }
            if (dbsourceData.ParaMeters == null) return true;// no para is set
            foreach(var itm in dbsourceData.ParaMeters.ParaMeter){
                if (itm == null) continue;
                if (!queryParaSettingInfo.isParaMeterNameExists(itm.PmName))
                {
                    strError = string.Format(DataLayerResources.datalayer_no_parameter_data_is_set, itm.PmName);
                    return false;
                }
            }
            return true;
        }

        public DataTable QueryDataBasedonDataSource(MarsEngineDBSourceRoot dbsourceData,
            DBQueryDataSettingRoot queryParaSettingInfo,
            string strConn,
            ref bool isOk, ref string strError)
        {
            MarsDBConnectionFactory dbFactory = null;
            try
            {
                Logger.logBegin("QueryDataBasedonDataSource");
                if (dbsourceData == null)
                {
                    strError = "Code 04C:No valiated DB source Data is passed.";
                    isOk = false;
                    return null;
                }
                /// 1, get cnn based on type
                /// 2, get command
                /// 3, get combine para
                /// 4, query data
                /// 
                dbFactory = new MarsDBConnectionFactory(dbsourceData.DBType, strConn);
                if (!dbFactory.InitDBConnection())
                {
                    strError = "Code 04E:Make sure the database type and connection string are right.";
                    Logger.Error("QueryDataBasedonDataSource", strError);
                    isOk = false;
                    return null;
                }
                if (!IsDatasourceQueryMatchToPara(dbsourceData, queryParaSettingInfo,ref strError))
                {
                    strError = "Code 04F:Make sure that parameter settings from Data column match Datasource settings.";
                    Logger.Error("QueryDataBasedonDataSource", strError);
                    isOk =false;
                    return null;
                }
                var conn = dbFactory.GetConnection();
                IDbCommand dbCmd = dbFactory.CreateCommand(dbsourceData.Sql, conn);
                // add para 
                dbCmd =dbFactory.addParas(dbCmd, dbsourceData.ParaMeters, queryParaSettingInfo.para, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("QueryDataBasedonDataSource", strError);
                    return null;
                }
                DataTable dt = dbFactory.readDataToDataTable(dbCmd, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("QueryDataBasedonDataSource", strError);
                    strError = DataLayerResources.datalayer_cant_read_data;
                    return null;
                }
                // check whether data has the target columns and remove other columns
                dt = dbFactory.filterColumnsFromDatatable(dbsourceData.ResultSetFields, dt, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("QueryDataBasedonDataSource", strError);
                    strError = DataLayerResources.datalayer_cant_read_data;
                    return null;
                }
                return dt;
            }
            catch(Exception e)
            {
                Logger.Error("QueryDataBasedonDataSource", e.Message, e);
                strError = DataLayerResources.datalayer_cant_dealwith_datasrouce;
                return null;
            }
            finally
            {
                if (dbFactory != null)
                {
                    dbFactory.closeConns();
                }
                Logger.logEnd("QueryDataBasedonDataSource", $"returns|{isOk}|");
            }
            
        }
    }
}
