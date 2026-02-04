using com.Mars.Config;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsTestFrame.plugins
{
    public class MarsPluginsLoader: ConfigurationSection
    {
        internal const string CNST_PROPERTY_TYPE = "Type";
        internal const string CNST_PROPERTY_NAME = "Name";
        internal const string CNST_PROPERTY_PATH = "Path";
        protected static MLogger Logger = MLogger.GetLogger(typeof(MarsPluginsLoader));
        public MarsPluginsLoader():base()
        {

        }
        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(ConfigTesMarsPluginsCollection), AddItemName = "MarsPlugin")]
        public ConfigTesMarsPluginsCollection MarsPlugins
        {
            get
            {
                return (ConfigTesMarsPluginsCollection)this[""];
            }
        }
    }

    public class ConfiguablePlugins : ConfigurationElement
    {
        ///<MarsPlugin Type="MarsPlugin-TestSteps" Name="TestPlugins">
        ///<AssemblyPath>C:\automationTest\Automation Workbooks\dlls\TestPlugins.dll</AssemblyPath>    
        ///</MarsPlugin>
        [ConfigurationProperty(MarsPluginsLoader.CNST_PROPERTY_TYPE, DefaultValue = "MarsPlugin-TestSteps", IsKey = false, IsRequired = true)]
        public string PluginType
        {
            get { return (string)this[MarsPluginsLoader.CNST_PROPERTY_TYPE]; }
            set { this[MarsPluginsLoader.CNST_PROPERTY_TYPE] = value; }
        }
        [ConfigurationProperty(MarsPluginsLoader.CNST_PROPERTY_NAME, DefaultValue = "Test", IsKey = true, IsRequired = true)]
        public string PluginName
        {
            get { return (string)this[MarsPluginsLoader.CNST_PROPERTY_NAME]; }
            set { this[MarsPluginsLoader.CNST_PROPERTY_NAME] = value; }
        }
        [ConfigurationProperty(MarsPluginsLoader.CNST_PROPERTY_PATH, DefaultValue = ".\\", IsKey = false, IsRequired = true)]
        
        public string PluginPath
        {
            get { return (string)this[MarsPluginsLoader.CNST_PROPERTY_PATH]; }
            set { this[MarsPluginsLoader.CNST_PROPERTY_PATH] = value; }
        }
    }

    public class ConfigTesMarsPluginsCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            var itmNew = new ConfiguablePlugins();
            return itmNew;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfiguablePlugins)element).PluginName;
        }
    }
}
