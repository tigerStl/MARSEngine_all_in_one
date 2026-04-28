using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using MySql.Data.MySqlClient;
using Mars.message.AutoTestingDriver.ExecuteTestcase.keywordOp;
using System.Net.Http.Formatting;
using Mars.Properties;

namespace Mars.message.DataLayer.multipleDBSupport
{
    public class MarsDBConnectionFactory
    {
        private IDbConnection connection;
        public string currentDBType { get; set; }
        public string currentConnectionString { get; set; }
        public MarsDBConnectionFactory(string dbType, string connectionString)
        {
            currentDBType = dbType;
            currentConnectionString = connectionString;
        }

        public bool InitDBConnection()
        {
            switch (currentDBType.ToLower())
            {
                case "sql":
                    connection = new SqlConnection(currentConnectionString);
                    break;
                case "oracle":
                    connection = new OracleConnection(currentConnectionString);
                    break;
                //case "mysql":
                //    connection = new MySqlConnection(currentConnectionString);
                //    break;
                default:
                    return false;
            }
            return true; 
        }

        public IDbConnection GetConnection()
        {
            return connection;
        }

        public IDbCommand CreateCommand(string query, IDbConnection connection)
        {
            switch (connection)
            {
                case SqlConnection sqlConnection:
                    return new SqlCommand(query, sqlConnection);
                case OracleConnection oracleConnection:
                    return new OracleCommand(query, oracleConnection);
                //case MySqlConnection mySqlConnection:
                //    return new MySqlCommand(query, mySqlConnection);
                default:
                    throw new ArgumentException("Invalid connection type specified.");
            }
        }

        public IDbDataParameter CreateParameter(string parameterName, object value)
        {
            switch (connection)
            {
                case SqlConnection sqlConnection:
                    return new SqlParameter(parameterName, value);
                case OracleConnection oracleConnection:
                    return new OracleParameter(parameterName, value);
                //case MySqlConnection mySqlConnection:
                //    return new MySqlParameter(parameterName, value);
                default:
                    throw new ArgumentException("Invalid connection type specified.");
            }
        }

        public void ExecuteQueryWithParameters(string query, Action<IDbCommand> addParametersAction)
        {
            try
            {
                using (IDbConnection connection = GetConnection())
                {
                    connection.Open();
                    Console.WriteLine($"Connected to database.");

                    using (IDbCommand command = CreateCommand(query, connection))
                    {
                        // Allow the caller to add parameters to the command
                        addParametersAction?.Invoke(command);

                        using (IDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine($"Result: {reader[0]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        
        internal IDbCommand addParas(IDbCommand dbCmd, MarsQueryDBKeywordParaMeters paraMeters, List<DBQueryDataSettingPara> para, ref bool isOk, ref string strError)
        {
            dbCmd.Parameters.Clear();
            foreach(var itm in paraMeters.ParaMeter)
            {
                if (itm == null) continue;
                // get value
                var curPara = para.Where(px => px.f == itm.PmName).FirstOrDefault();
                if (curPara == null) {
                    isOk = false;
                    strError = string.Format(DataLayerResources.datalayer_no_parameter_data_is_set, itm.PmName);
                    return null;
                }

                IDbDataParameter p = CreateParameter(itm.PmName, curPara.v);
                dbCmd.Parameters.Add(p);
            }
            isOk = true;
            return dbCmd;            
        }

        internal DataTable readDataToDataTable(IDbCommand dbCmd, ref bool isOk, ref string strError)
        {
            DataTable dt = new DataTable();
            try
            {

                using (IDataReader reader = dbCmd.ExecuteReader())
                {
                    // Load the data into the DataTable
                    dt.Load(reader);
                }
                isOk = true;
                return dt;
            }
            catch (Exception e)
            {
                strError = e.Message;
                isOk = false;
                return null;
            }
        }

        internal DataTable filterColumnsFromDatatable(MarsQueryDBKeywordResultSetFields resultSetFields, DataTable dt, ref bool isOk, ref string strError)
        {
            if (dt==null)
            {
                isOk = true;
                return null;
            }
            if (resultSetFields == null)
            {
                isOk = true;
                return dt;
            }
            var columnsToKeep = resultSetFields.ResultSetField.Select(p=>p.FieldName).ToList();
            var columnsToRemove = dt.Columns.Cast<DataColumn>()
                .Where(column => !columnsToKeep.Contains(column.ColumnName))
                .ToList();
            foreach (var column in columnsToRemove)
            {
                dt.Columns.Remove(column);
            }
            isOk = true;
            return dt;
        }

        internal void closeConns()
        {
            if (connection != null)
            {
                try
                {
                    connection.Close();
                }catch(Exception)
                {

                }
            }
        }
    }
}
