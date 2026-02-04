using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Data.Common;

namespace Mars.message.DataLayer
{
    public class MarsTransactionMgrByConn
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTransactionMgrByConn));

        private DbTransaction mobjCurrentTransaction = null;
        private DbConnection mobjCurrentConnection = null;
        public DbConnection CurrentConnection
        {
            get { return mobjCurrentConnection; }
            private set { mobjCurrentConnection = value; }
        }

        public bool InitTransaction(string strDBIdx, ref string strError)
        {
            try
            {
                mobjCurrentConnection = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx).Database.Connection;
                if (mobjCurrentConnection.State != System.Data.ConnectionState.Open)
                    mobjCurrentConnection.Open();
                mobjCurrentTransaction = mobjCurrentConnection.BeginTransaction();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InitTransaction", strError = string.Format("Exception when get transaction and connection:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }

        }

        public bool CommitCurrent(ref string strError)
        {
            Logger.logBegin("CommitCurrent");
            try
            {
                mobjCurrentTransaction.Commit();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CommitCurrent", strError = string.Format("Exception when commit:[{0}],stackTrace:\r\n{1}\r\nInner:{2}",
                    e.Message,
                    e.StackTrace,
                    e.InnerException == null ? "N/A" : e.InnerException.Message));
                return false;
            }
        }
    }
}
