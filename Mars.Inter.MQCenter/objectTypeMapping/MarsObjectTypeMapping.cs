using System.Collections.Generic;
using System.Xml.Serialization;

namespace Mars.message.Inter.MQCenter.objectTypeMapping
{
    public class MarsObjectKeyword
    {
        public const string cnst_clickbutton    = "ClickButton";
        public const string cnst_clickat        = "ClickAt";
        public const string cnst_closewindow    = "CloseWindow";
        public const string cnst_captureValue   = "CaptureValue";
        public const string cnst_clickMenuIcon  = "ClickMenuIcon";
        public const string cnst_dismiss        = "Dismiss";
        public const string cnst_filledit       = "FillEdit";
        public const string cnst_pegwindow      = "PegWindow";
        public const string cnst_pressKeys      = "PressKeys";
        public const string cnst_selectdropdown = "SelectDropdown";
        public const string cnst_selectListItem = "SelectListItem";
        public const string cnst_selecttab      = "SelectTab";
        public const string cnst_selectMenuItem = "SelectMenuItem";
        public const string cnst_setbox         = "SetBox";
        public const string cnst_searchAndClick = "SearchAndClick";
        public const string cnst_waitForSeconds = "WaitForSeconds";

        public const string cnst_pauseRecord    = "_PauseRecordingAndReplay";


        public const string cnst_keyword_para_CURRENT_POS = "CURRENT_POS";
    }

    /// <summary>
    /// 该文件提供所有的对象类型mapping
    /// 样例文件：
    /// <MarsObjectTypeMappings>
    //    <ObjType MarsObjName = "SwfEdit" >
    //        < MarsMappingDetail >
    //            < SourceType > Summit.desktopEditor </ SourceType >
    //            < SourceAssemblyName > Summit.GUI.FrontDesk.dll </ SourceAssemblyName >
    //            < MarsOpAssemblyName > c:\automationTest\dlls\swfSummitEdit.dll</MarsOpAssemblyName>
    //            <MarsOpInitMethod>InitMarsEditForSummit</MarsOpInitMethod>
    //        </MarsMappingDetail>

    //    </ObjType>
    //</MarsObjectTypeMappings>
    /// </summary>
    /// 
    [XmlRoot(elementName: "MarsObjectTypeMappings")]
    public class MarsObjectTypeMappings
    {
        public const string cnst_swf_Checkbox   = "SwfCheckBox";
        public const string cnst_swf_Combobox   = "SwfCombobox";
        public const string cnst_swf_Button     = "SwfButton";
        public const string cnst_swf_edit       = "SwfEdit";
        public const string cnst_swf_Label      = "SwfLable";
        public const string cnst_swf_ListView   = "SwfListView";
        public const string cnst_swf_Menu       = "SwfMenu";

        public const string cnst_swf_Table      = "SwfTable";
        
        public const string cnst_swf_Tree       = "SwfTreeView";
        public const string cnst_swf_StatusBar  = "SwfStatusBar";
        public const string cnst_swf_ToolBar    = "SwfToolBar";
        public const string cnst_swf_Tab        = "SwfTab";        
        public const string cnst_swf_Window     = "SwfWindow";
        public const string cnst_swf_object     = "SwfObject";
        public const string cnst_swf_pegwindow  = "PegWindow";

        

        [XmlArray(ElementName = "ObjType")]
        public List<MarsObjTypeMapping> MarsObjTypes;

    }

    public class MarsObjTypeMapping
    {
        [XmlAttribute("MarsObjName")]
        public string MarsObjectName;
        [XmlArray("MarsMappingDetail")]
        public List<MarsMappingDetail> MappingDetails;
    }

    public class MarsMappingDetail
    {
        [XmlElement(ElementName = "SourceType")]
        public string ObjectSourceType;
        [XmlElement(ElementName = "SourceAssemblyName")]
        public string SourceAssemblyName;
        [XmlElement(ElementName = "MarsOpAssemblyName")]
        public string MarsOpAssemblyName;
        [XmlElement(ElementName = "MarsOpInitMethod")]
        public string MarsOpInitMethod;

    }
}
