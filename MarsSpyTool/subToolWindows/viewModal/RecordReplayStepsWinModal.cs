using Mars.message.Utility;
using MarsSpyTool.objectFileMonitor;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.message.Inter.MQCenter.HttpRestService;
using System.Collections.Specialized;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using NLog.Targets.Wrappers;
using MarsSpyTool.httpSvc;
using Mars.message.ViewModel;
using Mars.message.Inter.MQCenter.interProcess.HttpRestService;
using MarsSpyTool.Utility.MarsThreads;
using Mars.message.Inter.MQCenter.objectTypeMapping;
using Mars.message.Inter.MQCenter.interProcess.HttpRestService.SvcMode;
using MarsSpyTool.message.Inter.MQCenter.interProcess.HttpRestService.client;
using System.Windows.Media;
using MarsSpyTool.subToolWindows.replayTestStep;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using MarsSpyTool.subToolWindows.testStepEditor;
using System.Windows.Forms.VisualStyles;
using System.Data.SqlClient;
using System.Threading;
using MarsSpyTool.nonGuiKeywordOp;

namespace MarsSpyTool.subToolWindows.viewModal
{
    internal class RecordReplayStepsWinModal: INotifyPropertyChanged
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");

        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<string> _stepsListBox;
        private List<MarsRecordReplayStep> _recordedStepsFromFile = null;

        public ICommand _save2MarsClick;
        public ICommand save2MarsClick { get => _save2MarsClick; }
        public ICommand replayMarsClick { get => _replayMarsClick; }
        private ICommand _replayMarsClick;

        private ICommand _RemoveCommand;
        public ICommand RemoveCommand { 
            get { 
                if (_RemoveCommand == null)
                {
                    _RemoveCommand = new RelayCommand(OnRowRemoveButtonClicked);
                }
                return _RemoveCommand;
            } 
        }

        public ICommand EditStepsInEditor { get => _editStepsInEditor; }
        private ICommand _editStepsInEditor;

        public ObservableCollection<string> stepsListBox
        {
            get { return _stepsListBox; }
            set
            {
                _stepsListBox = value;
                RaisePropertyChanged("stepsListBox");
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(stepsListBox)));
            }
        }

        private ObservableCollection<MarsRecordReplayStep> _testSteps = new ObservableCollection<MarsRecordReplayStep>();
        public ObservableCollection<MarsRecordReplayStep> TestSteps
        {
            get
            {
                return _testSteps;
            }
            set
            {
                if (_testSteps != value)
                {
                    _testSteps = value;
                    RaisePropertyChanged("TestSteps");
                }
            }
        }

        public bool whenTestStepGeneratedImpl(List<MarsRecordReplayStep> steps, ref string strError)
        {
            logger.Info("whenTestStepGeneratedImpl\tBegin");
            try
            {
                if (steps == null) return true;
                if (steps.Count <= 0) return true;

                foreach (var itm in steps) {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (itm != null)
                        {
                            this.TestSteps.Add(itm);
                        }
                    }
                    );
                }
                /// 启动线程，匹配对象名词
                /// 
                MatchingObjectNameThreads thrd = new MatchingObjectNameThreads();
                thrd.addObjectsToMapping(steps);
                thrd.start();
                return true;
            }catch(Exception ex)
            {
                logger.Error(ex, $"whenTestStepGeneratedImpl\t|{ex.Message}\r\n|");
                return false;
            }
            finally
            {
                logger.Info("whenTestStepGeneratedImpl\tEnd");
            }
        }


        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        private string currentStepJsonFilePath = null;
        protected void ReadStepJsonFileToList(ref bool isOk, ref string strError)
        {
            if (!System.IO.File.Exists(currentStepJsonFilePath))
            {
                logger.Error($"no such file |{currentStepJsonFilePath}| exist");
                return;
            }
            using (StreamReader file = File.OpenText(this.currentStepJsonFilePath))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o2 = (JObject)JToken.ReadFrom(reader);
                }
        }

        private string _currentStatus;
        public string currentStatus
        {
            get { return this._currentStatus; }
            set
            {
                this._currentStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(currentStatus)));
            }
        }

        private void OnRowRemoveButtonClicked(object parameter)
        {
            if (parameter == null) return;
            if (!(parameter is MarsRecordReplayStep)) return;
            TestSteps.Remove((MarsRecordReplayStep)parameter);
            /// send message to replay remove
            /// 
            if (replayClient == null) return;
            replayClient.doRecordReplayRemoveTestStep(((MarsRecordReplayStep)parameter).runOrder);
        }

        private static List<string> NonGuiKeywords = new List<string>() { MarsObjectKeyword.cnst_waitForSeconds, "KILLAPPLICATION", "STARTAPPLICATION", "RESUMENEXT", "RESUMENEXT" };
        public RecordReplayStepsWinModal()
        {
            stepsListBox = new ObservableCollection<string>();
            //stepsListBox.CollectionChanged += lstBox_CollectionChanged;
            GetStepJsonFilePath();

            //stopButtonClick = new RelayCommand(param=>stopButtonClickImpl());
            _save2MarsClick = new DelegateCommand(() => { save2MarsClickImpl(); });
            _replayMarsClick = new DelegateCommand(() => { replayMarsClickImpl(); });
            _editStepsInEditor = new DelegateCommand(() => {
                editStepsInEditorImpl();
            });

            //RemoveCommand = new RelayCommand(OnRowRemoveButtonClicked);

            /// start file monitor
            /// 
            objectFileMonitor.MarsFileMonitor marsFileMonitor = objectFileMonitor.MarsFileMonitor.InitMonitor(
                stepFileChangeImpl,stepFileCreateImpl, stepFileDeleteImpl, MarsConstants.CNST_SYPTOOL_STEPS_FILENAME
                );
            //marsFileMonitor.
            //marsFileMonitor.dealFileCreateHandler+=

            /// 设置step确认的回调函数
            //MarsSpyRESTfulNetServer.whenTestStepGeneratedHandle = whenTestStepGeneratedImpl;
        }
        #region replay member section
        private RESTClient2MessageCenter replayClient = null;
        /// <summary>
        /// 用于replay的线程
        /// </summary>
        private System.Threading.Thread replayThread = null;
        private bool replayThreadContinueMonitor = true;
        #endregion replay member section
        private void replaySteps()
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|replaySteps\tbegin");
            /// 应该通过一个线程来执行？
            /// 
            int iStpIdx = 0;
            replayThreadContinueMonitor = true;
                    
            if (SelectedTestStep != null)
            {
                int idxSelected = TestSteps.IndexOf(SelectedTestStep);
                if (idxSelected > 0)
                    iStpIdx = idxSelected + 1;
            }
            string strError = "";
            while ((iStpIdx < TestSteps.Count) && (replayThreadContinueMonitor))
            {
                try {

                    var itm = TestSteps[iStpIdx];
                    if (itm == null) continue;
                    bool isOk = true;
                    if (string.IsNullOrEmpty(itm.keyWord))
                    {
                        continue;
                    }
                    /// None gui keywords
                    if (itm.keyWord.Equals(MarsObjectKeyword.cnst_waitForSeconds, StringComparison.OrdinalIgnoreCase))
                    {
                        isOk = (new WaitForSecondsKeyword()).doKeyword(itm.Parameter, itm.opText, ref strError);
                    }
                    else
                    {   /// create a post to 
                        /// 
                        string strSteps = System.Text.Json.JsonSerializer.Serialize(itm);
                        isOk = replayClient.doTestStepReplayViaStepString(strSteps, ref strError);
                    }
                    if (!isOk)
                    {
                        // 将该行背景改为红色
                        // 弹出对话框，显示详细内容，包括重试，
                        if (SetTestStepExecuteStatusColor != null)
                        {
                            SetTestStepExecuteStatusColor(itm, Brushes.OrangeRed);
                        }
                        ReplyTestStepInfoForm frm = new ReplyTestStepInfoForm();
                        frm.loadReplayTestStep(itm);
                        frm.setStatusMessage(strError);
                        frm.ShowDialog();
                        switch (frm.ReplayAction)
                        {
                            case MarsTestStep_replay_action.saveAndRetry:
                            case MarsTestStep_replay_action.StopReplay: 
                                return;
                            case MarsTestStep_replay_action.Ignore:
                                iStpIdx += 1;
                                continue;
                            case MarsTestStep_replay_action.Retry:
                                iStpIdx -= 1;
                                continue;
                            default:
                                return;
                        }
                    }
                    else
                    {
                        // 将改行前景改为蓝色
                        if (SetTestStepExecuteStatusColor != null)
                        {
                            SetTestStepExecuteStatusColor(itm, Brushes.LightBlue);
                        }
                    }
                }
                finally
                {
                    iStpIdx += 1;
                }
            }
            
            logger.Info($"{iMark}|replaySteps\tEnd");
        }

        private bool IsRecordAndReplaySvcRunning()
        {
            RESTClient2MessageCenter clnt = new RESTClient2MessageCenter();
            bool isOk = false;
            string strError = "";
            var replayRecordStatus = clnt.QueryRecordReplayStatus(ref isOk, ref strError);
            if ((!isOk) || (replayRecordStatus == null))
            {
                MessageBox.Show($"Can't get service's status, please try later", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            if (replayRecordStatus.IsRunning)
            {
                MessageBox.Show($"Please stop recording first.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            return false;
        }

        /// 加载端口文件
        /// 
        private bool LoadSvcConfigFile()
        {
            string dir = typeof(RecordReplayStepsWinModal).Assembly.Location;
            dir = System.IO.Path.GetDirectoryName(dir);
            string swapFileName = System.IO.Path.Combine(dir, MarsRestFulCnst.cnst_SwapDir, MarsRestFulCnst.cnst_port_swapfile);
            string strError = "";
            if (!RestServiceInfo.Instance().loadFromFile(swapFileName, ref strError))
            {
                MessageBox.Show($"Can't open service config file, please try later.", "Message", MessageBoxButton.OK);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 将teststep发送到editorwindow
        /// </summary>
        private void editStepsInEditorImpl()
        {

            /// 通过web socket判断是否已经停止了record
            /// 
            if (!LoadSvcConfigFile()) {
                return;
            }
            if (IsRecordAndReplaySvcRunning())
            {
                return;
            }

            MARSTestStepEditorModel stepEditorModel = new MARSTestStepEditorModel();
            //stepEditorModel.SelectedApplication = appConfirmModal.SelectedApplication;
            //stepEditorModel.TargetDBIdx = this.currentMarsDBIdx;
            // 创建编辑区
            TestStepEditorForSpyer stepEditor = TestStepEditorForSpyer.getInstance();
            stepEditorModel.KeywordList = KeywordsForSpyer.MarsKeywords;
            stepEditorModel.convertFromRecordAndFromList(this.TestSteps);
            stepEditorModel.isRecordReplayMode = true;//设置为record replay mode
            //stepEditorModel.SetTestStep(lstTestStep);
            stepEditorModel.scrollToLast += stepEditor.ScrollToTheBottom;
            stepEditorModel.updateProcessBar += stepEditor.UpdateProcessBar;
            //stepEditorModel.TestSteps.CollectionChanged += stepEditor.monitorColletionChanges;
            stepEditor.DataContext = stepEditorModel;

            stepEditor.Show();
        }

        private MarsRecordReplayStep _SelectedTestStep = null;
        private MarsRecordReplayStep SelectedTestStep { get => _SelectedTestStep; 
            set
            {
                if (value != _SelectedTestStep)
                {
                    _SelectedTestStep = value;
                    RaisePropertyChanged("SelectedTestStep");
                }
            } 
        }

        /// <summary>
        /// replay录制的test step。算法：
        /// 1，找到端口，连到socket上
        /// 2，循环发送test step到url:xxxx/replay/....
        /// 
        /// </summary>
        private void replayMarsClickImpl()
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|replayMarsClickImpl\tbegin");
            string strError = "SUCESS";
            try
            {
                if (!LoadSvcConfigFile()) return;

                if (IsRecordAndReplaySvcRunning())
                {
                    return;
                }

                //1，找到端口，连到socket上
                if (replayClient == null)
                {
                    string dir = typeof(RecordReplayStepsWinModal).Assembly.Location;
                    dir = System.IO.Path.GetDirectoryName(dir);
                    string swapFileName = System.IO.Path.Combine(dir, MarsRestFulCnst.cnst_SwapDir, MarsRestFulCnst.cnst_port_swapfile);
                    if (!System.IO.File.Exists(swapFileName))
                    {
                        strError = $"No such file exists|{swapFileName}";
                        return;
                    }
                    var restSvcInfo = RestServiceInfo.Instance();
                    if (!restSvcInfo.loadFromFile(swapFileName, ref strError))
                    {
                        logger.Error($"{iMark}|replayMarsClickImpl\t|error|{strError}");
                        return;
                    }
                    replayClient = RESTClient2MessageCenter.getInstance();
                    
                }
                /// 2，循环发送test step到url:xxxx/replay/....
                /// 
                if (_recordedStepsFromFile == null)
                {
                    MessageBox.Show($"No test steps, please record some test step first", "MARS Message", MessageBoxButton.OK);
                    return;
                }
                if (replayThread != null)
                {
                    replayThread.Abort();
                }
                replayThread = new System.Threading.Thread(replaySteps);
                replayThread.Start();
                logger.Info($"{iMark}|replayMarsClickImpl\tThread has being started");
                return;
            }
            catch (Exception e)
            {
                logger.Error($"{iMark}|replayMarsClickImpl\tError|{e.Message}", e);
                return;
            }
            finally
            {
                logger.Info($"{iMark}|replayMarsClickImpl\tEnd|status|{strError}");
            }
        }

        private void save2MarsClickImpl()
        {
            // stop 
            int iMark = new Random().Next();
            logger.Info($"{iMark}|save2MarsClickImpl\tbegin");
            try
            {
                if (IsRecordAndReplaySvcRunning())
                {
                    return;
                }

                RestClient2MarsServer marsRestClient = new RestClient2MarsServer();
                string strError = "";
                bool isOk= marsRestClient.sendTestCaseRecordToServer(_recordedStepsFromFile,ref strError);
                if (!isOk)
                {
                    currentStatus = strError;
                }
                else currentStatus = "Test steps have been send to WebServer, please check your browser.";
            }
            catch (Exception e)
            {
                logger.Error(e,$"{iMark}|exception|{e.Message}|{e.StackTrace}");
            }
            finally
            {
                logger.Info($"{iMark}|End");
            }
            
        }

        private static bool isEndofListSetting = false;
        internal Action<object, Brush> SetTestStepExecuteStatusColor=null;

        private void lstBox_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!isEndofListSetting) return;
            //// Scroll to the last item in the ListBox
            //var listBox = System.Windows.Application.Current.MainWindow.FindName("stepsListBox") as ListBox;
            //if (e.NewItems.Count>0)
            //    listBox.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
        }

        private void GetStepJsonFilePath()
        {
            string strPath = typeof(MarsFileMonitor).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);
            string UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            strPath = System.IO.Path.Combine(strPath, $"data\\obj\\{UserName}");
            currentStepJsonFilePath = System.IO.Path.Combine(strPath, MarsConstants.CNST_SYPTOOL_STEPS_FILENAME);
        }

        private void stepFileCreateImpl(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError)
        {
            currentStatus = $"start to record...";
        }
        /// <summary>
        /// 文件监控的实现部分，一旦该文件发生变化，将自动load该文件到list中
        /// </summary>
        /// <param name="fileChangeEvent"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        private void stepFileChangeImpl(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError)
        {
            currentStatus = $"a new step is created";
            string strFromStepFile = File.ReadAllText(currentStepJsonFilePath);
            _recordedStepsFromFile = JsonConvert.DeserializeObject<List<MarsRecordReplayStep>>(strFromStepFile);
            if (_recordedStepsFromFile == null)
            {
                this.currentStatus = "No steps is created";
                return;
            }

            /// remove last pauseRecord
            Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer.RemovePauseSteps( _recordedStepsFromFile );
            /// conver to string
            /// 
            List<string> lstTpmBox = _recordedStepsFromFile.Select(p=>p.toMarsStep())
                .ToList() ;
            //lstTpmBox.ForEach(p => p = p.Replace("\r\n", "|").Replace("\n", "|"));
            for (int i = 0; i < lstTpmBox.Count; i++)
            {
                lstTpmBox[i] = lstTpmBox[i].Replace("\r\n", "|").Replace("\n", "|");
            }

            isEndofListSetting = false ;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                //if (lstTpmBox.IndexOf(MarsObjectKeyword.cnst_pauseRecord))
                _stepsListBox = new ObservableCollection<string>(lstTpmBox);
                TestSteps = new ObservableCollection<MarsRecordReplayStep>(_recordedStepsFromFile);
                //lstTpmBox.ForEach(p => _stepsListBox.Add(p));
                RaisePropertyChanged("TestStepDataGrid");
                //RaisePropertyChanged("TestSteps");
            });
            isEndofListSetting = true;
            //this.stepsListBox = new ObservableCollection<string>(lstTpmBox);
        }
        private void stepFileDeleteImpl(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError)
        {

        }
    }
}
