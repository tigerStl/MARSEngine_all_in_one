extern alias clientWCF;
using com.Mars.Constants;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace com.Mars.KeyWords.KeyWordObject
{
    public class KeyWordsSection : ConfigurationSection
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MLogger));

        public KeyWordsSection()
            : base()
        {

        }
        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(KeyWordsConfigCollection), AddItemName = "Keyword")]
        public KeyWordsConfigCollection Keywords
        {
            get {
                return (KeyWordsConfigCollection)this[""];
            }
        }
    }

    public class KeyWordsConfigCollection : ConfigurationElementCollection
    {
        public KeyWordsConfigCollection ():base() 
        {

        }
        protected override ConfigurationElement CreateNewElement()
        {
            KeyWordsElement objNew = new KeyWordsElement();
            return objNew;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((KeyWordsElement)element).KeywordName;
        }

        public KeyWordsElement this[int index]
        {
            get { return base.BaseGet(index) as KeyWordsElement; }
            set {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
    }

    public class KeyWordsElement : ConfigurationElement
    {
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_KEY, IsKey=true, DefaultValue= SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_KEY_DEFAULT)]
        public string KeywordName
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_KEY]; }
            set { this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_KEY] = value; }
        }
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_APPLIEDAPPS, IsRequired= true)]
        public string AppliedApps
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_APPLIEDAPPS]; }
            set { this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_APPLIEDAPPS] = value; }
        }
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_RUNFROM, IsRequired = true)]
        public string RunFrom
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_RUNFROM]; }
            set { this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_RUNFROM] = value; }
        }
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_PARAM_PARSE)]
        public string ParseClass
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_PARAM_PARSE]; }
            set { this[SystemConstant.CNST_APPCONFIG_KEYWORDS_ATTR_PARAM_PARSE] = value; }
        }
    }
}
