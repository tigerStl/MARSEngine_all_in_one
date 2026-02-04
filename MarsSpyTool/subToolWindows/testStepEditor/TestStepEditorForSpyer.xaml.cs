using Mars.Inter.MQCenter.objectEngine;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Utility;
using Mars.message.Utility.visualObjects.objectSpyer;
using Mars.message.windowsWrapper.SystemUtil;
using MarsSpyTool.message.Inter.MQCenter.interProcess.HttpRestService.client;
using MarsSpyTool.Properties;
using MarsSpyTool.subToolWindows.hintWindows;
using MarsSpyTool.subToolWindows.viewModal;
using MarsSpyTool.Utility;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MarsSpyTool.subToolWindows.testStepEditor
{
  

    /// <summary>
    /// Interaction logic for TestStepEditorForSpyer.xaml
    /// </summary>
    public partial class TestStepEditorForSpyer : Window
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("MarsSpyLog");

        private static TestStepEditorForSpyer instance;
        public TestStepEditorForSpyer()
        {
            InitializeComponent();
            
        }

        public static TestStepEditorForSpyer getInstance()
        {
            if (instance== null) 
                instance = new TestStepEditorForSpyer();
            return instance;
        }


        public void ScrollToTheBottom(int iRow=-1)
        {
            if (iRow < 0)
            {
                if (this.testStepsDataGrid.Items.Count > 0)
                {
                    this.testStepsDataGrid.ScrollIntoView(
                                    this.testStepsDataGrid.Items[this.testStepsDataGrid.Items.Count - 1]);
                }
            }
            else
            {
                /// scroll to the special row num
                /// 
                if (iRow < this.testStepsDataGrid.Items.Count)
                {
                    this.testStepsDataGrid.ScrollIntoView(this.testStepsDataGrid.Items[iRow]);
                }
            }

        }

        public void UpdateProcessBar(int v)
        {
            this.processBar.Value = v;
        }
        public void monitorColletionChanges(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                try
                {
                    
                    //if (e.NewStartingIndex<this.testStepsDataGrid.Items.Count)
                    //    this.testStepsDataGrid.ScrollIntoView(
                    //        this.testStepsDataGrid.Items[e.NewStartingIndex]);
                }
                catch
                {

                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                // Set the window startup location to center screen
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void locate_ObjectClick(object sender, RoutedEventArgs e)
        {
            Logger.Info("locate_ObjectClick\tBegin");
            /// highlight对象
            /// 
            if ((!(DataContext is MARSTestStepEditorModel))
                ||(DataContext==null)
                ||(((MARSTestStepEditorModel)DataContext).SelectedTestStep==null))
            {
                Logger.Error($"locate_ObjectClick\tNo selected row");
                MessageBox.Show("Please select a row first", "Hint",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            MARSTestStepEditorModel model = DataContext as MARSTestStepEditorModel;
            /// build command to highlight the object
            /// 
            bool isOk = true;
            string strError = "";
            RESTClient2MessageCenter clnt = new RESTClient2MessageCenter();
            clnt.HighlightObject(model.SelectedTestStep.AttachedObject, ref isOk, ref strError);
            Logger.Info("locate_ObjectClick\tend");
        }

        private void TestRunClick(object sender, RoutedEventArgs e)
        {
            Logger.Info("TestRunClick\tbegin");
            try
            {
                this.WindowState = WindowState.Minimized;

                if ((!(DataContext is MARSTestStepEditorModel))
                || (DataContext == null)
                || (((MARSTestStepEditorModel)DataContext).SelectedTestStep == null))
                {
                    Logger.Error($"TestRunClick\tNo selected row");
                    MessageBox.Show("Please select a row first", "Hint", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                MARSTestStepEditorModel model = DataContext as MARSTestStepEditorModel;
                /// build command to highlight the object
                /// 
                bool isOk = true;
                string strError = "";
                RESTClient2MessageCenter clnt = new RESTClient2MessageCenter();
                var rspOfExecuteStp = clnt.RunOneTestStep(model.SelectedTestStep, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error($"TestRunClick\t|{strError}");
                    MessageBox.Show($"can't execute test step with error:\r\n|{strError}|");
                }
                else
                {
                    if (rspOfExecuteStp.executeStepOk)
                    {
                        Logger.Info($"TestRunClick\t|OK");
                        MessageBox.Show($"Test step executed successfully!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Logger.Info($"TestRunClick\t|Error|{rspOfExecuteStp.msg}");
                        MessageBox.Show(rspOfExecuteStp.msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex, $"TestRunClick\tException|{ex.Message}");
                return;
            }
            finally
            {
                this.WindowState = WindowState.Normal;
                Logger.Info("locate_ObjectClick\tend");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if ((DataContext == null)||(!(DataContext is MARSTestStepEditorModel)))
            {
                MessageBox.Show("no data is set", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isOk = true;
            string strError = "";
            List<MarsSpiedObjectBasicInfo> objs = RESTClient2MessageCenter
                .getInstance()
                .QueryCurrentObjects(new QueryObjectRequst() { 
                    typeOfGenerateSteps = ((MARSTestStepEditorModel)DataContext).GenerateObjType?1:0
                }, ref isOk, ref strError);
            if (!isOk)
            {
                Logger.Error("StartEngineAgent\t" + $"can't query objects|with Error|{strError}");
                return;
            }
            // filter ignored types
            try
            {
                (sender as Button).IsEnabled = false;
                List<MARSTestStepsModel> lstTestStep = ((MARSTestStepEditorModel)DataContext).FilterObjectsAndBuildTestSteps(objs, ref isOk, ref strError);
                ((MARSTestStepEditorModel)DataContext).SetTestStep(lstTestStep, true);
            }
            finally
            {
                (sender as Button).IsEnabled = true;
            }
        }

        private void CheckSkipSteps(object sender, RoutedEventArgs e)
        {            
            for (int i= this.testStepsDataGrid.Items.Count-1; i>=0; i--)
            {
                try
                {
                    var itm = testStepsDataGrid.Items[i] as MARSTestStepsModel;
                    if (itm == null) continue;
                    if (itm.IsSkip)
                    {
                        var dataRow = testStepsDataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                        if (dataRow == null) continue;
                        System.Windows.Controls.DataGridRow row = (System.Windows.Controls.DataGridRow)dataRow;
                        row.Visibility = Visibility.Collapsed;
                        Task.Delay(50);
                        Logger.Info($"CheckSkipSteps\t{i}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"CheckSkipSteps\t{ex.Message}");
                }
            }
        }

        private void UnCheckSkipSteps(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < this.testStepsDataGrid.Items.Count; i++)
            {
                try
                {
                    var itm = testStepsDataGrid.Items[i] as MARSTestStepsModel;
                    if (itm == null) continue;
                    if (itm.IsSkip)
                    {
                        var dataRow = testStepsDataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                        System.Windows.Controls.DataGridRow row = (System.Windows.Controls.DataGridRow)dataRow;
                        row.Visibility = Visibility.Visible;
                        Task.Delay(50);
                        Logger.Info($"CheckSkipSteps\t{i}");
                    }
                }
                catch(Exception ex)
                {
                    Logger.Error(ex, $"UnCheckSkipSteps\t{ex.Message}");
                }
            }
        }

        private void AddNewRowClick(object sender, RoutedEventArgs e)
        {
            var lst = ((MARSTestStepEditorModel)DataContext).TestSteps;
            if (this.testStepsDataGrid.SelectedItem == null)
            {

                lst.Add(new MARSTestStepsModel()
                {
                    Run_order = lst.Count
                });
            }
            else
            {
                MARSTestStepsModel selectedStep = testStepsDataGrid.SelectedItem as MARSTestStepsModel;
                if (selectedStep == null) return;
                if (this.testStepsDataGrid.SelectedIndex < lst.Count)
                {

                    lst.Insert(this.testStepsDataGrid.SelectedIndex, new MARSTestStepsModel()
                    {
                        Run_order = selectedStep.Run_order 
                    });
                    for (int i= selectedStep.Run_order ; i < lst.Count; i++)
                    {
                        lst[i].Run_order = lst[i].Run_order + 1;
                    }
                }
            }
        }

        private void RemoveRowClick(object sender, RoutedEventArgs e)
        {
            if (testStepsDataGrid.SelectedIndex < 0)
            {
                MessageBox.Show("Please select an item to delete first.");
                return;
            }
            if (this.testStepsDataGrid.SelectedIndex < ((MARSTestStepEditorModel)DataContext).TestSteps.Count)
            {
                var lst = ((MARSTestStepEditorModel)DataContext).TestSteps;
                MARSTestStepsModel selectedStep = testStepsDataGrid.SelectedItem as MARSTestStepsModel;
                for (int i = selectedStep.Run_order; i < lst.Count; i++)
                {
                    lst[i].Run_order = lst[i].Run_order - 1;
                }
                ((MARSTestStepEditorModel)DataContext).TestSteps.RemoveAt(this.testStepsDataGrid.SelectedIndex) ;
                
            }
        }

        private void RemoveIgnoreType_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("RemoveIgnoreType_Click\tbegin");
            MARSTestStepEditorModel dataModel = (MARSTestStepEditorModel)DataContext;

            if (string.IsNullOrEmpty(dataModel.Settings.CurrentType))
            {
                MessageBox.Show("Select an Item first, before remove", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int idx = -1; 
            if((idx=dataModel.Settings.IgnoreTypes.IndexOf(dataModel.Settings.CurrentType)) < 0)
            {
                MessageBox.Show($"The Item |{dataModel.Settings.CurrentType}|, doesn't exists in the List", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (idx <= 0) return;
            dataModel.Settings.IgnoreTypes.RemoveAt(idx);
            /// updaet 
            /// 
            ObjectEngineConfigFileManagement.GetEngineObject().ignoreTypes = dataModel.Settings.IgnoreTypes.ToList();
            bool isOk = false;
            string strError = "";
            ObjectEngineConfigFileManagement.saveBacktoFile(ref isOk, ref strError);
            if (!isOk)
            {
                Logger.Error($"RemoveIgnoreType_Click\tend\tCan't save to ignore Types|with Error|{strError}|");
            }
            else
            {
                Logger.Info("RemoveIgnoreType_Click\tend\tSucessfully done");
            }
        }

        private void AddIgnoreType_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("AddIgnoreType_Click\tbegin");
            MARSTestStepEditorModel dataModel = (MARSTestStepEditorModel)DataContext;

            if (string.IsNullOrEmpty(dataModel.Settings.CurrentType))
            {
                MessageBox.Show("Set some type string before click Add button", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int idx = -1;
            if ((idx = dataModel.Settings.IgnoreTypes.IndexOf(dataModel.Settings.CurrentType.Trim())) >= 0)
            {
                MessageBox.Show($"The Item |{dataModel.Settings.CurrentType}|, already exists in the List", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            List<string> lst = dataModel.Settings.IgnoreTypes.ToList();
            lst.Add(dataModel.Settings.CurrentType);
            lst.Sort();

            dataModel.Settings.IgnoreTypes = new ObservableCollection<string>(lst);// dataModel.Settings.IgnoreTypes.OrderBy(p => p);
            Logger.Info("AddIgnoreType_Click\tEnd");
        }

        private void MappingObjectsClick(object sender, RoutedEventArgs e)
        {
            Logger.Info("MappingObjectsClick\tbegin");
            MARSTestStepEditorModel dataModel = (MARSTestStepEditorModel)DataContext;
            bool isOk = false;
            string strError = "";

            if (MarsTestAPPDBInfo.CurrentApplicationInfo == null)
            {
                var appConfirmModal = new MarsHintWindowsModal();

                appConfirmModal.CreateApplicationsFromList(MarsSpyApplication.CurrentMarsFilteredApplications);
                MarsHintWindowSelectApplication appConfirm = new MarsHintWindowSelectApplication(appConfirmModal);
                appConfirm.ShowDialog();
                if ((bool)!appConfirm.DialogResult)
                {
                    MessageBox.Show("Please select an application first.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            dataModel.mappingObjects(
                ref isOk, ref strError);
            if (!isOk) {
                Logger.Error($"MappingObjectsClick\tError|{strError}");
                MessageBox.Show($"There are errors\r\n{strError}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("Have mapped objects successfully.","Info", MessageBoxButton.OK, MessageBoxImage.Information);
            Logger.Info("MappingObjectsClick\tEnd");
        }

        private void RemoveNoneMappingObjectClick(object sender, RoutedEventArgs e)
        {
            Logger.Info("RemoveNoneMappingObjectClick\tbegin");
            MARSTestStepEditorModel dataModel = (MARSTestStepEditorModel)DataContext;
            dataModel.RemoveUnmappingObjectsFromSteps();
            Logger.Info("RemoveNoneMappingObjectClick\tend");
        }

        private void SaveToMarsClick(object sender, RoutedEventArgs e)
        {
            Logger.Info("SaveToMarsClick\tbegin");
            try
            {
                MARSTestStepEditorModel dataModel = (MARSTestStepEditorModel)DataContext;
                string strError = "";
                if (!dataModel.saveToMarsImpl(ref strError))
                {
                    MessageBox.Show(strError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Test case has been saved to MARS web Successfully!", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Saveto MarsClick has exception|{ex.Message}");
            }
            finally
            {
                Logger.Info("SaveToMarsClick\tend");
            }
        }

        private void FindObjectClick(object sender, RoutedEventArgs e)
        {

        }
        private bool IsDragging { get; set; }
        private Cursor FinderCursor = null;
        private void FindObjectMouseDown(object sender, MouseButtonEventArgs e)
        {
            Logger.Info("FindObjectMouseDown");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mdl = ((MARSTestStepEditorModel)DataContext);
                if (mdl.SelectedTestStep == null)
                {
                    MessageBox.Show("Please active a row first", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                CaptureMouse();
                IsDragging = true;
                e.Handled = true;

                Mousecapture.SetHook(mouseMoveHookImple,
                        null,
                        mouseLeftButtonUpImp);
                if (FinderCursor == null)
                {
                    FinderCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("MarsSpyTool.Resources.icon-generate-testcase.cur"));
                }
                Cursor = FinderCursor;
            }
        }
        private Mouse_ObjectPosManagement CurrentObjectStays = new Mouse_ObjectPosManagement();

        public void mouseMoveHookImple(int x, int y)
        {
            //Console.WriteLine("mouseMoveHookImple");
            if (!Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.IsLeftMousePressed())
            {
                StopSnoopTargetsSearch();
                return;
            }
            //UpdateFeedbackWindowPosition();

            if (!IsDragging) return;
            if (CurrentObjectStays.PreviousTimeTickStopped == 0)
            {
                CurrentObjectStays.PreviousTimeTickStopped = DateTime.Now.Ticks;
                return;
            }
            CurrentObjectStays.HighlightObjectAtMousePosition(x, y, false);
            /// 判断停留时间
            /// 
            long lCurDis = DateTime.Now.Ticks - CurrentObjectStays.PreviousTimeTickStopped;
            if ((lCurDis / 10000) > 500) // greater than 500 ms, then get current 
            {
                CurrentObjectStays.PreviousTimeTickStopped = lCurDis;
                //绘制当前的window
            }

        }
        private void StopSnoopTargetsSearch()
        {
            //logger.Info("StopSnoopTargetsSearch\tbegin");
            ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
        }

        public void mouseLeftButtonUpImp(int x, int y)
        {
            Logger.Info($"mouseLeftButtonUpImp\tbegin|{x}|{y}|");
            try
            {
                Cursor = Cursors.Arrow;                

                if (!IsDragging) return;
                if (this.WindowState == WindowState.Minimized)
                    this.WindowState = WindowState.Normal;
                if (!TestStepsTab.IsSelected)
                {
                    TestStepsTab.IsSelected = true;
                }
                HighlightWindow.HideAndDestroy();
                // if record&replay should still hold the mouse                
                StopSnoopTargetsSearch();
                IsDragging = false;

                //算法：
                //获得当前位置的windows
                IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));
                //获得进程
                int pid;                
                if (MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out pid) == 0) return;
                Process p = Process.GetProcessById(pid);
                if (p.Id == Process.GetCurrentProcess().Id)
                    return;
                // 判断是不是一个process
                if (((MARSTestStepEditorModel)DataContext).HookedPId != p.Id)
                {
                    MessageBox.Show("Please drag the mouse to the previous process");                    
                    return;
                }
                MARSTestStepEditorModel mdl = (MARSTestStepEditorModel)DataContext;
                QueryObjectRequst req = new QueryObjectRequst();
                req.currentHandle = hwnd.ToInt64();
                req.x = x;
                req.y = y;
                req.typeOfGenerateSteps = mdl.GenerateObjType ? 1 : 0;
                bool isOk = false;
                
                string strError = "";
                List<MarsSpiedObjectBasicInfo> objs = RESTClient2MessageCenter
                                .getInstance()
                                .QueryCurrentObjects(req, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("StartEngineAgent\t" + $"can't query objects|with Error|{strError}");
                    return;
                }
                List<MARSTestStepsModel> lstTestStep = ((MARSTestStepEditorModel)DataContext).FilterObjectsAndBuildTestSteps(objs, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("StartEngineAgent\t" + $"can't filter objects|with Error|{strError}");
                    return;
                }
                
                /// 
                if (req.typeOfGenerateSteps == 1)
                {
                    /// then insert into 
                    ///                     
                    mdl.SetTestStep(lstTestStep,isFlash:true, insertPos:mdl.SelectedTestStep.Run_order);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"mouseLeftButtonUpImp\t{e.Message}\r\n{e.StackTrace}");
            }
            finally
            {
                Mousecapture.UnHookMouse();
                Logger.Info("mouseLeftButtonUpImp\tend");
            }
        }

        private void SelectedItemsChangedImpl(object sender, SelectionChangedEventArgs e)
        {
            MARSTestStepEditorModel mdl = (MARSTestStepEditorModel)DataContext;
            mdl.SeltectedTestList.Clear();

            foreach (var itm in this.testStepsDataGrid.SelectedItems)
            {
                if (itm == null) continue;
                if (itm is MARSTestStepsModel)
                    mdl.SeltectedTestList.Add((MARSTestStepsModel)itm);
            }
        }
    }
}
