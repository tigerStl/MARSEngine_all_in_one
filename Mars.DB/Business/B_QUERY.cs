using Mars.message.DataLayer;
using MARS_Repository.BO;
using MARS_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Mars.message.Business
{
    [DataContract]
    public class B_QUERY
    {
        [DataMember]
        public long QUERY_ID { get; set; }
        [DataMember]
        public string QUERY_NAME { get; set; }
        [DataMember]
        public string QUERY_DESC { get; set; }
        [DataMember]
        public Nullable<short> IS_ACTIVE { get; set; }
        [DataMember]
        public string CREATEDBY { get; set; }
        [DataMember]
        public Nullable<System.DateTime> CREATED_DATE { get; set; }
        [DataMember]
        public string MODIFIEDBY { get; set; }
        [DataMember]
        public Nullable<System.DateTime> MODIFIED_DATE { get; set; }
        [DataMember]
        public Nullable<long> CONN_ID { get; set; }
        [DataMember]
        public B_DATABASE_CONN DB_CONN { get; set; }

        public static B_QUERY FromEntity(T_QUERY ent)
        {
            if (ent == null) return null;
            var result = new B_QUERY();
            result.CONN_ID = ent.CONN_ID;
            result.CREATEDBY = ent.CREATEDBY;
            result.QUERY_ID = ent.QUERY_ID;
            result.CREATED_DATE = ent.CREATED_DATE;
            result.MODIFIEDBY = ent.MODIFIEDBY;
            result.QUERY_DESC = ent.QUERY_DESC;
            result.QUERY_NAME = ent.QUERY_NAME;
            result.MODIFIED_DATE = ent.MODIFIED_DATE;
            result.IS_ACTIVE = ent.IS_ACTIVE;
            return result;
        }

        public B_QUERY GetQuerySourceVarName(string strQueryName, string strDBIdx, ref bool isOk, ref string strError,
            ref string strStack, ref string strAdv)
        {
#if !_forWebClient
            throw new NotImplementedException() ;
#else
            B_QUERY queryDataFromDB = (new MarsRESTfulApiClient(strDBIdx)).GetQueryByQueryName(strQueryName,
                ref isOk, ref strError, ref strStack, ref strAdv);
            
            if (!isOk) return null;
            
            return queryDataFromDB;
#endif
        }
    }
}
