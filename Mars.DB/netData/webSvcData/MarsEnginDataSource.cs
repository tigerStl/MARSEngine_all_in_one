using Mars.message.Business;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Mars.message.AutoTestingDriver.ExecuteTestcase.keywordOp
{
    internal class MarsEnginDataSource
    {
    }

    // using System.Xml.Serialization;
    // XmlSerializer serializer = new XmlSerializer(typeof(MarsEngineDBSource));
    // using (StringReader reader = new StringReader(xml))
    // {
    //    var test = (MarsEngineDBSource)serializer.Deserialize(reader);
    // }
    #region sample of xml
    /// <summary>
    /// sample DB source file
    /// <MarsEngineDBSource>
    //   <ver>1.0</ver>
    //   <DBType>Oracle</DBType>
    //   <sql>SELECT ExtTradeId, Location, Id, OriginalId, OwnerTable, dmPOSTINGEVENT.TradeId, SeqNum, AssetID, Event, Amount, Ccy, PostingDate, ValueDate, BookingDate, Description, AccountID, DrOrCr, PostingType, ExtractFlag, ExtractDate, ProcessFlag, SettleAcctType, SettleAcctID, AltOwnerTable, AltTradeId, AltVersion, Version, Beneficiary, CustRole, AcctType, OrigSettleCcy, OrigSettleAmt, ObjId, GLDate
    //      FROM dmPOSTINGEVENT, dmBACK, dmENV
    //      WHERE ExtTradeId LIKE '%TC%' 
    //	  AND dmPOSTINGEVENT.PostingDate=:PostingDate
    //      AND dmBACK.TradeId = dmPOSTINGEVENT.TradeId

    //      and dmPOSTINGEVENT.ownertable = dmBack.ownertable

    //      and trader = :TRADER
    //      and dmenv.tradestatus = 'VER'

    //      and dmenv.audit_current = 'Y'

    //      and dmenv.tradeid = dmback.tradeid

    //      and dmenv.dmownertable = dmback.

    //      ORDER BY Id desc
    //   </sql>
    //   <QueryType>DQL</QueryType>
    //   <ParaMeters>
    //	<ParaMeter>
    //		<pmName>TRADER</pmName>
    //		<!--can be string, date, int, double-->
    //		<pmType>String</pmType>
    //	</ParaMeter>
    //	<ParaMeter>
    //		<pmName>PostingDate</pmName>
    //		<!--can be string, date, int, double-->
    //		<pmType>DateString</pmType>
    //		<pmFormat>yyyymmdd</pmFormat>
    //	</ParaMeter>
    //   </ParaMeters>
    //   <ResultSetFields>
    //		<ResultSetField order = "1" FieldType= "string" FieldName= "ExtTradeId" >


    //        </ ResultSetField >

    //        < ResultSetField order= "2" FieldType= "string" FieldName= "Location" >


    //        </ ResultSetField >
    //   </ ResultSetFields >
    //   < DBConnection >


    //   </ DBConnection >
    //</ MarsEngineDBSource >


    /// </summary>
    /// 
    #endregion

    [XmlRoot(ElementName = "ParaMeter")]
    public class MarsQueryDBKeywordParaMeter
    {

        [XmlElement(ElementName = "pmName")]
        public string PmName { get; set; }

        [XmlElement(ElementName = "pmType")]
        public string PmType { get; set; }

        [XmlElement(ElementName = "pmFormat")]
        public string PmFormat { get; set; }
    }

    [XmlRoot(ElementName = "ParaMeters")]
    public class MarsQueryDBKeywordParaMeters
    {

        [XmlElement(ElementName = "ParaMeter")]
        public List<MarsQueryDBKeywordParaMeter> ParaMeter { get; set; }
    }

    [XmlRoot(ElementName = "ResultSetField")]
    public class MarsQueryDBKeywordResultSetField
    {

        [XmlAttribute(AttributeName = "order")]
        public int Order { get; set; }

        [XmlAttribute(AttributeName = "FieldType")]
        public string FieldType { get; set; }

        [XmlAttribute(AttributeName = "FieldName")]
        public string FieldName { get; set; }
    }

    [XmlRoot(ElementName = "ResultSetFields")]
    public class MarsQueryDBKeywordResultSetFields
    {

        [XmlElement(ElementName = "ResultSetField")]
        public List<MarsQueryDBKeywordResultSetField> ResultSetField { get; set; }
    }

    [XmlRoot(ElementName = "MarsEngineDBSource")]
    public class MarsEngineDBSourceRoot
    {

        [XmlElement(ElementName = "ver")]
        public double Ver { get; set; }

        [XmlElement(ElementName = "DBType")]
        public string DBType { get; set; }

        [XmlElement(ElementName = "sql")]
        public string Sql { get; set; }

        [XmlElement(ElementName = "QueryType")]
        public string QueryType { get; set; }

        [XmlElement(ElementName = "ParaMeters")]
        public MarsQueryDBKeywordParaMeters ParaMeters { get; set; }

        [XmlElement(ElementName = "ResultSetFields")]
        public MarsQueryDBKeywordResultSetFields ResultSetFields { get; set; }

        [XmlElement(ElementName = "DBConnection")]
        public object DBConnection { get; set; }

       
    }

    /// json data 
    ///  <summary>
    /// json data 
    /// {
    //  "para":[{
    //    "f":"PostingDate",
    //	"v":"20231201"

    //    },
    //	{
    //    "f":"TRADER",
    //	"v":"MARS"

    //    }
    //  ], 
    //  "DbConn":"MARSSQL",
    //"Export":{
    //  	"varType":"MARS_TABLE", 
    //	"varName":"QueryData"
    //  }
    //}
    /// </summary>
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class DBQueryDataSettingPara
    {
        public string f { get; set; }
        public string v { get; set; }
    }

    public class MarsDBQueryExport
    {
        public string varType { get; set; }
        public string varName { get; set; }
    }

    public class DBQueryDataSettingRoot
    {
        public List<DBQueryDataSettingPara> para { get; set; }
        public string DbConn { get; set; }
        public MarsDBQueryExport Export { get; set; }

        public bool isParaMeterNameExists(string strParaName)
        {
            if (string.IsNullOrEmpty(strParaName) && ((para == null) || (para.Count == 0))) return true;
            try
            {
                return para.Any(p => string.Compare(p.f, strParaName) == 0);
            }catch(Exception e)
            {
                return false;
            }
        }
    }

    public class DBQueryKeywordDataParaMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(DBQueryKeywordDataParaMgr));

        public DBQueryDataSettingRoot isDataSettingFromStepIsRight(string strData,ref bool isOk)
        {
            try
            {
                DBQueryDataSettingRoot rslt=  JsonSerializer.Deserialize<DBQueryDataSettingRoot>(strData);
                isOk = true;
                return rslt;
            }catch(Exception e)
            {
                isOk = false;
                return null;
            }
        }

        public DBQueryDataSettingRoot getDataFromTQuery(B_QUERY queryFromDB, ref bool isOk, ref string strError)
        {
            Logger.logBegin("getDataFromTQuery", queryFromDB==null?"null for queryFromDB":$"Datasource|{queryFromDB.QUERY_NAME}|{queryFromDB.QUERY_DESC}");
            
            
            return null;
        }
    }
}
