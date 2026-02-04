using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace XmlCompareLib
{
    public  class XmlNodeClassifier
    {
        public enum NodeType  
        {
            DOCUMENT = 0,
            TEXT = 1,
            EMPTY = 2,
            PARENT = 3,
            CHILD = 4,
            ENTITY = 5
        };

        public static NodeDescriptor GetNodeDescriptor(XmlNode node)
        {
            NodeDescriptor desriptor = new NodeDescriptor();
            desriptor.name = node.Name;


            if (node.Name.Equals("#document"))
            {
                desriptor.type = NodeType.DOCUMENT;
            }
            else
            if (node.Name.Equals("#text"))
            {
                desriptor.type = NodeType.TEXT;
            }
            else

            if (node.ChildNodes.Count == 0)
            {
                desriptor.type = NodeType.EMPTY;
            }

            else if (node.ChildNodes.Count == 1 && node.FirstChild.HasChildNodes == false)
            {
                desriptor.type = NodeType.CHILD;
            }
            else
            {
                if (isEntity(node))
                    desriptor.type = NodeType.ENTITY;
                else
                    desriptor.type = NodeType.PARENT;
            }
           
            return desriptor;
        }

        private static bool isEntity(XmlNode node)
        {
            bool isEnt = false;

            if ((node.Name.Equals("TRADELIST") || node.Name.Equals("ENTITYLIST")) ||
                    (node.Attributes != null &&
                    node.Attributes.Count > 0 &&
                    node.Attributes["TYPE"] != null &&
                    node.Attributes["SINGLE"] != null &&
                    node.Attributes["TYPE"].Value.Equals("EntList") &&
                    node.Attributes["SINGLE"].Value.Equals("N"))
                )
                isEnt = true;
            return isEnt;

        }

    }

    public  class NodeDescriptor
    {

        public XmlNodeClassifier.NodeType type;
        public string name = "";

        
    }

}
