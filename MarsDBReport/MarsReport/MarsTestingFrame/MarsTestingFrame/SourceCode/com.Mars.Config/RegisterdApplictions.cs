using com.Mars.Constants;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace com.Mars.Config
{
    /** demoe = <Application name="Summit 5.7" command="" path="" identifier="">
    </Application>
     * com.Mars.Config.RegisterdApplictions
     */    
    public class RegisterdApplictions:ConfigurationSection
    {
        //protected static MLogger Logger = MLogger.GetLogger(typeof(RegisterdApplictions));
        public RegisterdApplictions():base()
        {
            
        }
        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(ConfigTestApplicationCollection), AddItemName = "RegApplication")]
        public ConfigTestApplicationCollection RegApplications
        {
            get { //return (ConfigTestApplicationCollection)base["RegApplication"]??new ConfigTestApplicationCollection(); 
                return (ConfigTestApplicationCollection)this[""];
            }
            //set { base["RegApplication"] = value; }
        }               
    }

    public class ConfigTestApplication:ConfigurationElement{        
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_NAME, DefaultValue="Summit",IsKey=true,IsRequired=true)]
        public string AppName
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_NAME]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_NAME] = value; }
        }

        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_COMMAND, IsRequired = true)]
        public string Command
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_COMMAND]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_COMMAND] = value; }
        }

        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_PATH, IsRequired = true)]
        public string path
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_PATH]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_PATH] = value; }
        }

        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_IDENTIFIER, IsRequired = true)]
        public string identifier
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_IDENTIFIER]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_IDENTIFIER] = value; }
        }
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_APPLICATIONTYPE, DefaultValue = SystemConstant.CNST_APPCONFIG_APPREG_ATTR_APPLICATIONTYPE_DEFAULT)]
        public string AppliationType
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_APPLICATIONTYPE]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_APPLICATIONTYPE] = value; }
        }
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_OBJECTPATH)]
        public string ObjectPath
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_OBJECTPATH]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_OBJECTPATH] = value; }
        }
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA, DefaultValue = SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DEFAULT)]
        public string ExtraRequirement{
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA] = value; }
        }
        [ConfigurationProperty(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRAPOPUPMENU, DefaultValue = "1")]
        public string ExtraPopupMenu
        {
            get { return (string)this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRAPOPUPMENU]; }
            set { this[SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRAPOPUPMENU] = value; } 
        }


#if _Datafrom_Database
        public object Tag { get; set; }


#endif
        public void Update()
        {

        }

        
    }

    public class ConfigTestApplicationCollection : ConfigurationElementCollection
    {
        //protected readonly List<ConfigTestApplication> Applications=new List<ConfigTestApplication>();
        public ConfigTestApplicationCollection()
        {
        }
        protected override ConfigurationElement CreateNewElement()
        {
            var objElement = new ConfigTestApplication() ;
            //this.Applications.Add(objElement) ;
            return objElement ;
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigTestApplication)element).AppName;
        }
        
        public ConfigTestApplication this[int index]
        {
            get
            {
                return base.BaseGet(index) as ConfigTestApplication ;
            }
            set
            {
                if (index < this.Count)
                {
                    if (base.BaseGet(index) != null)
                    {
                        base.BaseRemoveAt(index);
                    }
                }
                if(value != null)
                    this.BaseAdd(index, value);
            }
        }

        public ConfigTestApplication GetSingle(String strShortNameIndex)
        {
            for (int i=0;i<this.Count;i++)
            {
                if (this[i] == null) continue;
                if (string.Compare(this[i].AppName, strShortNameIndex, true) != 0) continue;
                return this[i];
            }
            return null;
        }

    }
}
