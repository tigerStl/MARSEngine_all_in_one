using Mars.AutoTestingDriver.injector;
using Mars.AutoTestingDriver.MarsMessageCenter;
using Mars.Inter.MQCenter.interProcess;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.interProcess;
using MarsCore.MessageCenter;
using NLog;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.MarsImage
{
    public class MarsImageKeywordsEntry
    {

       
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsImageKeywordsEntry));

        /// <summary>
        /// 进行图片模式的处理。目前支持fillEdit和clickbutton。主要的步骤如下：
        /// 1，判断objectType是否是图片模式的类型，
        /// 2，判断图片是否存在本地(messagecenter 处理）
        /// 3，判断messagecenter是否启动，如果没有启动，则直接返回失败
        /// 4，将请求用httpclient发送到messagecenter，/dealWithImagePattern,内容包括，keyword，objectname，定位信息，数据，parameter
        /// 5，将返回的结果进行处理
        /// </summary>
        /// <param name="objGUIKeyWordMessage"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool DealWithImagePattern(MarsMessageKeywordOpObjectInfo objGUIKeyWordMessage, ref string strError, ref MARSDealResult dealResult)
        {
            Logger.Info("DealWithImagePattern", $"keywordInfo:{objGUIKeyWordMessage.StepId}|{objGUIKeyWordMessage.GetCurrentTestStepInfo().Keyword}");
            var stepInfo = objGUIKeyWordMessage.GetCurrentTestStepInfo();
            if (stepInfo == null)
            {
                strError = "StepInfo is null";
                dealResult.ResultMessage=$"FAILED,{strError}";
                dealResult.ErrorMessage = strError;
                return false;
            }
            //判断objectType是否是图片模式的类型
            // obejctType的key中应该有SwfImageFile，EditOffsetX等 
            if (!IsValidateImagePatternMode(stepInfo.TestStepObjectInformation.TargetObject.ObjectIDPropertiesAndValue))
            {
                strError = $"StepInfo is not image pattern mode,objectType:{stepInfo.TestStepObjectInformation.ObjectType}";
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.ErrorMessage = strError;
                return false;
            }

            // 发送消息去messagecenter处理            
            string strSourceFileName = "";
            bool isOk = MarsMessageCenterStub.DoKeywordsByImageStepInfo(objGUIKeyWordMessage.GetCurrentTestStepInfo(),ref strSourceFileName, ref strError);
            if (!isOk)
            {
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.ErrorMessage = strError;
                return false;
            }
            dealResult.ResultMessage = "SUCCESS";
            dealResult.AckTime = DateTime.Now;
            dealResult.ActualInputData = strSourceFileName;
            return true;
        }

        private static string[] cnst_patterKeys = new string[] { MarsImagePatternConst.CNST_IMAGE_PATTERN_ID_SWFIMAGE_FILE, MarsImagePatternConst.CNST_IMAGE_PATTERN_ID_SWFIMAGE_STREAM };

        private static bool IsValidateImagePatternMode(MarsDictionary testStepObjectInformation)
        {
            //判断objectType是否是图片模式的类型
            // obejctType的key中应该有SwfImageFile，EditOffsetX等
            if (testStepObjectInformation == null)
            {
                Logger.Error("IsValidateImagePatternMode", "testStepObjectInformation is null");    
                return false;
            }
            if (testStepObjectInformation.Items == null)
            {
                Logger.Error("IsValidateImagePatternMode", "testStepObjectInformation.Items is null");
                return false;
            }
            if (testStepObjectInformation.Items.Count == 0)
            {
                Logger.Error("IsValidateImagePatternMode", "testStepObjectInformation.Items is empty");
                return false;
            }
            
            if (!testStepObjectInformation.Items.Any(p=>(p!=null)&& (p.Key != null) 
                && (cnst_patterKeys.Any(z=>z.Equals(p.Key, StringComparison.OrdinalIgnoreCase)))))
            {
                Logger.Error("IsValidateImagePatternMode", $"testStepObjectInformation.Items is not contain {string.Join(",", cnst_patterKeys)}");
                return false;
            }

            return true;
        }

        internal static bool IsImagePatterMode(string strObjType)
        {            
            return MarsImagePatternConst.CNST_IMAGE_PATTERN_TYPE_LIST.Any(x => x.Equals(strObjType, StringComparison.OrdinalIgnoreCase));
        }
    }
}
