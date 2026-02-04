using Mars.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DomUtil
{
    public class DomHelper
    {
        public static string mode { get; set; }

        public static bool DEBUG = false;

        public static XmlDocument ReadXmlDoc()
        {
            XmlDocument doc;
            XmlDocument doc2;

            if (mode.Equals("DB"))
            {
                doc = DbDomHelper.ReadXmlDoc();
                if (DEBUG)
                    doc2 = WebApiDomHelper.ReadXmlDoc();
            }
                
            else
                doc = WebApiDomHelper.ReadXmlDoc();

            return doc;
        }

        public static void DeleteXmlNode(XmlNode node)
        {
            if (mode.Equals("DB"))
            {
                DbDomHelper.DeleteXmlNode(node);
                if (DEBUG)
                    WebApiDomHelper.DeleteXmlNode(node);

            }
            else
                WebApiDomHelper.DeleteXmlNode(node);

        }

        public static bool UpdateXmlDoc(XmlNode node,ref string strError)
        {
            if (mode.Equals("DB"))
            {
                DbDomHelper.UpdateXmlDoc(node);
                return true;
            }
               
            else
            {
                bool isOk = WebApiDomHelper.UpdateXmlDoc(node,ref strError);
                return isOk;
            }
        }
    }
}
