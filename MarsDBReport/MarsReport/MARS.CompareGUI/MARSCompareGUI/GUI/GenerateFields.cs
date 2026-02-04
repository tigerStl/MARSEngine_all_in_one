
/*Class to generate fields from files and db sources*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Configuration;
using System.Collections.Specialized;
using Mars.TestFramework.DataCompare;

namespace MARS.CompareGUI
{
    class GenerateFields
    {
        public static string GenFields(string file)
        {
            string fields = "";
            var xmlDoc = new XmlDocument();
            XmlNamespaceManager xmlnsManager1;
            xmlnsManager1 = new XmlNamespaceManager(xmlDoc.NameTable);

            xmlnsManager1.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager1.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager1.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager1.AddNamespace("z", "#RowsetSchema");

            xmlDoc.Load(file);

            foreach (XmlNode targetnode in xmlDoc.SelectNodes("//" + "rs:data", xmlnsManager1))
            {
                XmlNode n = targetnode.SelectSingleNode("./" + "z:row", xmlnsManager1);
                fields = AllAttrString(n);
            }

            fields = fields.TrimEnd(',');
            fields = fields.Remove(fields.Length - 1);
            //fields = fields.ToLower();
            return fields;
        }

        public static string GenFieldsCSV(string file, string customDelim)
        {
            string fields = "";
            var xmlDoc = new XmlDocument();
            XmlNamespaceManager xmlnsManager1;
            xmlnsManager1 = new XmlNamespaceManager(xmlDoc.NameTable);

            xmlnsManager1.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager1.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager1.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager1.AddNamespace("z", "#RowsetSchema");

            //xmlDoc.Load(file);
            xmlDoc = Conversion.CsvToDom(file, customDelim);

            foreach (XmlNode targetnode in xmlDoc.SelectNodes("//" + "rs:data", xmlnsManager1))
            {
                XmlNode n = targetnode.SelectSingleNode("./" + "z:row", xmlnsManager1);
                fields = AllAttrString(n);
            }

            fields = fields.TrimEnd(',');
            fields = fields.Remove(fields.Length - 1);
            //fields = fields.ToLower();
            return fields;
        }

        public static string GenDatabaseFields(XmlDocument xmlfile)
        {
            string fields = "";
            var xmlDoc = new XmlDocument();
            XmlNamespaceManager xmlnsManager1;
            xmlnsManager1 = new XmlNamespaceManager(xmlDoc.NameTable);

            xmlnsManager1.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager1.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager1.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager1.AddNamespace("z", "#RowsetSchema");

            xmlDoc = xmlfile;

            foreach (XmlNode targetnode in xmlDoc.SelectNodes("//" + "rs:data", xmlnsManager1))
            {
                XmlNode n = targetnode.SelectSingleNode("./" + "z:row", xmlnsManager1);
                fields = AllAttrString(n);
            }

            fields = fields.TrimEnd(',');
            fields = fields.Remove(fields.Length - 1);
            //fields = fields.ToLower();
            return fields;
        }

        public static string AllAttrString(XmlNode rowNode1)
        {
            string all = "";

            foreach (XmlAttribute attr in rowNode1.Attributes)
            {
                all += attr.Name + ", ";
            }

            return all;
        }
/*
        internal static string GenFieldsREPORT_byXML(string file)
        {
            string fields = "";
            var xmlDoc = new XmlDocument();
            XmlNamespaceManager xmlnsManager1;
            xmlnsManager1 = new XmlNamespaceManager(xmlDoc.NameTable);

            xmlnsManager1.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager1.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager1.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager1.AddNamespace("z", "#RowsetSchema");

            //xmlDoc.Load(file);
            xmlDoc = Conversion.ReportToDom(file, 1);

            foreach (XmlNode targetnode in xmlDoc.SelectNodes("//" + "rs:data", xmlnsManager1))
            {
                XmlNode n = targetnode.SelectSingleNode("./" + "z:row", xmlnsManager1);
                fields = AllAttrString(n);
            }

            fields = fields.TrimEnd(',');
            fields = fields.Remove(fields.Length - 1);
            //fields = fields.ToLower();
            return fields;
        }
*/

        internal static string GenFieldsREPORT(string file, string fmtLocation)
        {
            string fields = "";

            System.Data.DataTable dt = Conversion.ConvertReportToDataTable(file, 1, fmtLocation);

            foreach (System.Data.DataColumn col in dt.Columns)
            {
                fields += col.ColumnName + ", ";
            }

            int idx = fields.LastIndexOf(",");

            fields = fields.Remove(idx);
            
            return fields;
        }

        

        internal static string GenFieldsExcel(string file)
        {
            string fields = "";

            System.Data.DataTable dt = Conversion.ConvertExcelToDataTable(file);

            foreach (System.Data.DataColumn col in dt.Columns)
            {
                fields += col.ColumnName + ", ";
            }

            int idx = fields.LastIndexOf(",");

            fields = fields.Remove(idx);

            return fields;
        }

        internal static string GenFieldsSWIFT(string file)
        {
            string fields = "";

            System.Data.DataTable dt = Conversion.ConvertSWIFTToDataTable(file);

            foreach (System.Data.DataColumn col in dt.Columns)
            {
                fields += col.ColumnName + ", ";
            }

            int idx = fields.LastIndexOf(",");

            fields = fields.Remove(idx);

            return fields;
        }
    }
}
