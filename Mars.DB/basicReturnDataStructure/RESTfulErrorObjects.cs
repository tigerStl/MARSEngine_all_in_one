using Mars.message.Business;
using Mars.message.Dto;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MarsEngineSvc.basicReturnDataStructure
{

    [DataContract]
    public class RESTfulReturnedObjects : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<B_V_OBJECT_SNAPSHOT> Objects;
    }

    /// <summary>
    /// 请求参数，向服务器请求相关的对象的信息， 依据appld
    /// return 对象为RESTfulReturnedObjects
    /// </summary>
    [DataContract]
    public class RESTfulObjectsIdsAndAppIds_Ask : RESTfulReturnedObjects
    {
        [DataMember]
        public string appId;
        [DataMember]
        public IEnumerable<string> arrObjIds;
    }


    [DataContract]
    public class RESTfullReturnApplicationObjects : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<B_REGISTERED_APPS> AssignedObjects;
    }

    [DataContract]
    public class RESTfulReturnStoryboardObjects : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<V_STORYBOARD_TEST_FULLVISIONDTO> StoryboardDTOs;
    }

    [DataContract]
    public class RESTfulReturnLastMarkIdObjects : RESTfulReturnObjects
    {
        [DataMember]
        public long LastMarkId;
    }

    [DataContract]
    public class RESTfulReturnedVTestCaseSteps : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<V_TEST_STEPS_FULLVISIONDTO> TestStepsForTestcase;
    }

    [DataContract]
    public class RESTfulReturnedTestData : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<MarsRESTKeyValuePair<long?, TEST_DATA_SETTINGDTO>> TestDataSetWithDBSetId;
    }

    [DataContract]
    public class RESTfulReturnedSeqNumber : RESTfulReturnObjects
    {
        [DataMember]
        public long SeqNumber;
    }



    [DataContract]
    public class RESTfulReturnedKeywords : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<MarsRESTKeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>> KeywordsWithDicRel;
    }

    [DataContract]
    public class RESTfulReturnedSystemLookup : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<B_SYSTEM_LOOKUP> SystemLookups;
    }
    [Serializable]
    [DataContract]
    public class RESTfulReturnedTestReportSteps : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<T_TEST_REPORT_STEPSDTO> TestReportSteps;
    }

    [Serializable]
    [DataContract]
    public class RESTfulReturnedTestReport : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<T_TEST_REPORTDTO> TestReports;
    }

    [Serializable, DataContract]
    public class RESTfulBTestReport : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<B_TEST_REPORT> TestReports;
    }


    [Serializable, DataContract]
    public class MarsRESTfulCaptureDataInfo
    {
        [DataMember]
        public long? ReportId { get; set; }
        [DataMember]
        public long? StepId { get; set; }
        [DataMember]
        public DateTime? beginTime { get; set; }
        [DataMember]
        public DateTime? endTime { get; set; }
        [DataMember]
        public short successId { get; set; }
        [DataMember]
        public IEnumerable<MarsRESTKeyValuePair<string, string>> objectNameAndValues { get; set; }
        [DataMember]
        public long? dataSummaryId { get; set; }
        [DataMember]
        public string objectNameIdx { get; set; }
        [DataMember]
        public string runningError { get; set; }

        public MarsRESTfulCaptureDataInfo(long? lRptId, long? stpId, DateTime? bgnTime,
            DateTime? endTm,
            short iSuccessId, List<KeyValuePair<string, string>> lstObjectNameAndValues,
            long? dATA_SUMMARY_ID, string strObjectNameIdx,
            string strRunningError)
        {
            ReportId = lRptId;
            StepId = stpId;
            beginTime = bgnTime;
            endTime = endTm;    
            successId = iSuccessId;
            objectNameAndValues = new MarsRESTKeyValuePair<string, string>().copyFromList(lstObjectNameAndValues);
            dataSummaryId = dATA_SUMMARY_ID;
            objectNameIdx = strObjectNameIdx;
            runningError = strRunningError;
        }
    }

    [Serializable]
    [DataContract]
    public class RESTfulReturnedCaptureData : RESTfulReturnObjects
    {
        [DataMember]
        public MarsRESTfulCaptureDataInfo CapturedData;
    }

    [Serializable]
    [DataContract]
    public class RESTfulTestReportStepOperation : RESTfulReturnObjects
    {
        [DataMember]
        public int OperationId;

        [DataMember]
        public int ResultNumber;
    }

    [Serializable, DataContract]
    public class RESTfulProjTestResult : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<B_PROJ_TEST_RESULT> Proj_Test_Results;
    }

    [Serializable, DataContract]
    public class RESTfulStoryboardTestFullVison : RESTfulReturnObjects
    {
        [DataMember]
        public string actionInfo;
        [DataMember]
        public string action2Info;
        [DataMember]
        public IEnumerable<V_STORYBOARD_TEST_FULLVISIONDTO> storyboardTestFullVisions;
    }

    [Serializable, DataContract]
    public class RESTfulStoryboardBasicInfo : RESTfulReturnObjects
    {
        [DataMember]
        public IEnumerable<T_STORYBOARD_SUMMARYDTO> storyboardInfos;
        [DataMember]
        public T_REGISTERED_APPSDTO applicationInfo;
    }

    [Serializable, DataContract]
    public class RESULTTQueryInfo : RESTfulReturnObjects
    {
        [DataMember]
        public B_QUERY DataSourceInfo;
    }


    [Serializable, DataContract]
    public class RESTfulMappingObjectsRequest: RESTfulReturnObjects
    {
        [DataMember]
        public List<V_TEST_STEPS_FULLVISIONDTO> steps { get; set; }
  
        [DataMember]
        public long application { get; set; }
    }

    [Serializable, DataContract]
    public class RESTfulSaveTestCasesRequest: RESTfulMappingObjectsRequest
    {
        [DataMember]
        public string TestCaseName { get; set; }
        [DataMember]
        public string TestCaseDesc { get; set; }

        public RESTfulSaveTestCasesRequest() : base()
        {
            objectType = (int)RESTfulObjectType._save_teststep_request;
        }
    }


    [DataContract]
    public class RESTfulHardDiskSeq
    {
        [DataMember]
        public bool IsSuccess { get; set; } = false;
        [DataMember]
        public int objectType { get; set; } = (int)RESTfulObjectType._hardDisk_volumn_response_ok;
        [DataMember]
        public string? HardDiskIdx { get; set; }
        [DataMember]
        public string? HardDiskSeria { get; set; }
        /// <summary>
        /// error messge will set if IsSucess is false
        /// </summary>
        [DataMember]
        public string? Message { get; set; }
    }

    #region 2025-6-25wei
    /// 为网页版添加，
    /// 
    [DataContract]
    public class RESTfulProjectResponse : RESTfulReturnObjects
    {
        [DataMember]
        public long? applicatinId { get; set; }
        [DataMember]
        public List<T_TEST_PROJECTDTO>? projectDtos { get; set; }
    }

    [DataContract]
    public class RESTfulTestSuiteResponse : RESTfulReturnObjects
    {
        [DataMember]
        public long? projectId { get; set; }
        [DataMember]
        public List<T_TEST_SUITEDTO>? testsuites { get; set; }
    }
    [DataContract]
    public class DatabaseConfigItem
    {
        public string? dbIdxName { get; set; }
        public string? databaseType { get; set; }
        public string? hostName { get; set; }

        public override string ToString()
        {
            return $"{dbIdxName}|{databaseType}|{hostName}";
        }
    }

    [DataContract]
    public class RESTfulMarsConfigDatabaseResponse : RESTfulReturnObjects
    {
        public RESTfulMarsConfigDatabaseResponse() : base()
        {
            this.objectType = (int)RESTfulObjectType._getMarsConfigDatabase_response_ok;
        }
        public List<DatabaseConfigItem>? DatabaseIdx { get; set; }
    }

    //[DataContract]
    //public class RESTfulMarsCreateOrSaveTCWithStepsFromWeb : RESTfulReturnObjects
    //{
    //    //public long? testcaseId { get; set; }
    //    public WsAskForSaveTestInMainWindow? testcaseInfoFromSpy { get; set; }
    //    public RESTfulMarsCreateOrSaveTCWithStepsFromWeb() : base()
    //    {
    //        this.objectType = (int)RESTfulObjectType._createOrSaveTC_response_ok;
    //    }
    //}

    //public class RESTfulMarsCreateOrUpdateDatasetFromWeb : RESTfulReturnObjects
    //{
    //    [DataMember]
    //    public WsCreateDatasetInMainWindow? testDataSet { get; set; }
    //    public RESTfulMarsCreateOrUpdateDatasetFromWeb() : base()
    //    {
    //        this.objectType = (int)RESTfulObjectType._createOrUpdateDataset_response_ok;
    //    }
    //}
    #endregion


    #region image objects
    public class GetObjectImagePathRequest
    {
        public string? DbIdx { get; set; }
        public long? ObjectId { get; set; }
        public string? ObjectName { get; set; }
        public long? ApplicationId { get; set; }
    }


    [Serializable, DataContract]
    public class RESTfulImageObjects : RESTfulReturnObjects
    {
        [DataMember]
        public long? ApplicationId { get; set; }

        [DataMember]
        public object? Data { get; set; }
        [DataMember]
        public string? imagePath { get; set; } //= relativePath,
        [DataMember]
        public string? imageData { get; set; } //= dataUrl,
        [DataMember]
        public string? base64Data { get; set; } //= base64String,
        [DataMember]
        public string? mimeType { get; set; } //= mimeType,
        [DataMember]
        public string? fileName { get; set; } //= Path.GetFileName(latestFile),
        [DataMember]
        public int? fileSize { get; set; } //= imageBytes.Length
        [DataMember]
        public long ObjectId { get; set; }
        [DataMember]
        public string ObjectName { get; set; }
    }
    #endregion
}