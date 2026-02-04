using Mars.Inter.MQCenter.objectEngine;
using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.message.Inter.MQCenter.HttpRestService;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.objectTypeMapping;
using Mars.message.ViewModel;
using MarsSpyTool.message.Inter.MQCenter.interProcess.HttpRestService.client;
using MarsSpyTool.Utility;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;
using Color = System.Drawing.Color;

namespace MarsSpyTool.subToolWindows.testStepEditor
{

    public delegate void ScrollToTheLastRow(int iRow=-1);
    public delegate void UpdateProcessBar(int v);

    public class KeywordsForSpyer
    {
        private static ObservableCollection<T_KEYWORDDTO> marsKeywords = new ObservableCollection<T_KEYWORDDTO>();
        public static ObservableCollection<T_KEYWORDDTO> MarsKeywords
        {
            get => marsKeywords;
            set => marsKeywords = value;
        }
        private static NLog.Logger logger= NLog.LogManager.GetLogger("MarsSpyLog");
        public static void InitKeyword()
        {
            logger.Info($"InitKeyword\tbegin|{SystemGlobalHelper.g_currentDB_Idx}");
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(SystemGlobalHelper.g_currentDB_Idx);
            try
            {
                string strError = "";
                bool isOk = false;
                var keywords = clnt.LoadAllKeywords(ref isOk, ref strError);

                marsKeywords = new ObservableCollection<T_KEYWORDDTO>(keywords.Keys);
                logger.Info($"InitKeyword\t|get keywords|{marsKeywords.Count}|");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"InitKeyword\t{ex.Message}");
            }
        }

        public static T_KEYWORDDTO CaptureAndCompare
        {
            get
            {
                if (marsKeywords==null ) { return null; }
                return marsKeywords.FirstOrDefault(p => p.KEY_WORD_NAME.Equals("CAPTUREANDCOMPARE", StringComparison.OrdinalIgnoreCase));
            }
        }

    }

    public class IsSkipToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSkip = (bool)value;
            return isSkip ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class StringToUpperConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.ToUpperInvariant();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class BooleanToGenerateTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value == null || parameter == null || !(value is bool isChecked) || !isChecked)
            //    return DependencyProperty.UnsetValue;

            return value;
        }
    }

    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(value is bool isChecked) || !isChecked)
                return DependencyProperty.UnsetValue;

            return parameter;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class MarsAutoGenStepsSettings: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<string> ignoreTypes = null;

        public ObservableCollection<string> IgnoreTypes
        {
            get => ignoreTypes==null?ignoreTypes= new ObservableCollection<string>( ObjectEngineConfigFileManagement.GetEngineObject().ignoreTypes):ignoreTypes;
            set
            {
                if (ignoreTypes == value) return;
                ignoreTypes = value;
                OnPropertyChanged(nameof(IgnoreTypes));
            }
        }

        private string currentType;
        public string CurrentType
        {
            get => currentType;
            set
            {
                if (currentType == value) return;
                currentType = value;
                OnPropertyChanged(nameof(CurrentType));
            }
        }
    }


    public class MarsSpyProcessInfo: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int currentValue;
        public int CurrentValue
        {
            get => currentValue;
            set
            {
                if (currentValue == value) return;
                currentValue = value;
                OnPropertyChanged(nameof(CurrentValue));
            }
        }
    }


    internal class MARSTestStepEditorModel: INotifyPropertyChanged
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog") ;
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MARSTestStepEditorModel()
        {
            
        }
        public ScrollToTheLastRow scrollToLast = null;
        public UpdateProcessBar updateProcessBar = null;

        private MarsSpyProcessInfo currentProcessInfo=new MarsSpyProcessInfo();
        public MarsSpyProcessInfo CurrentProcessInfo
        {
            get => currentProcessInfo;
            set
            {
                if (currentProcessInfo == value) return;
                currentProcessInfo = value;
                
                OnPropertyChanged(nameof(CurrentProcessInfo));
            }
        }

     
        private string currentWorkStatus = "Status";
        public string CurrentWorkStatus
        {
            get { return currentWorkStatus; } 
            set {
                if (currentWorkStatus == value) return;
                currentWorkStatus = value;
                OnPropertyChanged(nameof(CurrentWorkStatus));
            }
        }

        
        public MarsSpyApplication SelectedApplication
        {
            get => MarsTestAPPDBInfo.CurrentApplicationInfo;
            set
            {
                if (MarsTestAPPDBInfo.CurrentApplicationInfo == value) return;
                MarsTestAPPDBInfo.CurrentApplicationInfo = value;
                OnPropertyChanged(nameof(SelectedApplication));
            }
        }

        public string TargetDBIdx
        {
            get => MarsTestAPPDBInfo.currentDBIdx;
            set
            {
                if (MarsTestAPPDBInfo.currentDBIdx == value) return;
                MarsTestAPPDBInfo.currentDBIdx = value;
                OnPropertyChanged(nameof(TargetDBIdx));
            }
        }

        private Color currentWorkStatusColor = Color.Black;
        public Color CurrentWorkStatusColor
        {
            get { return currentWorkStatusColor; }
            set
            {
                if (currentWorkStatusColor == value) return;
                currentWorkStatusColor = value;
                OnPropertyChanged(nameof(CurrentWorkStatusColor));
            }
        }

        private bool isIgnoreLable = true;
        public bool IsIgnoreLable
        {
            get => isIgnoreLable;
            set
            {
                if (isIgnoreLable == value) return;
                isIgnoreLable = value;
                OnPropertyChanged(nameof(IsIgnoreLable));
            }
        }

        private List<MARSTestStepsModel> testStepsSource = new List<MARSTestStepsModel>();

        private ObservableCollection<MARSTestStepsModel> testSteps;
        public ObservableCollection<MARSTestStepsModel> TestSteps
        {
            get => testSteps;
            set
            {
                if (testSteps == value)
                {
                    return;
                }
                testSteps = value;
                OnPropertyChanged(nameof(TestSteps));
            }
        }

        public ICommand RemoveCommand;
        public bool isRecordReplayMode { get; set; } = false;

        private MARSTestStepsModel selectedTestStep;
        public MARSTestStepsModel SelectedTestStep
        {
            get => selectedTestStep;
            set
            {
                if (selectedTestStep == value) return;
                selectedTestStep = value;
                OnPropertyChanged(nameof(SelectedTestStep));
            }
        }

        private ObservableCollection<MARSTestStepsModel> seltectedTestList = new ObservableCollection<MARSTestStepsModel>();
        public ObservableCollection<MARSTestStepsModel> SeltectedTestList
        {
            get => seltectedTestList;
            set
            {
                if (seltectedTestList == value) return;
                seltectedTestList = value;
                OnPropertyChanged(nameof(SeltectedTestList));
            }
        }

        private ObservableCollection<T_KEYWORDDTO> keywordList;
        public ObservableCollection<T_KEYWORDDTO> KeywordList
        {
            get => keywordList;
            set
            {
                if (keywordList == value) return;
                keywordList = value;
                OnPropertyChanged(nameof(KeywordList));
            }
        }

        public bool GenerateObjType
        {
            get => ObjectEngineConfigFileManagement.GetEngineObject() == null ? false
                : ObjectEngineConfigFileManagement.GetEngineObject().IsOnlyGenerateObjectInsideContainer;
            set
            {
                if (ObjectEngineConfigFileManagement.GetEngineObject() == null) return;
                if (ObjectEngineConfigFileManagement.GetEngineObject().IsOnlyGenerateObjectInsideContainer == value) return;
                ObjectEngineConfigFileManagement.GetEngineObject().IsOnlyGenerateObjectInsideContainer = value;
                OnPropertyChanged(nameof(GenerateObjType));
                // save to config file

                bool isOk = false;
                string strError = "";
                ObjectEngineConfigFileManagement.saveBacktoFile(ref isOk, ref strError);
            }
        }

        public bool GenerateObjTypeFalse
        {
            get => !GenerateObjType;
            set
            {
                if (GenerateObjType!= value) return;
                GenerateObjType = !value;
                OnPropertyChanged(nameof(GenerateObjTypeFalse));
            }
        }
        /// <summary>
        /// 判断第一步是否是pegwindow，如果不是，就直接添加最近的
        /// </summary>
        private bool FixPegwindowSteps(ObservableCollection<MarsRecordReplayStep> stepsFromRecordAndReplay)
        {
            int iPegIdx = -1;
            bool isFirstStepPegwin = true;
            var guiKeywords = ObjectEngineConfigFileManagement.GetEngineObject().marsTypeMapping_dotNet
                .Select(p => p.defaultKeywords)
                .SelectMany(p => p)
                .Distinct()
                .ToList();
            MarsRecordReplayStep fakePeg = null;
            string strFirstStepKeywords = stepsFromRecordAndReplay[0].keyWord;
            if (!MarsObjectKeyword.cnst_pegwindow.Equals(strFirstStepKeywords, StringComparison.OrdinalIgnoreCase))
            {
                isFirstStepPegwin = false;
            }
            if (!isFirstStepPegwin)
            {
                /// find first gui step
                /// 
                var firstGuiStp = stepsFromRecordAndReplay
                    .Select((step, index)=> new { Step = step, Idx = index})
                    .FirstOrDefault(p => guiKeywords.IndexOf(p.Step.keyWord) >= 0);
                if (firstGuiStp != null)
                {
                    fakePeg = firstGuiStp.Step.CreatePegKeyword();
                    iPegIdx = firstGuiStp.Idx;
                }
            }
            
            if (fakePeg != null)
            {
                stepsFromRecordAndReplay.Insert(iPegIdx, fakePeg);
                return true;
            }
            return false;
        }

        public void convertFromRecordAndFromList(ObservableCollection<MarsRecordReplayStep> stepsFromRecordAndReplay)
        {
            if (testSteps == null)
                testSteps = new ObservableCollection<MARSTestStepsModel>();
            int iRunOrd = 1;

            /// fix pegwindow
            /// 
            if (!FixPegwindowSteps(stepsFromRecordAndReplay))
            {
                MessageBox.Show("No GUI related Step Generated, please try again", "Message", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            for (int i = 0; i < (stepsFromRecordAndReplay == null ? -1 : stepsFromRecordAndReplay.Count); i++)
            {
                var itmSrc = stepsFromRecordAndReplay[i];
                if (itmSrc == null) continue;
                var k = this.keywordList.FirstOrDefault(p => (p.KEY_WORD_NAME == null) ? false : p.KEY_WORD_NAME.Equals(itmSrc.keyWord, StringComparison.OrdinalIgnoreCase));
                if (k == null) // no keyword is find
                    continue ;
                MARSTestStepsModel itmTarget = new MARSTestStepsModel();
                itmTarget.IsSkip = false;
                itmTarget.AttachedObject = new MarsSpiedObjectBasicInfo();
                itmTarget.AttachedObject.controlMarsType = itmSrc.objectMarsType;
                itmTarget.AttachedObject.index = 0;
                itmTarget.AttachedObject.isVisible = true;
                itmTarget.AttachedObject.objectName = itmSrc.objectQuickAccess==null? null:itmSrc.objectQuickAccess.objectName;
                itmTarget.AttachedObject.objectNamePath = itmSrc.objectQuickAccess == null ? null : itmSrc.objectQuickAccess.objectNamePath;
                itmTarget.AttachedObject.objectType = itmSrc.objectMarsType;
                itmTarget.AttachedObject.hwnd = itmSrc.objectHandle.ToInt64();
                
                if (itmSrc.bound != null)
                {
                    itmTarget.AttachedObject.x = itmSrc.bound.x;
                    itmTarget.AttachedObject.y = itmSrc.bound.y;
                    itmTarget.AttachedObject.h = itmSrc.bound.h;
                    itmTarget.AttachedObject.w = itmSrc.bound.w;
                }
                itmTarget.AttachedObject.objectTypePath = itmSrc.objectQuickAccess == null ? null : itmSrc.objectQuickAccess.objectTypePath;
                itmTarget.AttachedObject.Pegwindow = new MarsSpiedObjectBasicInfo();

                if (itmSrc.pegQuickAccess != null)
                {
                    itmTarget.AttachedObject.Pegwindow.objectName = itmSrc.pegQuickAccess.objectName;
                    itmTarget.AttachedObject.Pegwindow.isVisible = true;
                    itmTarget.AttachedObject.Pegwindow.objectNamePath = itmSrc.pegQuickAccess.objectNamePath;
                    itmTarget.AttachedObject.Pegwindow.objectType = itmSrc.pegQuickAccess.objectType;
                    itmTarget.AttachedObject.Pegwindow.objectTypePath = itmSrc.pegQuickAccess.objectTypePath;
                    itmTarget.AttachedObject.Pegwindow.Text = itmSrc.pegQuickAccess.Text;
                }
                
                itmTarget.ControlType = itmSrc.objectType;
                itmTarget.Keyword = k;
                itmTarget.CurrentData = itmSrc.opText;
                itmTarget.Test_parameter = itmSrc.Parameter;
                itmTarget.Run_order = iRunOrd++;
                itmTarget.ObjectType = itmSrc.objectMarsType;
                itmTarget.GuiObjectName = itmSrc.objectQuickAccess == null ? null : itmSrc.objectQuickAccess.objectName;
                itmTarget.GuiObjectInfo = itmSrc.BuildObjectQuickAccess();
                itmTarget.AttachedObject.hwnd = itmSrc.objectHandle.Equals(IntPtr.Zero)?0:itmSrc.objectHandle.ToInt64();
                if (itmSrc.bound!=null)
                    itmTarget.Rect = new Rectangle(itmSrc.bound.x, itmSrc.bound.y, itmSrc.bound.w, itmSrc.bound.h);
                testSteps.Add(itmTarget);

            }
            OnPropertyChanged(nameof(TestSteps));
            
        }

        public async void SetTestStep(List<MARSTestStepsModel> lstStep, bool isFlash=false, int iTime=100, 
            int insertPos=0)
        {
            if (!isFlash)
            {
                testStepsSource = lstStep;
                TestSteps = new ObservableCollection<MARSTestStepsModel>(lstStep);
                CurrentWorkStatus = $"Status: Total|{lstStep.Count}| Test Step";
            }
            else
            {
                RESTClient2MessageCenter clnt = new RESTClient2MessageCenter();
                bool isOk = true;
                string strError = "";
                if (insertPos < 0)
                    insertPos = 0;
                bool isInsertMode = false;
                if (insertPos == 0)
                    TestSteps.Clear();
                else isInsertMode = true;
                testStepsSource = new List<MARSTestStepsModel>();
                await Task.Delay(iTime);
                CurrentWorkStatusColor = Color.Blue;
                Dispatcher.CurrentDispatcher.Invoke(() => { 
                    this.CurrentProcessInfo.CurrentValue = 2; 
                    if (this.updateProcessBar != null)
                    {
                        this.updateProcessBar(this.currentProcessInfo.CurrentValue);
                    }
                });
                await Task.Delay(iTime);
                //System.Threading.Thread.Sleep(iTime);
                int iScrollRow = -1;
                int i = 0;
                int iAttachedCount = 0;
                while (i < lstStep.Count)
                {
                    try
                    {
                        var testStep = lstStep[i];
                        CurrentWorkStatus = $"Status: Total|{lstStep.Count}|from|{insertPos}|Test Step, Loaded|{i + 1}";
                        if (!testStep.IsSkip)
                        {
                            clnt.HighlightObject(testStep.AttachedObject, ref isOk, ref strError);

                            if ((testStep.ObjectType ?? "").Equals("SWFTABLE", StringComparison.OrdinalIgnoreCase))
                            {
                                if (this.IsGenerateCaptureAndCompareForGrid)
                                {
                                    List<MARSTestStepsModel> lstExtensionStps=QueryObjectDetailsColumns(testStep, clnt, ref isOk, ref strError);
                                    if ((isOk)&&(lstExtensionStps!=null)&&(lstExtensionStps.Count>0))
                                    {
                                        lstExtensionStps.ForEach(x => { 
                                            if (x != null)
                                            {
                                                iAttachedCount += 1;
                                                TestSteps.Insert(insertPos+i+ iAttachedCount, x);
                                            }
                                        });
                                        continue;
                                    }
                                }
                            }

                            if (isInsertMode)
                            {
                                testStep.Run_order = insertPos + i + 1;
                                TestSteps.Insert(insertPos + i, testStep);
                                iScrollRow = testStep.Run_order;
                                SelectedTestStep = testStep;
                            }
                            else
                                TestSteps.Add(testStep);

                        }
                        else
                        {
                            if ((testStep.ObjectType ?? "").Equals("SWFTABLE", StringComparison.OrdinalIgnoreCase))
                            {
                                if (this.IsGenerateCaptureAndCompareForGrid)
                                {
                                    List<MARSTestStepsModel> lstExtensionStps = QueryObjectDetailsColumns(testStep, clnt, ref isOk, ref strError);
                                    if ((isOk) && (lstExtensionStps != null) && (lstExtensionStps.Count > 0))
                                    {
                                        lstExtensionStps.ForEach(x => {
                                            if (x != null)
                                            {
                                                iAttachedCount += 1;
                                                TestSteps.Insert(insertPos + i + iAttachedCount, x);
                                            }
                                        });
                                        continue;
                                    }
                                }
                            }
                            if (isInsertMode)
                            {
                                testStep.Run_order = insertPos + i + 1;
                                TestSteps.Insert(insertPos + i, testStep);
                                iScrollRow = testStep.Run_order;
                                SelectedTestStep = testStep;

                            }
                            else
                                TestSteps.Add(testStep);
                        }

                        if (scrollToLast != null)
                        {
                            scrollToLast(iScrollRow);
                        }
                        int r = (int)((i * 100) / lstStep.Count);
                        if (r > this.CurrentProcessInfo.CurrentValue)
                        {
                            Dispatcher.CurrentDispatcher.Invoke(() =>
                            {
                                this.CurrentProcessInfo.CurrentValue = r;
                                if (this.updateProcessBar != null)
                                {
                                    this.updateProcessBar(this.currentProcessInfo.CurrentValue);
                                }
                            });
                        }
                        await Task.Delay(iTime);
                    }
                    finally
                    {
                        i += 1;
                    }
                }

                for (i=0;i<TestSteps.Count;i++)
                {
                    var itm = TestSteps[i];
                    if (itm == null) continue;
                    itm.Run_order = i + 1;
                }
                //OnPropertyChanged(nameof(TestSteps));
                this.CurrentProcessInfo.CurrentValue = 100;
                if (this.updateProcessBar != null)
                {
                    this.updateProcessBar(100);
                }
            }
        }
        /// <summary>
        /// 创建captureAndCompare,从对象中获得所有的column
        /// </summary>
        /// <param name="testStep"></param>
        /// <param name="clnt"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private List<MARSTestStepsModel> QueryObjectDetailsColumns(MARSTestStepsModel testStep, RESTClient2MessageCenter clnt, ref bool isOk, ref string strError)
        {
            logger.Info("QueryObjectDetailsColumns\tBegin");
            try
            {
                if (testStep.AttachedObject == null)
                {
                    strError = "No object information is attached";
                    isOk = false;
                    return null;
                }
                EngineGetObjectExtensionDetailReq req = new EngineGetObjectExtensionDetailReq();
                req.uuid = testStep.AttachedObject.obj_uuid;
                req.objectHwnd = testStep.AttachedObject.hwnd;
                req.objectExtCmd = EngineQueryObjCommand._getAllColumns;
                EngineGetObjectExtensionDetailRspn rspns = clnt.QueryObjDetails(req, ref isOk, ref strError);
                if (!isOk)
                {
                    logger.Error($"QueryObjectDetailsColumns\tError|{strError}|");
                    return null ;
                }
                if (!(rspns.msg??"").Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Error($"QueryObjectDetailsColumns\tError|{strError}|");
                    isOk = false ;
                    return null;
                }
                List<MARSTestStepsModel> rslt = new List<MARSTestStepsModel>();
                int iRunOrder = 0;
                foreach (var itm in rspns.extensionData)
                {
                    if (itm == null) continue;
                    rslt.Add(new MARSTestStepsModel()
                    {
                        AttachedObject = testStep.AttachedObject,
                        ControlType = testStep.ControlType,
                        CurrentData = testStep.CurrentData,
                        GuiObjectInfo = testStep.GuiObjectInfo,
                        GuiObjectName = testStep.GuiObjectName,
                        IsDisplay = testStep.IsDisplay,
                        IsSkip = testStep.IsSkip,
                        Keyword = KeywordsForSpyer.CaptureAndCompare,
                        ObjectInfoInDB = testStep.ObjectInfoInDB,
                        ObjectNameId = testStep.ObjectNameId,
                        ObjectType = testStep.ObjectType,
                        Rect = testStep.Rect,
                        Run_order = testStep.Run_order + (++iRunOrder),
                        Test_parameter = $"ALLROWS;{itm.getNoneEmptyKOrV()}"
                    });
                }
                return rslt;
            }
            catch(Exception e)
            {
                strError = $"Can't get details with exception|{e.Message}";
                logger.Error(e, $"QueryObjectDetailsColumns\t|{strError}");
                isOk = false;
                return null;
            }
            finally
            {
                logger.Info("QueryObjectDetailsColumns\tEnd");
            }
        }

        //private bool isHideSkipSteps = false;
        //public bool IsHideSkipSteps
        //{
        //    get => isHideSkipSteps;
        //    set
        //    {
        //        if (isHideSkipSteps == value) return;
        //        isHideSkipSteps = value;
        //        OnPropertyChanged(nameof(IsHideSkipSteps));

        //        if (this.TestSteps != null)
        //        {
        //            foreach(var itm in TestSteps)
        //            {
        //                if (itm == null) continue;
        //                if (itm.IsSkip) itm.IsDisplay = !isHideSkipSteps;
        //            }
        //        }
        //    }
        //}

        private MarsAutoGenStepsSettings settings = new MarsAutoGenStepsSettings();
        public MarsAutoGenStepsSettings Settings
        {
            get => settings;
            set
            {
                if (settings == value) return;
                settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        private bool isGenerateCaptureAndCompareForGrid;
        public bool IsGenerateCaptureAndCompareForGrid
        {
            get => isGenerateCaptureAndCompareForGrid;
            set
            {
                if (isGenerateCaptureAndCompareForGrid == value) return;
                isGenerateCaptureAndCompareForGrid = value;
                OnPropertyChanged(nameof(isGenerateCaptureAndCompareForGrid));
            }
        }

        public void RemoveUnmappingObjectsFromSteps()
        {
            logger.Info("RemoveUnmappingObjectsFromSteps\tbegin");
            int i = 0;
            while (i<TestSteps.Count)
            {
                try
                {
                    if (TestSteps[i] == null)
                    {
                        TestSteps.RemoveAt(i);
                        continue;
                    }
                    if (string.IsNullOrEmpty(TestSteps[i].GuiObjectName))
                    {
                        TestSteps.RemoveAt(i);
                        continue;
                    }
                    testSteps[i].Run_order = i + 1;
                    i += 1;
                }
                finally
                {
                    
                }
            }
            logger.Info("RemoveUnmappingObjectsFromSteps\tend");
        }

        //private MarsStepsInsertOption currentStepInsertOption;
        public MarsStepsInsertOption CurrentStepInsertOption
        {
            get => ObjectEngineConfigFileManagement.GetEngineObject() == null ? MarsStepsInsertOption.AppendToTheEnd
                : ObjectEngineConfigFileManagement.GetEngineObject().stepsInsertOption;
            set
            {
                if (ObjectEngineConfigFileManagement.GetEngineObject() == null)
                    return;
                if (ObjectEngineConfigFileManagement.GetEngineObject().stepsInsertOption==value) return;
                ObjectEngineConfigFileManagement.GetEngineObject().stepsInsertOption = value;
                OnPropertyChanged(nameof(CurrentStepInsertOption));
                bool isOk = false;
                string strError = "";
                ObjectEngineConfigFileManagement.saveBacktoFile(ref isOk, ref strError);
            }
        }

        private T_KEYWORDDTO getKeywordBYName(string keyIdx)
        {
            return KeywordsForSpyer.MarsKeywords.Where(p => p.KEY_WORD_NAME.Equals(keyIdx, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public List<MARSTestStepsModel> FilterObjectsAndBuildTestSteps(List<MarsSpiedObjectBasicInfo> objs, ref bool isOk, ref string strError)
        {
            List<MARSTestStepsModel> lstRslt = new List<MARSTestStepsModel>();
            try
            {
                /// fitler
                /// 1, filter ignore types
                /// 
               
                if (ObjectEngineConfigFileManagement.GetEngineObject() == null)
                {
                    return lstRslt;
                }
                var lstFiltered = objs.Where(p => !ObjectEngineConfigFileManagement.GetEngineObject().ignoreTypes.Contains(p.objectType))
                    .Where(p => (p.w > 0) && (p.h > 0))
                    .ToList();
                if (IsIgnoreLable)
                {
                    lstFiltered = lstFiltered.Where(p =>
                        string.IsNullOrEmpty(p.controlMarsType) || (
                        !p.controlMarsType.Equals("swfLable", StringComparison.OrdinalIgnoreCase)
                    )).ToList();
                }
                logger.Info("FilterObjectsAndBuildTestSteps", $"after filter ignore type|{lstFiltered.Count}|objects left");
                // 将invisible和宽度和高度为0的放到
                // 修正对象的mars type
                var eumObjAndType = from q in lstFiltered
                                    from t in ObjectEngineConfigFileManagement.GetEngineObject().marsTypeMapping_dotNet
                                    .Where(x => (x.controlType.Contains(q.objectType)) || (x.marsType.Equals(q.controlMarsType, StringComparison.OrdinalIgnoreCase)))
                                    .DefaultIfEmpty()
                                    select new
                                    {
                                        guiObj = q,
                                        newType = t == null ? null : t.marsType,
                                        keywords = t == null ? null : t.defaultKeywords
                                    };
                foreach (var itm in eumObjAndType)
                {
                    if (itm == null) continue;
                    if (!string.IsNullOrEmpty(itm.newType))
                        itm.guiObj.controlMarsType = itm.newType;
                    else if (string.IsNullOrEmpty(itm.guiObj.controlMarsType))
                        itm.guiObj.controlMarsType = "swfObject";
                }
                // remove 
                int iRunOrder = 0;

                var defaultKeyword = KeywordsForSpyer.MarsKeywords.Where(p => "ClickButton".Equals(p.KEY_WORD_NAME, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                var lstStepx = eumObjAndType.OrderBy(p => p.guiObj.PegWindUUID)
                    .ThenBy(p => p.guiObj.y)
                    .ThenBy(p => p.guiObj.x)
                    .ToList();
                List<MARSTestStepsModel> lstStep = new List<MARSTestStepsModel>();
                string strPreviousKeyword = "";
                lstStepx.ForEach((itm) =>
                {
                    // build test step
                    if (itm != null)
                    {
                        if (!string.IsNullOrEmpty(itm.newType))
                        {
                            itm.guiObj.controlMarsType = itm.newType;
                        }

                        MARSTestStepsModel tmpStepModl = new MARSTestStepsModel()
                        {
                            Run_order = lstRslt.Count + 1,
                            GuiObjectName = "TBD",
                            Keyword = ((itm.keywords == null) || (itm.keywords.Count <= 0)) ? defaultKeyword : getKeywordBYName(itm.keywords[0]),
                            GuiObjectInfo = itm.guiObj.getObjectInfo(),
                            CurrentData = itm.guiObj.Text,
                            IsSkip = (!itm.guiObj.isVisible) || (itm.guiObj.w == 0) || (itm.guiObj.h == 0),
                            ObjectType = itm.guiObj.controlMarsType,
                            ControlType = itm.guiObj.objectType,
                            AttachedObject = itm.guiObj,
                            Rect = new Rectangle(itm.guiObj.x, itm.guiObj.y, itm.guiObj.w, itm.guiObj.h)
                        };
                        if ((!string.IsNullOrEmpty(tmpStepModl.Keyword.KEY_WORD_NAME))
                        && (tmpStepModl.Keyword.KEY_WORD_NAME.Equals("pegwindow", StringComparison.OrdinalIgnoreCase))
                        && (strPreviousKeyword.Equals("pegwindow", StringComparison.OrdinalIgnoreCase))
                        )
                        {
                            //replace the last item of the list
                            lstRslt[lstRslt.Count - 1] = tmpStepModl;
                            tmpStepModl.Run_order = lstRslt.Count;
                        }
                        else
                            lstRslt.Add(tmpStepModl);
                        strPreviousKeyword = tmpStepModl.Keyword.KEY_WORD_NAME;
                    }
                });
                /// 如果相鄰的test step都是pegwindow，那么，保留后一个
                /// 
                return lstRslt;
            }catch(Exception e)
            {
                logger.Error(e, $"FilterObjectsAndBuildTestSteps\t|{e.Message}|{e.StackTrace}");
                return lstRslt;
            }
        }

        internal void mappingObjects(ref bool isOk, ref string strError)
        {
            logger.Info("mappingObjects\tbegin");
            try
            {
                /// convert teststps to v_teststep_fullvi, then invoke from db_web client
                /// 
                List<V_TEST_STEPS_FULLVISIONDTO> lstStps = new List<V_TEST_STEPS_FULLVISIONDTO>();
                if (this.seltectedTestList.Count > 1)
                {
                    if (this.seltectedTestList.Count < 5)
                    {
                        strError = "Please Select at least 5 steps if you want to mapping parts of the test steps";
                        isOk = false;
                        return;
                    }
                    foreach (var stp in this.seltectedTestList)
                    {
                        if (stp == null) continue;
                        lstStps.Add(new V_TEST_STEPS_FULLVISIONDTO()
                        {
                            RUN_ORDER = stp.Run_order,
                            KEY_WORD_NAME = stp.Keyword.KEY_WORD_NAME,
                            KEY_WORD_ID = stp.Keyword.KEY_WORD_ID,
                            OBJECT_HAPPY_NAME = stp.GuiObjectName,
                            APPLICATION_ID = this.SelectedApplication.ApplicationId,
                            QUICK_ACCESS = stp.AttachedObject?.getQuickAccess(true),
                            TYPE_NAME = stp.ObjectType
                        });
                    }
                }
                else
                {
                    foreach (var stp in TestSteps)
                    {
                        if (stp == null) continue;
                        lstStps.Add(new V_TEST_STEPS_FULLVISIONDTO()
                        {
                            RUN_ORDER = stp.Run_order,
                            KEY_WORD_NAME = stp.Keyword.KEY_WORD_NAME,
                            KEY_WORD_ID = stp.Keyword.KEY_WORD_ID,
                            OBJECT_HAPPY_NAME = stp.GuiObjectName,
                            APPLICATION_ID = this.SelectedApplication.ApplicationId,
                            QUICK_ACCESS = stp.AttachedObject?.getQuickAccess(true),                           
                            TYPE_NAME = stp.ObjectType
                        });
                    }
                }
                
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(TargetDBIdx);
                lstStps = clnt.mappingObjectsBasedOnTestStepFromCaptureAndReplay(lstStps, 
                    this.SelectedApplication.ApplicationId,
                    TargetDBIdx,
                    ref strError, ref isOk);
                if (!isOk)
                {
                    logger.Error($"mappingObjects\t{strError}");
                    return;
                }

                /// lstStp could have more than one item for the same run order
                /// 
                var objInfo = lstStps.GroupBy(p=>p.RUN_ORDER)
                    .ToDictionary(p=>p.Key, v=>v.ToList());
                /// 假定只有一个数据,所以对ObjectInfo暂时不做处理
                /// 
                var lstOrder = objInfo.Keys.OrderBy(p => p);
                var rslt = new ObservableCollection<MARSTestStepsModel>();
                foreach(var ord in lstOrder){
                    List<V_TEST_STEPS_FULLVISIONDTO> step = objInfo[ord];
                    var stpToDisplay = this.testSteps.Where(p => p.Run_order == ord).FirstOrDefault();
                    if (stpToDisplay == null) continue;
                    if ((step == null) || (step.Count <= 0))                    {
                        
                        if (stpToDisplay == null) continue;
                        rslt.Add(stpToDisplay);
                        continue;
                    }
                    var targetStp = step.FirstOrDefault(p => !string.IsNullOrEmpty(p.OBJECT_HAPPY_NAME));
                    if (targetStp == null) targetStp = step[0];
                    if ((stpToDisplay.Keyword != null)
                        && (stpToDisplay.Keyword.KEY_WORD_NAME.Equals("pegwindow", StringComparison.OrdinalIgnoreCase)))
                    {
                        stpToDisplay.GuiObjectName = step[0].OBJECT_TYPE;
                        stpToDisplay.IsObjectNameModified = true;
                        stpToDisplay.ObjectNameId = targetStp.OBJECT_NAME_ID ?? -1;
                        stpToDisplay.ObjectInfoInDB = step;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(stpToDisplay.ObjectType))
                        {
                            stpToDisplay.GuiObjectName = targetStp.OBJECT_HAPPY_NAME;
                            stpToDisplay.IsObjectNameModified = true;
                            stpToDisplay.ObjectNameId = targetStp.OBJECT_NAME_ID ?? -1;
                            stpToDisplay.ObjectInfoInDB = step;
                        }
                    }
                }
            }
            catch(Exception e )
            {
                logger.Error(e, $"mappingObjects\tException|{e.Message}");
            }
            finally
            {
                logger.Info("mappingObjects\tend");
            }
        }

        private int hookedPId;
        public int HookedPId
        {
            get => hookedPId;
            set
            {
                if (hookedPId == value) return;
                hookedPId = value;
                OnPropertyChanged(nameof(HookedPId));
            }
        }

        private string testCaseName; 
        public string TestCaseName
        {
            get => testCaseName;
            set
            {
                if (testCaseName==value) { return; }
                testCaseName = value;
                OnPropertyChanged(nameof(TestCaseName));
            }
        }


        private string testcaseDesc;
        public string TestCaseDesc
        {
            get => testcaseDesc;
            set
            {
                if (testcaseDesc==value) { return;}
                testcaseDesc = value;
                OnPropertyChanged(nameof(TestCaseDesc));
            }
        }

        public bool saveToMarsImpl(ref string strError)
        {
            logger.Info("saveToMarsImpl\tbegin");
            /// 算法：
            /// 1，判断是否存在没有设置好的obje，如果存在返回
            /// 2，调用save to mars的，显示处理结果
            /// 
            this.CurrentWorkStatus = "saving test case...";
            if (string.IsNullOrEmpty(this.TestCaseName))
            {
                strError = "Please set Testcase name first.";
                return false;
            }

            var objStp = this.testSteps
                .Where(p => string.Compare(p.GuiObjectName, "TBD", true) == 0)
                .FirstOrDefault();
            if (objStp != null)
            {
                strError = "Please make sure that all objects has been updated";
                return false;
            }

            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(TargetDBIdx);
            List<V_TEST_STEPS_FULLVISIONDTO> lstStps = new List<V_TEST_STEPS_FULLVISIONDTO>();
            foreach (var stp in TestSteps)
            {
                if (stp == null) continue;
                lstStps.Add(new V_TEST_STEPS_FULLVISIONDTO()
                {
                    RUN_ORDER = stp.Run_order,
                    KEY_WORD_NAME = stp.Keyword.KEY_WORD_NAME,
                    KEY_WORD_ID = stp.Keyword.KEY_WORD_ID,
                    OBJECT_HAPPY_NAME = stp.GuiObjectName,
                    APPLICATION_ID = this.SelectedApplication.ApplicationId,
                    QUICK_ACCESS = stp.GuiObjectInfo,
                    TYPE_NAME = stp.ObjectType,
                    VALUE_SETTING = stp.CurrentData,
                    OBJECT_NAME_ID = stp.ObjectNameId
                });
            }

            bool isOk = clnt.SaveTestcaseStepsForEngine(this.TestCaseName, 
                this.TestCaseDesc,                
                lstStps,
                this.TargetDBIdx,
                ref strError);
            logger.Info($"saveToMarsImpl\tSaveTestcaseStepsForEngine|{isOk}|{strError}|");
            logger.Info($"saveToMarsImpl\tend");
            return isOk;
        }
    }


    internal class MARSTestStepsModel: INotifyPropertyChanged
    {
        private T_KEYWORDDTO keyword;
        public T_KEYWORDDTO Keyword { get => keyword;
            set
            {
                if (keyword == value) return;
                keyword = value;
                OnPropertyChanged(nameof(Keyword));
            }
        }

       
        private int run_order;
        public int Run_order
        {
            get=> run_order;
            set
            {
                if (run_order == value) return;
                run_order = value;
                OnPropertyChanged(nameof(Run_order));
            }
        }

        private string guiObjectName;
        public string GuiObjectName
        {
            get => guiObjectName;
            set
            {
                if (guiObjectName == value) return;
                guiObjectName = value;
                OnPropertyChanged(nameof(GuiObjectName));
            }
        }

        private long objectName_id;
        public long ObjectNameId
        {
            get => objectName_id;
            set
            {
                if (objectName_id == value) return;
                objectName_id = value;
                OnPropertyChanged(nameof(ObjectNameId));
            }
        }

        private bool isObjectNameModified;
        public bool IsObjectNameModified
        {
            get => isObjectNameModified;
            set
            {
                if (isObjectNameModified == value) return;
                isObjectNameModified = value;
                OnPropertyChanged(nameof(IsObjectNameModified));
            }
        }

        private string guiObjectInfo;
        public string GuiObjectInfo
        {
            get => guiObjectInfo;
            set
            {
                if (guiObjectInfo == value) return;
                guiObjectInfo = value;
                OnPropertyChanged(nameof(GuiObjectInfo));
            }
        }

        private string test_parameter;
        public string Test_parameter
        {
            get => test_parameter;
            set
            {
                if (test_parameter == value) return;
                test_parameter = value;
                OnPropertyChanged(nameof(Test_parameter));
            }
        }

        private string currentData;
        public string CurrentData
        {
            get => currentData;
            set
            {
                if (currentData == value) return;
                currentData = value;
                OnPropertyChanged(nameof(CurrentData));
            }
        }

        private bool isSkip;
        public bool IsSkip
        {
            get => isSkip;
            set
            {
                if (isSkip == value) return;
                isSkip = value;
                OnPropertyChanged(nameof(IsSkip));
            }
        }
        /// <summary>
        /// swfedit, or others
        /// </summary>
        private string objectType;
        public string ObjectType
        {
            get => objectType;
            set
            {
                if (objectType == value) return;
                objectType = value;
                OnPropertyChanged(nameof(ObjectType));
            }
        }

        private Rectangle rect;
        public Rectangle Rect
        {
            get => rect;
            set
            {
                if (rect == value) return;
                rect = value;
                OnPropertyChanged(nameof(Rect));
            }
        }
        /// <summary>
        /// 对象类别
        /// </summary>
        private string controlType;
        public string ControlType
        {
            get { return controlType; } 
            set { 
                if (controlType == value) return;
                controlType = value;
                OnPropertyChanged(nameof(ControlType));
            }
        }

        private bool isDisplay = true;
        public bool IsDisplay
        {
            get => isDisplay;
            set
            {
                if (isDisplay==value) return;
                isDisplay = value;
                OnPropertyChanged(nameof(IsDisplay));


            }
        }

        public MarsSpiedObjectBasicInfo AttachedObject { get; set; }

        public List<V_TEST_STEPS_FULLVISIONDTO> ObjectInfoInDB { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
