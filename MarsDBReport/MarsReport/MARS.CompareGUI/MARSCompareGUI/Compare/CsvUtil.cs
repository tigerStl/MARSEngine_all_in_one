using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MARS.CompareGUI
{
    class CsvUtil
    {
        public static string delim = ",";
        public static string noHeaders ="";
        public static XmlDocument CsvToDom(string path)
        {
            XmlDocument doc = null;

            //DataTable dt = CsvUtil.GetDataTableFromCsv(path, true);

            DataTable dt = ConvertCSVtoDataTable(path);

            foreach (DataColumn column in dt.Columns)
            {
                //string newColName = column.ColumnName.Replace(" ", string.Empty);
                string newColName = column.ColumnName.Replace(" ", string.Empty).Replace(")", string.Empty).Replace("(", string.Empty).Replace("#", string.Empty).Replace("/", string.Empty).Replace("*", string.Empty);
                column.ColumnName = newColName;
            }

            doc = DataTableToDom(dt);
            return doc;
        }

        private static XmlDocument DataTableToDom(DataTable dt)
        {
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();

            XmlDocument doc = new XmlDocument();

            XmlNode rootNode = doc.CreateNode("element", "rs:data", "urn:schemas-microsoft-com:rowset");
            doc.AppendChild(rootNode);
            foreach (DataRow row in dt.Rows)
            {
                XmlNode node = doc.CreateNode("element", "z:row", "#RowsetSchema");
                rootNode.AppendChild(node);
                foreach (string colName in columnNames)
                {
                    // Console.WriteLine("colName " + colName);
                    ((XmlElement)node).SetAttribute(colName, row[colName].ToString());
                }
            }

            //Console.WriteLine(doc.OuterXml);
            return doc;
        }

        public static DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            if (delim != null)
            {
                ///////////////////////

                using (var input = File.OpenText(path))
                using (StreamWriter output = new StreamWriter(path + "_3"))
                {
                    string line;
                    while (null != (line = input.ReadLine()))
                    {
                        line = line.Replace(delim, ",");
                        output.WriteLine(line);
                    }
                    output.Flush();

                    output.Close();
                    output.Dispose();
                }
                fileName = fileName + "_3";
                /////////////////////

                /*
                string text = File.ReadAllText(path);
                text = text.Replace(delim, ",");
                File.WriteAllText(path + "_1", text);
                
                fileName = fileName + "_1";
                */
            }

            string sql = @"SELECT * FROM [" + fileName + "]";


            /*
             * before adding FMT=Delimited
              using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + "\""))
             */


            using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + "\""))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                DataTable dataTable = new DataTable();
                dataTable.Locale = CultureInfo.CurrentCulture;
                try
                {
                    adapter.Fill(dataTable);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return dataTable;
            }
        }

        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            Console.WriteLine("CSV: " + strFilePath);

            HashSet<string> names = new HashSet<string>();

            if (delim == null)
                delim = ",";

            StreamReader sr = new StreamReader(strFilePath);

            string headerLine = sr.ReadLine();

            string[] headers = headerLine.Split(delim[0]);

            // Handle sheet that has no headers -- create headers on the fly
            if (noHeaders != null && noHeaders.Equals("TRUE"))
            {
                for (int i = 0; i < headers.Length; i++)
                    headers[i] = "FIELD_" + i;
            }


            // handle unnamed fields
            for (int i = 0; i < headers.Length; i++)
            {
                // handle duplicate fields
                if (names.Contains(headers[i]))
                    headers[i] = headers[i] + i;

                if (headers[i] == null || headers[i].Trim().Length == 0)
                    headers[i] = "FIELD_" + i;

                names.Add(headers[i]);
            }




            DataTable dt = new DataTable();
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            while (!sr.EndOfStream)
            {
                string[] rows = sr.ReadLine().Split(delim[0]);
                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }


        private static void CsvTest()
        {
            DataTable dt = CsvUtil.GetDataTableFromCsv(@"c:\temp\plupd1.csv", true);
            Console.Read();
        }

    }
}
