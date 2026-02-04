
/*Class to delete configuration  of compare, dbconnection and queries.
  Uses ID to identify the config to be deleted*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Configuration;
using System.Windows.Forms;
using Mars.DataLayer;
using DomUtil;

namespace MARS.CompareGUI
{
    public class DeleteConfigXML
    {
        public static void DeleteQueryData(string ID, string filename)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

            //Removing an existing entry
            string searchstring = "//configuration/Queries/Query[@ID='" + ID + "']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);
                DomHelper.DeleteXmlNode(CheckNodeRegion);
            }
            //xmlDoc.Save(@filename);
           
        }

        public static void DeleteDBConnectionData(string ID, string filename)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

            //Removing an existing entry
            string searchstring = "//configuration/Connections/DBConn[@ID='" + ID + "']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);
                DomHelper.DeleteXmlNode(CheckNodeRegion);
            }
           // xmlDoc.Save(@filename);
        }

        public static void DeleteCompareData(string ID, string filename)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

            //Removing an existing entry
            string searchstring = "//configuration/Compares/Compare[@ID='" + ID + "']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);
                DomHelper.DeleteXmlNode(CheckNodeRegion);
            }
            //xmlDoc.Save(@filename);
        }

        internal static void DeleteProfileData(string ID, string filename)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(@filename);
            xmlDoc = DomHelper.ReadXmlDoc();

            //Removing an existing entry
            string searchstring = "//configuration/Profiles/Profile[@ID='" + ID + "']";
            var CheckNodeRegion = xmlDoc.SelectSingleNode(searchstring);
            if (CheckNodeRegion != null)
            {
                CheckNodeRegion.ParentNode.RemoveChild(CheckNodeRegion);
                DomHelper.DeleteXmlNode(CheckNodeRegion);
            }
        }
    }
}
