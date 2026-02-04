using com.Mars.Constants;
using MarsTestFrame.CommuniteServer;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QtpStarter.Info
{
    public delegate void FormIsRead();
    public delegate void TestSuiteTestCaseReadyEvent(string strTestSuiteName, string strTestCase);

    public delegate void MonitorFormCloseEvent();

    //public partial class TestStepHintForm : MarsShellLib.MarsAppDeskTopToolBar
    public partial class TestStepHintForm : Form
    {
        //private TestStepHintForm gMonitorForm = null;
        private static bool isLoad = false;

        private static MLogger Logger = MLogger.GetLogger(typeof(TestStepHintForm));

        private FormIsRead mFormReadyEventHandler = null;
        private MonitorFormCloseEvent mMonitorFormCloseEventHandler = null;

        private static TestStepHintForm gMonitorForm = null;

        private Thread mobjThrd = null;
        private int uCallBackMsg;

        public TestStepHintForm()
        {
            InitializeComponent();
        }

        [STAThread]
        internal static TestStepHintForm GetInstance()
        {
            if (!isLoad) //return new TestStepHintForm();
            {

                if (gMonitorForm == null)
                {
                    //Application objNew = Application.
                    //Application.Run(gMonitorForm = new TestStepHintForm());    
                    gMonitorForm = new TestStepHintForm();                    
                }
                isLoad = true;
                /*
                Logger.Info("---CreateAppBar", "Begin....");
                //(gMonitorForm.mobjThrd = new Thread(new ThreadStart(gMonitorForm.StartMonitorAndUpdate))).Start();
                gMonitorForm.Show();
                
                Thread.Sleep(50);
                gMonitorForm.Update();
                Thread.Sleep(50);
                 * */
            }

            gMonitorForm.Show();
            Thread.Sleep(50);
            gMonitorForm.Update();
            Thread.Sleep(50);
            Logger.Info("---CreateAppBar", "End");
            return gMonitorForm;

        }

        private void StartMonitorAndUpdate()
        {
            if (gMonitorForm == null) return;
            gMonitorForm.Show();
            Thread.Sleep(100);

        }


        #region eventhandler out

        public void AddFormCloseEventCloseHander(MonitorFormCloseEvent funcCloseEventHandler)
        {
            mMonitorFormCloseEventHandler += funcCloseEventHandler;
        }

        public void RefillTestStepsGrid(List<TestStep4Services> lstSteps)
        {
            if (lstSteps == null) return;
            Logger.Info("RefillTestStepsGrid", string.Format("total [{0}] Items to Insert", lstSteps.Count));

            this.StatusGrid.Rows.Clear();
            string strFormatStr = "{0,-" + SystemConstant.CNST_CLIENT_GRID_HINT_MXLENGHT + "}";
            string strRuningInfo = "";
            for (int i = 0; i < lstSteps.Count; i++)
            {
                strRuningInfo = string.Format(strFormatStr + "{1}\n", SystemConstant.CNST_CLIENT_GRID_KEYWORD, lstSteps[i].Keyword);
                strRuningInfo = string.Format("{2}" + strFormatStr + "{1,-12}\n", SystemConstant.CNST_CLIENT_GRID_OBJECT, string.IsNullOrEmpty(lstSteps[i].ObjectName) ? "<NOT Set>" : lstSteps[i].ObjectName, strRuningInfo);
                strRuningInfo = string.Format("{2}" + strFormatStr + "{1,-12}", SystemConstant.CNST_CLIENT_GRID_RC, string.IsNullOrEmpty(lstSteps[i].Row_Column) ? "<NOT Set>" : lstSteps[i].Row_Column, strRuningInfo);

                /** add one to Grid */
                this.StatusGrid.Rows.Add();
                this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.LightBlue : Color.LightGray;

                this.StatusGrid.Rows[i].Cells[1].Value = strRuningInfo;
                //this.StatusGrid.Rows[this.StatusGrid.Rows.Count - 1].Height = 80;
                this.StatusGrid.Rows[i].Height = 48;
                this.StatusGrid.Rows[i].Cells[2].Value = "-";
                if (this.StatusGrid.Rows[i].Cells[0] is DataGridViewCheckBoxCell)
                {
                    DataGridViewCheckBoxCell objCheckBoxCell = (DataGridViewCheckBoxCell)this.StatusGrid.Rows[i].Cells[0];
                    objCheckBoxCell.Value = false;
                    this.StatusGrid.Rows[i].Cells[0].Tag = lstSteps[i];
#if _tigerDebug
                    this.StatusGrid.Update();
#endif
                }
#if _tigerDebug
                Thread.Sleep(200);
#endif
            }
            this.StatusGrid.Update();
            Thread.Sleep(1000);
        }
        #endregion

        private void RegisterAppBar(bool registered)
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = this.Handle;
            if (!registered)
            {
                //register
                uCallBackMsg = APIWrapper.RegisterWindowMessage("APPBARMSG_CSDN_HELPER");
                abd.uCallbackMessage = uCallBackMsg;
                uint ret = APIWrapper.SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
            }
            else
            {
                APIWrapper.SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
            }
        }
        


        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void TestSuiteInfoEventHandler(string strTestSuite, string strTestCase)
        {
            if (this.TSTCGrid.RowCount < 2)
                LoadDefaultTSTCGridInfo();
            this.TSTCGrid.Rows[0].Cells[1].Value = strTestSuite;
            this.TSTCGrid.Rows[1].Cells[1].Value = strTestCase;
            this.TSTCGrid.Update();
        }

        public void SetFormReadEventHandler(FormIsRead funcReady)
        {
            this.mFormReadyEventHandler += funcReady;
        }

        private void TestStepHintForm_Load(object sender, EventArgs e)
        {
            //this.Edge = AppBarEdges.Left;
            this.RegisterAppBar(false);

            LoadDefaultTSTCGridInfo();
        }

        private void LoadDefaultTSTCGridInfo()
        {
            while (this.TSTCGrid.Rows.Count < 2)
            {
                this.TSTCGrid.Rows.Add();
            }
            this.TSTCGrid.Rows[0].Cells[0].Value = "TS";
            this.TSTCGrid.Rows[0].DefaultCellStyle.BackColor = Color.LightBlue;
            this.TSTCGrid.Rows[1].Cells[0].Value = "TC";
            this.TSTCGrid.Rows[1].DefaultCellStyle.BackColor = Color.LightBlue;
        }

        private void TSTCGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void TestStepHintForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.RegisterAppBar(true);

            if (this.mMonitorFormCloseEventHandler != null)
                this.mMonitorFormCloseEventHandler();
            //gMonitorForm = null;

        }
    }


    public class APIWrapper
    {
        [DllImport("SHELL32", CallingConvention = CallingConvention.StdCall)]
        public static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string msg);
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }
    public enum ABMsg : int
    {
        ABM_NEW = 0,
        ABM_REMOVE,
        ABM_QUERYPOS,
        ABM_SETPOS,
        ABM_GETSTATE,
        ABM_GETTASKBARPOS,
        ABM_ACTIVATE,
        ABM_GETAUTOHIDEBAR,
        ABM_SETAUTOHIDEBAR,
        ABM_WINDOWPOSCHANGED,
        ABM_SETSTATE
    }
    public enum ABNotify : int
    {
        ABN_STATECHANGE = 0,
        ABN_POSCHANGED,
        ABN_FULLSCREENAPP,
        ABN_WINDOWARRANGE
    }
    public enum ABEdge : int
    {
        ABE_LEFT = 0,
        ABE_TOP,
        ABE_RIGHT,
        ABE_BOTTOM
    }
}
