using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Mars.TestFramework.DataCompare
{
    public class XmlCompareProcessor
    {
        XmlDocument doc1, doc2;
        Dictionary<XmlNode, string> nodeDict = new Dictionary<XmlNode, string>();
        Dictionary<string,XmlNode> xpathDict = new Dictionary<string, XmlNode>();
        List<String> xPathList = new List<string>();

        public void ProcessDocument(string inputPath1, string inputPath2, string resultPath1, string resultPath2)
        {
            doc1 = new XmlDocument();
            doc1.Load(inputPath1);

            doc2 = new XmlDocument();
            doc2.Load(inputPath2);
          
            CompareXml();

            // Commented out for testing
             SortEntities(doc1);
             SortEntities(doc2);
            


            doc1.Save(resultPath1);
            /*
             using (var writer = new XmlTextWriter(resultPath1, new UTF8Encoding(false)))
            {
                Console.WriteLine("resultPath1=" + resultPath1);
                doc1.Save(writer);
            }
             */ 
            doc2.Save(resultPath2);

            /*
             using (var writer = new XmlTextWriter(resultPath2, new UTF8Encoding(false)))
             {
                 Console.WriteLine("resultPath2=" + resultPath2);
                 doc2.Save(writer);
             }
            */
        }

        public void SortDocument(XmlDocument doc )
        {
            XmlUtils.SortXml(doc);
        }

        private void SortEntities(XmlDocument doc)
        {
            doc.IterateThroughAllNodes(
               delegate(XmlNode node)
               {
                   NodeDescriptor descr = XmlNodeClassifier.GetNodeDescriptor(node);
                   if (descr.type != XmlNodeClassifier.NodeType.TEXT)
                   {
                       //Console.WriteLine("=>Name:" + descr.name + " Type:" + descr.type.ToString());
                       switch (descr.type)
                       {
                           case XmlNodeClassifier.NodeType.PARENT:
                               //SortChildren(node);
                               break;
                           case XmlNodeClassifier.NodeType.ENTITY:
                               List<string> keyFieldList = XmlKeyFieldConfig.GetKeyList(node.Name);
                               if (keyFieldList != null)
                                   SortEntity(node, keyFieldList);
                               break;

                           default:
                               break;
                       }
                   }

               });
        }

        private void CompareXml()
        {
            string previousXpath = "";
            doc1.IterateThroughAllNodes(
               delegate(XmlNode node)
               {
                   NodeDescriptor parentDescr = XmlNodeClassifier.GetNodeDescriptor(node.ParentNode);
                   NodeDescriptor descr = XmlNodeClassifier.GetNodeDescriptor(node);
                   string xpath = "";
                  
                   if (descr.type != XmlNodeClassifier.NodeType.TEXT && descr.name.Equals("xml") == false)
                   {
                       //Console.WriteLine("=>Name:" + descr.name + " Type:" + descr.type.ToString());

                       switch (parentDescr.type)
                       {
                           case XmlNodeClassifier.NodeType.DOCUMENT:
                               xpath = "/" + node.Name;
                               break;
                           case XmlNodeClassifier.NodeType.PARENT:
                           case XmlNodeClassifier.NodeType.CHILD:
                           case XmlNodeClassifier.NodeType.EMPTY:
                               xpath = nodeDict[node.ParentNode] + "/" + node.Name;
                               break;

                           case XmlNodeClassifier.NodeType.ENTITY:
                               string searchStr = GetSearchStr(node, parentDescr.name, nodeDict[node.ParentNode] + "/" + node.Name);
                               xpath = nodeDict[node.ParentNode] + "/" + node.Name + searchStr;
                               break;

                           default:
                               break;
                       }
                       //Console.WriteLine("xpath = " + xpath);
                       CompareNode(node, xpath, descr, previousXpath);
                       
                       SaveXpath(node, xpath);
                       previousXpath = xpath;
                   }
               });
        }

        private void CompareNode(XmlNode node, string xpath, NodeDescriptor descr, string previousXpath)
        {
            XmlNode node2 = doc2.SelectSingleNode(xpath);
            //Console.WriteLine("Processing xpath: " + xpath);
            if (node2 == null)
            {
                Console.WriteLine("WARNING  CompareNode: " + xpath + " was NOT found in the second XML");
                /*
                XmlAttribute attr = doc1.CreateAttribute("DIFF");
                attr.Value = "NOT FOUND";
                node.Attributes.SetNamedItem(attr);
                */

                // add node to the second document
                XmlNode newNode = doc2.ImportNode(node, true);
                string parentPath = nodeDict[node.ParentNode];

                XmlNode targetParentNode = doc2.SelectSingleNode(parentPath);

                XmlNode prevNode = doc2.SelectSingleNode(previousXpath);

                //targetParentNode.AppendChild(newNode); 
                // instead of append do InsrtAfter
                targetParentNode.InsertAfter(newNode, prevNode);

                MarkNewNode(newNode, "IMPORTED");
            }
           
            else if (  (descr.type == XmlNodeClassifier.NodeType.CHILD ||
                    descr.type == XmlNodeClassifier.NodeType.EMPTY) &&
               // !node.InnerXml.Equals(node2.InnerXml) &&
                !CompareValues(node, node2) &&
                !XmlKeyFieldConfig.IgnoreList.Contains(node.Name))
            {
                XmlAttribute attr = doc1.CreateAttribute("DIFF");
                attr.Value = "NOT EQUAL ";
                node.Attributes.SetNamedItem(attr);
            }
          }

        bool CompareValues(XmlNode node1, XmlNode node2)
        {
            bool isEqual = false;
            string value1 = node1.InnerXml;

            string value2 = XmlKeyFieldConfig.GetMappedValue(node2.Name, node2.InnerXml);

            isEqual = value1.Equals(value2);
            return isEqual;
        }

        private void MarkNewNode(XmlNode newNode, string marking)
        {
            XmlAttribute attr = doc2.CreateAttribute("DIFF");
            attr.Value = marking;
            newNode.Attributes.SetNamedItem(attr);
            newNode.IterateThroughAllNodes(
              delegate(XmlNode node)
              {
                  
                  if (node.Name.Equals("#text") == false)
                  { 
                      attr = doc2.CreateAttribute("DIFF");
                      attr.Value = marking;
                      node.Attributes.SetNamedItem(attr);
                  }
              });
        }

        private string GetSearchStr(XmlNode node, string parentName, string parentPath)
        {
            int position = 1;
            bool done = false;
            string searchStr = "[";
            string finalSearchString = "";

            List<string> keyFieldList = XmlKeyFieldConfig.GetKeyList(parentName);

            foreach (var key in keyFieldList)
            {
                if (node[key] != null)
                {
                    // map data here using mapping tables

                    string newValue = XmlKeyFieldConfig.GetMappedValue(key, node[key].InnerText);

                    if (newValue != null)
                        searchStr += key + "='" + newValue + "' and ";
                    else
                        searchStr += key + "='" + node[key].InnerText + "' and ";
                }
            }
            
            int idx = searchStr.LastIndexOf(" and ");
            searchStr = searchStr.Remove(idx);
            searchStr += "]";
           
            while (done == false)
            {
                finalSearchString = searchStr + "[" + position + "]";
                //Console.WriteLine(parentPath + finalSearchString + "<< Searching for");
               
                if (xpathDict.ContainsKey(parentPath + finalSearchString) == true)
                {
                    Console.WriteLine("WARNING: Duplicate key detected!!!");
                    position++;
                }
                else
                    break;
            }
            return finalSearchString;
        }

        private void SaveXpath(XmlNode node, string xpath)
        {
            nodeDict.Add(node, xpath);
            xPathList.Add(xpath);
            xpathDict.Add(xpath, node);
        }
        private void SortEntity(XmlNode node, List<string> keyFieldList)
        {
            XmlUtils.SortElementsById(node, keyFieldList);
        }
    }
}
