
/*Class to Save a new configuration*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Configuration;
using System.Windows.Forms;

using Mars.TestFramework.DataCompare;
using Mars.DataLayer;
using System.Xml.Linq;
using DomUtil;
using System.Net;

namespace MARS.CompareGUI
{
    public class SaveConfigXML
    {
        public static void SaveQueryData (QuerywithID obj, string filename)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

            //Removing an existing entry
            string searchstring = "//configuration/Queries/Query[@ID='"+obj.QueryID+"']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);    
            }
            //Creating a new entry
            var nodeRegion = xmlDoc.CreateElement("Query");
            nodeRegion.SetAttribute("ID", obj.QueryID);
            nodeRegion.SetAttribute("Query", WebUtility.HtmlDecode(obj.Query));
            var nodeRegion2 = xmlDoc.SelectSingleNode("//configuration/Queries");
            nodeRegion2.AppendChild(nodeRegion);
            string strError = "";
            bool isOk = DomHelper.UpdateXmlDoc(nodeRegion, ref strError);
            if (!isOk)
            {
                MessageBox.Show(strError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //xmlDoc.Save(@filename);
        }

        public static void SaveDBConnectionData(DBConnectionwithID obj, string filename)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

            //Removing an existing entry
            string searchstring = "//configuration/Connections/DBConn[@ID='"+obj.ConnectionID+"']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);
            }
            //Creating a new entry
            var nodeRegion = xmlDoc.CreateElement("DBConn");
            nodeRegion.SetAttribute("ID", obj.ConnectionID);
            nodeRegion.SetAttribute("Type", obj.DatabaseType);
            nodeRegion.SetAttribute("Host", obj.Host);
            nodeRegion.SetAttribute("Port", obj.Port);
            nodeRegion.SetAttribute("Protocol", obj.Protocol);
            nodeRegion.SetAttribute("ServiceName", obj.ServiceName);
            nodeRegion.SetAttribute("UserID", obj.UserID);
            nodeRegion.SetAttribute("Password", obj.Password);

            nodeRegion.SetAttribute("Sid", obj.Sid);
            nodeRegion.SetAttribute("ConnString", obj.ConnString);

            var nodeRegion2 = xmlDoc.SelectSingleNode("//configuration/Connections");
            nodeRegion2.AppendChild(nodeRegion);
            //xmlDoc.Save(@filename);
            string strError = "";
            bool isOk = DomHelper.UpdateXmlDoc(nodeRegion, ref strError);
        }

        public static void SaveCompareData(ComparewithID obj, string filename)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

            string formatedXml = FormatXml(xmlDoc.InnerXml);

            //Removing an existing entry
            string searchstring = "//configuration/Compares/Compare[@ID='"+obj.CompareID+"']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);
            }
            //Creating a new entry
            var nodeRegion = xmlDoc.CreateElement("Compare");
            nodeRegion.SetAttribute("ID", obj.CompareID);
            nodeRegion.SetAttribute("S1Type", obj.S1Type);
            nodeRegion.SetAttribute("S1DBConn", obj.S1DBConn);
            nodeRegion.SetAttribute("S1QueryID", obj.S1QueryID);
            nodeRegion.SetAttribute("S1FileLoc", obj.S1FileLocation);
            nodeRegion.SetAttribute("S1OpicsRepFileLoc", obj.S1OpicsRepFileLoc);
            nodeRegion.SetAttribute("S1CSVDelim", obj.S1CSVDelim);
           
            
            nodeRegion.SetAttribute("S2Type", obj.S2Type);
            nodeRegion.SetAttribute("S2DBConn", obj.S2DBConn);
            nodeRegion.SetAttribute("S2QueryID", obj.S2QueryID);
            nodeRegion.SetAttribute("S2FileLoc", obj.S2FileLocation);
            nodeRegion.SetAttribute("S2OpicsRepFileLoc", obj.S2OpicsRepFileLoc);
            nodeRegion.SetAttribute("S2CSVDelim", obj.S2CSVDelim);

            nodeRegion.SetAttribute("OFileLoc", obj.OFileLocation);
            nodeRegion.SetAttribute("KeyFields", obj.KeyFields);
            nodeRegion.SetAttribute("ShowFields", obj.ShowFields);
            nodeRegion.SetAttribute("CompareFields", obj.CompareFields);
            nodeRegion.SetAttribute("RowFields", obj.RowFields);
            nodeRegion.SetAttribute("ColumnFields", obj.ColumnFields);

            nodeRegion.SetAttribute("OutputFilter", obj.OutputFilter);
            nodeRegion.SetAttribute("OutputOrderBy", obj.OutputOrderBy);
            nodeRegion.SetAttribute("OutputFilterApply", obj.OutputFilterApply.ToString());

            var nodeRegion2 = xmlDoc.SelectSingleNode("//configuration/Compares");
            nodeRegion2.AppendChild(nodeRegion);

            formatedXml = FormatXml(xmlDoc.InnerXml);

            //xmlDoc.Save(@filename);
            string strError = "";
            bool isOk = DomHelper.UpdateXmlDoc(nodeRegion, ref strError);
        }


        public static void SaveOutputDirName(string outDirStr, string filename)
        {
            XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

         

            //Creating a new entry
            XmlNode opdNode = xmlDoc.CreateElement("OutPutDir");
            XmlNode opdpNode = xmlDoc.CreateElement("OutPutDirPath");
            opdpNode.InnerText = outDirStr;
            opdNode.AppendChild(opdpNode);


            XmlNode nodeRegion2 = xmlDoc.SelectSingleNode("//configuration/OutPutDir");
            if (nodeRegion2 == null)
            {
                xmlDoc.DocumentElement.AppendChild(opdNode);

            }
            else
            {
                xmlDoc.DocumentElement.ReplaceChild(opdNode, nodeRegion2);
            }
            xmlDoc.Save(@filename);
        }

        internal static void SaveProfileData(ProfileWithID newProfileWithID, string filename)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc = DomHelper.ReadXmlDoc();

            //Removing an existing entry
            string searchstring = "//configuration/Profiles/Profile[@ID='" + newProfileWithID.ProfileNameID + "']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);
            }
            //Creating a new entry
            var nodeRegion = xmlDoc.CreateElement("Profile");
            nodeRegion.SetAttribute("ID", newProfileWithID.ProfileNameID);

            nodeRegion.SetAttribute("BaselineFmt", newProfileWithID.BaselineFmt);
            nodeRegion.SetAttribute("BaselineRpt", newProfileWithID.BaselineRpt);
            nodeRegion.SetAttribute("CompareFmt", newProfileWithID.CompareFmt);
            nodeRegion.SetAttribute("CompareRpt", newProfileWithID.CompareRpt);
            nodeRegion.SetAttribute("outDir", newProfileWithID.outDir);


            var nodeRegion2 = xmlDoc.SelectSingleNode("//configuration/Profiles");
            nodeRegion2.AppendChild(nodeRegion);
            string strError = "";
            bool isOk = DomHelper.UpdateXmlDoc(nodeRegion, ref strError);
            //xmlDoc.Save(@filename);
        }

        static string FormatXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch (Exception)
            {
                // Handle and throw if fatal exception here; don't just ignore them
                return xml;
            }
        }
    }
}
