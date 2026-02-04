using MarsEngine.MarsSocketSvc;
#if !_sub_dll
using OpenQA.Selenium;
#endif
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;
using System;
using Mars.Inter.MQCenter.objectEngine;
//using System.Security.Cryptography.Xml;

namespace Mars.webSupport
{

    /// <summary>
    /// 获得MarsWebAGGrid的列信息
    /// </summary>
    public class MarsWebAGGridColumns
    {
        /// <summary>
        /// 显示为MARSType, 如swfEdit， 不过通常为WebAGGridColumn，或者其他WebXXXGridColumn
        /// </summary>
        public string marsType { get; set; }
        /// <summary>
        /// MarsWebAGGrid的列配置中推荐的keyword
        /// </summary>
        public string marsKeyword { get; set; }
        public string marsTag { get; set; } // 该列的tag
        public List<string> marsColumnNames { get; set; }
        public string marsXPath { get; set; }
        public string marsHeaderProperty { get; set; } // 获取头部text的属性 比如col-id, webColumnHeaderTextProperty
    }

    /// <summary>
    /// 该对象用来保存selenium对象的xpath信息，研究
    /// </summary>
    public class MARSWebElementXpathInfo
    {
        //private const string CNST_TYP_TITLE = "webTitle";
        //private const string CNST_WEBID = "webId";
        //private const string CNST_WEBNAME = "webName";
        //private const string CNST_WEBCLASS = "webClass";
        //private const string CNST_XPATH = "webXPath";
        //private const string CNST_VALUE = "WebValue";
        //private const string CNST_TAG = "webTag";
        //private const string CNST_INNERHTML = "webInnerHTML";
        //private const string CNST_FRAME = "webFrame";
        //private const string CNST_FRAME_BYNAME = "webFrameName";
        //private const string CNST_CSSSELECTOR = "webCSSSelector";
        //private const string CNST_WEBREPLACETEXT = "webReplaceText";// 格式，其中用data替换::?:: 示例如下 //td[text()='::?::'])[1]
        //private const string CNST_RESERVE_FRAME_ROOT = "MARS_ROOT";//用来switch to root
        //private const string CNST_WebURL = "WebUrl";

#if !_sub_dll
        [JsonIgnore]
        public IWebElement webElement { get; set; } = null;
#endif
        public string elementId { get; set; } = null;
        public string webClassInfo { get; set; } = null;
        public string marsObjectType { get; set; } = null; //swfedit.....
        public string webXpath { get; set; } = null;
        public string webTag { get; set; } = null;
        public string webName { get; set; } = null;
        public bool isDisplayed { get; set; }
        public string webId { get; set; } = null;
        public string webFrame { get; set; } = null;
        public string webTitle { get; set; } = null;
        public string webClass { get; set; } = null;
        
        public string webInnerHTML { get; set; } = null;
        public string webFrameName { get; set; } = null;
        public string webCSSSelector { get; set; } = null;
        public string webReplaceText { get; set; } = null;  //// 格式，其中用data替换::?:: 示例如下 //td[text()='::?::'])[1]
        public string webReserveFrameRoot { get; set; } = null; //用来switch to root
        public string webUrl { get; set; } = null; //用来switch to root
        public string data { get; set; }

        public static string BuildUILocator(string webXpath, string webId, string webName, string webFrame = "", 
            string webTitle = "", string webClass = "", string innerHtml = "", string frameName = "", string cssSelector = "",
            string replaceText = "", string reseverFrameRoot="")
        {
            List<string> uiLocator = new List<string>();    
            if (!string.IsNullOrEmpty(webXpath))
            {
                uiLocator.Add($"WebXpath:={webXpath}");                
            }
            if (!string.IsNullOrEmpty(webId))
            {
                uiLocator.Add($"WebId:={webId}");
            }
            if (!string.IsNullOrEmpty(webName))
            {
                uiLocator.Add($"SwfName:={webName}");
            }
            if (!string.IsNullOrEmpty(webFrame))
            {
                uiLocator.Add($"WebFrame:={webFrame}");
            }
            if (!string.IsNullOrEmpty(webTitle))
            {
                uiLocator.Add($"WebTitle:={webTitle}");
            }
            if (!string.IsNullOrEmpty(webClass))
            {
                uiLocator.Add($"WebClass:={webClass}");
            }
            if (!string.IsNullOrEmpty(innerHtml))
            {
                uiLocator.Add($"WebInnerHTML:={innerHtml}");
            }
            if (!string.IsNullOrEmpty(frameName))
            {
                uiLocator.Add($"WebFrameName:={frameName}");
            }
            if (!string.IsNullOrEmpty(cssSelector))
            {
                uiLocator.Add($"WebCSSSelector:={cssSelector}");
            }
            if (!string.IsNullOrEmpty(replaceText))
            {
                uiLocator.Add($"WebReplaceText:={replaceText}");
            }
            if (reseverFrameRoot != null)
            {
                uiLocator.Add($"WebReserveFrameRoot:={reseverFrameRoot}");
            }
            return string.Join("\r\n", uiLocator);
        }
        public static string BuildUILocator(MARSWebElementXpathInfo webElement)
        {
            return BuildUILocator(webElement.webXpath, webElement.webId, webElement.webName, webElement.webFrame, 
                webElement.webTitle, webElement.webClass, webElement.webInnerHTML,
                webElement.webFrameName, webElement.webCSSSelector, webElement.webReplaceText, webElement.webReserveFrameRoot);
        }

        public static MARSWebElementXpathInfo FromUILocator(string strUILocator)
        {
            MARSWebElementXpathInfo webElement = new MARSWebElementXpathInfo();
            string[] uiLocator = strUILocator.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in uiLocator)
            {
                if (item.StartsWith("WebXpath:=",StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webXpath = item.Substring("WebXpath:=".Length);
                }
                else if (item.StartsWith("WebId:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webId = item.Substring("WebId:=".Length);
                }
                else if (item.StartsWith("SwfName:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webName = item.Substring("SwfName:=".Length);
                }
                else if (item.StartsWith("WebFrame:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webFrame = item.Substring("WebFrame:=".Length);
                }
                else if (item.StartsWith("WebTitle:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webTitle = item.Substring("WebTitle:=".Length);
                }
                else if (item.StartsWith("WebClass:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webClass = item.Substring("WebClass:=".Length);
                }
                else if (item.StartsWith("WebInnerHTML:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webInnerHTML = item.Substring("WebInnerHTML:=".Length);
                }
                else if (item.StartsWith("WebFrameName:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webFrameName = item.Substring("WebFrameName:=".Length);
                }
                else if (item.StartsWith("WebCSSSelector:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webCSSSelector = item.Substring("WebCSSSelector:=".Length);
                }
                else if (item.StartsWith("WebReplaceText:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webReplaceText = item.Substring("WebReplaceText:=".Length);
                }
                else if (item.StartsWith("WebReserveFrameRoot:=", StringComparison.OrdinalIgnoreCase))
                {
                    webElement.webReserveFrameRoot = item.Substring("WebReserveFrameRoot:=".Length);
                }
            }
            
            return webElement;
        }

        public Dictionary<string, string> BuildDictionary()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(webXpath))
            {
                dic.Add("WebXpath", webXpath);
            }
            if (!string.IsNullOrEmpty(webId))
            {
                dic.Add("WebId", webId);
            }
            if (!string.IsNullOrEmpty(webName))
            {
                dic.Add("SwfName", webName);
            }
            if (!string.IsNullOrEmpty(webFrame))
            {
                dic.Add("WebFrame", webFrame);
            }
            if (!string.IsNullOrEmpty(webTitle))
            {
                dic.Add("WebTitle", webTitle);
            }
            if (!string.IsNullOrEmpty(webClass))
            {
                dic.Add("WebClass", webClass);
            }
            if (!string.IsNullOrEmpty(webInnerHTML))
            {
                dic.Add("WebInnerHTML", webInnerHTML);
            }
            if (!string.IsNullOrEmpty(webFrameName))
            {
                dic.Add("WebFrameName", webFrameName);
            }
            if (!string.IsNullOrEmpty(webCSSSelector))
            {
                dic.Add("WebCSSSelector", webCSSSelector);
            }
            if (!string.IsNullOrEmpty(webReplaceText))
            {
                dic.Add("WebReplaceText", webReplaceText);
            }
            if (!string.IsNullOrEmpty(webReserveFrameRoot))
            {
                dic.Add("WebReserveFrameRoot", webReserveFrameRoot);
            }
            if (!string.IsNullOrEmpty(webUrl))
            {
                dic.Add("WebUrl", webUrl);
            }
            return dic;
        }
    }

    public class MarsResponseWebObjects : MarsWebAPIPacket
    {
        public MarsResponseWebObjects() : base()
        {
            PacketType = MarsSocketSvcConstant.webapi_packet_type_reponse_webobject;
        }
        public List<MARSWebElementXpathInfo> webObjectList { get; set; }
        public List<MarsWebAGGridColumns> webAllColoumns { get; set; } = new List<MarsWebAGGridColumns>();
    }


    public static class MarsWebAPIPacketFactory
    {
        public static MarsWebAPIPacket? CreatePacket(string requestBody)
        {
            var jsonDocument = JsonDocument.Parse(requestBody);
            var root = jsonDocument.RootElement;

            if (root.TryGetProperty("PacketType", out JsonElement packetTypeElement))
            {
                var packetType = packetTypeElement.GetString();

                switch (packetType)
                {
                    case MarsSocketSvcConstant.webapi_packet_type_request_shakehand:
                        return JsonSerializer.Deserialize<MarsWebApiShakeHandRequest>(requestBody);
                    // Add more cases here for other packet types
                    case MarsSocketSvcConstant.webapi_packet_type_reponse_shakehand:
                        return JsonSerializer.Deserialize<MarsWebApiShakeHandResponse>(requestBody);
                    case MarsSocketSvcConstant.webapi_packet_type_request_webobject:
                        return JsonSerializer.Deserialize<MarsRequestWebObjects>(requestBody);
                    case MarsSocketSvcConstant.webapi_packet_type_reponse_webobject:
                        return JsonSerializer.Deserialize<MarsResponseWebObjects>(requestBody);
                    case MarsSocketSvcConstant.webapi_packet_type_reponse_highlight:
                        return JsonSerializer.Deserialize<MarsResponseHightlightWebObject>(requestBody);
                    case MarsSocketSvcConstant.webapi_packet_type_request_highlight:
                        return JsonSerializer.Deserialize<MarsReqHighLightWebObject>(requestBody);
                    case MarsSocketSvcConstant.webapi_packet_type_request_execute:
                        return JsonSerializer.Deserialize<MarsRequestExecuteTestStep>(requestBody);
                    case MarsSocketSvcConstant.webapi_packet_type_reponse_execute:
                        return JsonSerializer.Deserialize<MarsResponseExecuteTestStep>(requestBody);
                    case MarsSocketSvcConstant.webapi_packet_type_request_imagemode_execute:
                        return JsonSerializer.Deserialize<MarsImageModeTestStepRequest>(requestBody);
                    default:
                        return null;
                }
            }
            else
            {
                throw new InvalidOperationException("PacketType is missing in the request body.");
            }
        }
    }
}
