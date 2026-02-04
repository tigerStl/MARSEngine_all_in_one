using MARS_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MARS_Repository.BO
{
    [DataContract]
    public class B_DATABASE_CONN
    {
        [DataMember]
        public long CONNECTION_ID { get; set; }
        [DataMember]
        public string CONNECTION_NAME { get; set; }
        [DataMember]
        public Nullable<short> CONNECTION_TYPE { get; set; }
        
        [DataMember]
        public string HOST_NAME { get; set; }
        [DataMember]
        public Nullable<int> PORT_NUMBER { get; set; }
        [DataMember]
        public string PROTOCOL { get; set; }
        [DataMember]
        public string SERVICE_NAME { get; set; }
        [DataMember]
        public string DB_SID { get; set; }
        [DataMember]
        public string DB_USERNAME { get; set; }
        [DataMember]
        public string DB_PASSWORD { get; set; }
        [DataMember]
        public string CONNECTION_STRING { get; set; }
        [DataMember]
        public Nullable<short> ACTIVE { get; set; }
        [DataMember]
        public string CREATEDBY { get; set; }
        [DataMember]
        public Nullable<System.DateTime> CREATION_DATE { get; set; }
        [DataMember]
        public string MODIFIEDBY { get; set; }
        [DataMember]
        public Nullable<System.DateTime> MODIFIED_DATE { get; set; }
        [DataMember]
        public Nullable<short> IS_TESTED { get; set; }
        [DataMember]
        public Nullable<System.DateTime> LAST_TESTED { get; set; }
        [DataMember]
        public string ERROR_MESSAGE { get; set; }

        public void fromEntity(T_DATABASE_CONNECTIONS dbcnn)
        {
            this.ACTIVE = dbcnn.ACTIVE;
            this.CONNECTION_ID = dbcnn.CONNECTION_ID;
            this.CONNECTION_NAME = dbcnn.CONNECTION_NAME;
            this.CONNECTION_STRING = dbcnn.CONNECTION_STRING;
            this.CONNECTION_TYPE   = dbcnn.CONNECTION_TYPE;
            this.CREATEDBY = dbcnn.CREATEDBY;
            this.CREATION_DATE =   dbcnn.CREATION_DATE;
            this.DB_PASSWORD = dbcnn.DB_PASSWORD;
            this.DB_SID = dbcnn.DB_SID;
            this.DB_USERNAME = dbcnn.DB_USERNAME;
            this.ERROR_MESSAGE = dbcnn.ERROR_MESSAGE;
            this.HOST_NAME = dbcnn.HOST_NAME;
            this.IS_TESTED = dbcnn.IS_TESTED;
            this.LAST_TESTED = dbcnn.LAST_TESTED;
            this.MODIFIEDBY = dbcnn.MODIFIEDBY;
            this.MODIFIED_DATE = dbcnn.MODIFIED_DATE;
            this.PORT_NUMBER = dbcnn.PORT_NUMBER;
            this.PROTOCOL = dbcnn.PROTOCOL;
            this.SERVICE_NAME = dbcnn.SERVICE_NAME;
        }
    }
}
