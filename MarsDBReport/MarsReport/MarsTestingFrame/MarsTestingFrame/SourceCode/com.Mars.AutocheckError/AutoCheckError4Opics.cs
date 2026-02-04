extern alias clientWCF;
using clientWCF::MarsTestFrame.CommuniteServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace com.Mars.AutocheckError
{
    public class AutoCheckError4Opics
    {
        private string autocheckFileName;
        public AutoCheckError4Opics(string strAutoCheckErrorFileName)
        {
            autocheckFileName = strAutoCheckErrorFileName;
        }

        private MarsAutoCheckErrorFile AutoCheckErrorMessage;
        public bool InitObject(ref string strError)
        {
            //XmlSerializer objXmlSrlzr = new XmlSerializer(typeof(MarsAutoCheckErrorFile));
            try
            {
                AutoCheckErrorMessage = XmlHelper.XmlDeserializeFromFile<MarsAutoCheckErrorFile>(autocheckFileName, Encoding.UTF8);
                //AutoCheckErrorMessage = (MarsAutoCheckErrorFile)objXmlSrlzr.Deserialize(new System.IO.FileStream(autocheckFileName, System.IO.FileMode.Open));
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]",e.Message);
                AutoCheckErrorMessage = null;
                return false;
            }
        }

        public List<KeyValuePair<string, int>> CombineMessageToHash()
        {
            if (AutoCheckErrorMessage == null) return new List<KeyValuePair<string, int>>();
            if (AutoCheckErrorMessage.AutoCheckErrorMessages == null) return new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> resultList = new List<KeyValuePair<string, int>>();
            foreach (var itm in AutoCheckErrorMessage.AutoCheckErrorMessages)
            {
                resultList.Add(new KeyValuePair<string, int>(itm.AutocheckErrorMessage,itm.Type));
            };

            return resultList;
        }
    }

    [XmlRoot(ElementName = com.Mars.Constants.SystemConstant.CNST_AUTOCHECK_MESSAGE_FILE_ROOT)]
    public class MarsAutoCheckErrorFile
    {
        [XmlElement(com.Mars.Constants.SystemConstant.CNST_AUTOCHECK_MESSAGE_ELEMENT)]
        public List<MarsAutoCheckErrorElement> AutoCheckErrorMessages {
            get;
            set;
        }
    }
    [Serializable]
    public class MarsAutoCheckErrorElement
    {
        [XmlAttribute(com.Mars.Constants.SystemConstant.CNST_AUTOCHECK_MESSAGE_ATTR_MSG)]
        public string AutocheckErrorMessage { get; set; }

        [XmlAttribute(com.Mars.Constants.SystemConstant.CNST_AUTOCHECK_MESSAGE_ATTR_TYPE)]
        public int Type { get; set; }
    }

}
