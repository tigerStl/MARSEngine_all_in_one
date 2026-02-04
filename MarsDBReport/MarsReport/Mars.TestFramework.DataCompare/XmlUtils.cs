
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;


//using Dotnet.Commons.Reflection;

namespace Mars.TestFramework.DataCompare
{
  
  ///  
  /// <summary>
  /// This utility class contains wrapper functions that help to ease the handling and 
    /// manipulation of Xml documents, such as adding an element, adding an attribute
    /// to an element, copying and cloning of nodes, etc.
  ///
  /// </summary>
  /// 
    
  public abstract class XmlUtils
  {
        // #################################################################### //
        // These code is derived from Mainsoft.com                              //
        // http://www.koders.com/csharp/fid439BB5BEF93D1AEFAF0B9206236AB0ECE49BC229.aspx
        // #################################################################### //


        /// -----------------------------------------------------------
        /// <summary>
        /// Alphabetical sorting of the XmlNodes 
        /// and their  attributes in the <see cref="System.Xml.XmlDocument"/>.
        /// </summary>
        /// <param name="document"><see cref="System.Xml.XmlDocument"/> to be sorted</param>
        /// -----------------------------------------------------------
        public static void SortXml(XmlDocument document)
        {
            SortXml(document.DocumentElement);
        }


        /// -----------------------------------------------------------
        /// <summary>
        /// Inplace pre-order recursive alphabetical sorting of the XmlNodes child 
        /// elements and <see cref="System.Xml.XmlAttributeCollection" />.
        /// </summary>
        /// <param name="rootNode">The root to be sorted.</param>
        /// -----------------------------------------------------------
        public static void SortXml(XmlNode rootNode)
        {
            SortAttributes(rootNode.Attributes);
            SortElements(rootNode);
            foreach (XmlNode childNode in rootNode.ChildNodes)
            {
                SortXml(childNode);
            }
        }

        /// -----------------------------------------------------------
        /// <summary>
        /// Sorts an attributes collection alphabetically.
        /// It uses the bubble sort algorithm.
        /// </summary>
        /// <param name="attribCol">The attribute collection to be sorted.</param>
        /// -----------------------------------------------------------
        public static void SortAttributes(XmlAttributeCollection attribCol)
        {
            if (attribCol == null) 
                return;            

            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                for (int i = 1; i < attribCol.Count; i++)
                {
                    if (String.Compare(attribCol[i].Name, attribCol[i-1].Name, true) < 0)
                    {
                        //Replace
                        attribCol.InsertBefore(attribCol[i], attribCol[i - 1]);
                        hasChanged = true;
                    }
                }
            }

        }

        /// -----------------------------------------------------------
        /// <summary>
        /// Sorts a <see cref="XmlNodeList" /> alphabetically, by the names of the elements.
        /// It uses the bubble sort algorithm.
        /// </summary>
        /// <param name="node">The node in which its childNodes are to be sorted.</param>
        /// -----------------------------------------------------------
        public static void SortElements(XmlNode node)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 1; i < node.ChildNodes.Count; i++)
                {
                    if (String.Compare(node.ChildNodes[i].Name, node.ChildNodes[i-1].Name, true) < 0)
                    {
                        //Replace:
                        node.InsertBefore(node.ChildNodes[i], node.ChildNodes[i-1]);
                        changed = true;
                    }
                }
            }
        }

        public static void SortElementsById(XmlNode node, List<string> keyFieldList)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 1; i < node.ChildNodes.Count; i++)
                {
                    //if (String.Compare(node.ChildNodes[i].Name, node.ChildNodes[i - 1].Name, true) < 0)
                    if (CompareNodesById(node.ChildNodes[i], node.ChildNodes[i - 1], keyFieldList) < 0)
                    {
                        //Replace:
                        node.InsertBefore(node.ChildNodes[i], node.ChildNodes[i - 1]);
                        changed = true;
                    }
                }
            }
        }

        public static int CompareNodesById(XmlNode node1, XmlNode node2, List<string> keyFieldList)
        {
            int rc = 0;

            string value1 = GetCompareValue(node1, keyFieldList);
            string value2 = GetCompareValue(node2, keyFieldList);

            rc = String.Compare(value1, value2, true);

            return rc;
        }

        public static string GetCompareValue(XmlNode node, List<string> keyFieldList)
        {
            string combinedKey = "";

            foreach (var key in keyFieldList)
            {
                if (node[key] != null)
                    combinedKey +=  node[key].InnerText + "__";
            }
            int idx = combinedKey.LastIndexOf("__");

            combinedKey = combinedKey.Remove(idx);
            return combinedKey;
        }

    public static string FindXPath(XmlNode node)
    {
        StringBuilder builder = new StringBuilder();
        while (node != null)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    builder.Insert(0, "/@" + node.Name);
                    node = ((XmlAttribute) node).OwnerElement;
                    break;
                case XmlNodeType.Element:
                    int index = FindElementIndex((XmlElement) node);
                    builder.Insert(0, "/" + node.Name + "[" + index + "]");
                    node = node.ParentNode;
                    break;
                case XmlNodeType.Document:
                    return builder.ToString();
                default:
                    throw new ArgumentException("Only elements and attributes are supported");
            }
        }
        throw new ArgumentException("Node was not in a document");
    }

    static int FindElementIndex(XmlElement element)
    {
        XmlNode parentNode = element.ParentNode;
        if (parentNode is XmlDocument)
        {
            return 1;
        }
        XmlElement parent = (XmlElement) parentNode;
        int index = 1;
        foreach (XmlNode candidate in parent.ChildNodes)
        {
            if (candidate is XmlElement && candidate.Name == element.Name)
            {
                if (candidate == element)
                {
                    return index;
                }
                index++;
            }
        }
        throw new ArgumentException("Couldn't find element within parent");
    }

    public static void sort(string xmlfile, string path, string keyField)
    {
        XElement root = XElement.Load(xmlfile);
        var orderedtabs = root.Elements(path)
                              .OrderBy(xtab => (int)xtab.Element(keyField))
                              .ToArray();
        root.RemoveAll();
        foreach (XElement tab in orderedtabs)
            root.Add(tab);
        root.Save(xmlfile);
    }

    public static string XmlToString(XmlDocument xmlDoc)
    {
        string rString = "";

        using (var stringWriter = new StringWriter())
        using (var xmlTextWriter = XmlWriter.Create(stringWriter))
        {
            xmlDoc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            rString = stringWriter.GetStringBuilder().ToString();
        }

        return rString;

    }

}
   
}
