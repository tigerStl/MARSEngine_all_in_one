//using Mars.Inter.MQCenter.HttpRestService;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.DataLayer;
using Mars.message.Inter.MQCenter.HttpRestService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarsSpyTool.Utility.MarsThreads
{
    public enum enum_matching_threadStatus
    {
        _NotStart, 
        _SentRequest_to_svr, 
        _Waiting_for_response, 
        _done
    }

    internal class MatchingObjectNameThreads 
    {
        private Thread _mappingThread;

        public enum_matching_threadStatus thread_status = enum_matching_threadStatus._NotStart;
        //private RESTRecordReplayObjectMappingRequest _steps;//=new List<MarsRecordReplayStepBase>();

        internal void addObjectsToMapping(List<MarsRecordReplayStep> steps)
        {
            if (steps == null) return;
            //_steps = steps.Cast<MarsRecordReplayStepBase>().ToList();
            //_steps = new RESTRecordReplayObjectMappingRequest();
            //_steps.stepsToMatch = steps.Cast<MarsRecordReplayStepBase>().ToList();
        }

        internal void start()
        {
            _mappingThread = new Thread(new ThreadStart(RunMappingThread));
        }
        /// <summary>
        /// 需要从外部获得application的对象信息，如果有Id
        /// </summary>
        private void RunMappingThread()
        {
            string strError = "";
            bool isOk = false;
            //RESTRecordReplayObjectMappingResponse rspns = MarsRESTfulApiClient.MatchObjectForTestSteps(this._steps, ref strError, ref isOk);
        }
    }
}
