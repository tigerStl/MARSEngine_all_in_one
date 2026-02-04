using Mars.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DomUtil
{
    internal static class DbDomHelper
    {
        public static int COMPARE_TYPE = 1;
        public static int CONN_TYPE = 2;
        public static int QUERY_TYPE = 3;
        public static int PROFILE_TYPE = 4;

        public static string currentDBIdx = "MarsEntities";

        static XmlDocument doc = null;
        public static void RefreshXmlDoc()
        {
            doc = null;
            doc = ReadXmlDoc();
        }

        public static XmlDocument ReadXmlDoc()
        {
            if (doc != null && doc.FirstChild != null)
                return doc;

            doc = new XmlDocument();
            StringBuilder builder = new StringBuilder();
            string xmlString;



            var data = BoHelper.GetDataSourceData(currentDBIdx);

            List<string> compareData = (from d in data where d.DATA_SOURCE_TYPE == COMPARE_TYPE select d.DETAILS).ToList();
            List<string> connData = (from d in data where d.DATA_SOURCE_TYPE == CONN_TYPE select d.DETAILS).ToList();
            List<string> queryData = (from d in data where d.DATA_SOURCE_TYPE == QUERY_TYPE select d.DETAILS).ToList();
            List<string> profileData = (from d in data where d.DATA_SOURCE_TYPE == PROFILE_TYPE select d.DETAILS).ToList();

            // assemble the xml
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            builder.Append("<configuration>");

            // Compares
            builder.Append("<Compares>");
            foreach (string s in compareData)
                builder.Append(s);
            builder.Append("</Compares>");

            // Connections
            builder.Append("<Connections>");
            foreach (string s in connData)
                builder.Append(s);
            builder.Append("</Connections>");

            // Queries
            builder.Append("<Queries>");
            foreach (string s in queryData)
                builder.Append(s);
            builder.Append("</Queries>");

            // Profiles
            builder.Append("<Profiles>");
            foreach (string s in profileData)
                builder.Append(s);
            builder.Append("</Profiles>");


            builder.Append("</configuration>");

            xmlString = builder.ToString();

            doc.LoadXml(xmlString);

            return doc;
        }

        public static void DeleteXmlNode(XmlNode node)
        {
            string data = node.OuterXml;
            string name = node.Name;
            string id = node.Attributes["ID"].Value;
            short dataType = -1;
            switch (name)
            {
                case "Compare":
                    dataType = (short)COMPARE_TYPE;
                    break;
                case "Query":
                    dataType = (short)QUERY_TYPE;
                    break;
                case "DBConn":
                    dataType = (short)CONN_TYPE;
                    break;
                case "Profile":
                    dataType = (short)PROFILE_TYPE;
                    break;
            }
            BoHelper.DeleteDataSource(currentDBIdx,id, dataType);
            RefreshXmlDoc();
        }

        public static void UpdateXmlDoc(XmlNode node)
        {
            string data = node.OuterXml;
            string name = node.Name;
            string id = node.Attributes["ID"].Value;
            short dataType = -1;

            switch (name)
            {
                case "Compare":
                    dataType = (short)COMPARE_TYPE;
                    break;

                case "Query":
                    dataType = (short)QUERY_TYPE;
                    break;

                case "DBConn":
                    dataType = (short)CONN_TYPE;
                    break;

                case "Profile":
                    dataType = (short)PROFILE_TYPE;
                    break;
            }

            BoHelper.UpdateDataSource(currentDBIdx, id, data, dataType);
            // AF: looks like this is not needed         RefreshXmlDoc();
        }

        public static void SaveXmlDoc(XmlDocument doc)
        {

        }

    }
}
