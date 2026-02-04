using Mars.message.Inter.MQCenter.HttpRestService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Animation;

namespace MarsSpyTool.subToolWindows.replayTestStep
{

    public enum MarsTestStep_replay_action
    {
        None = 0,
        saveAndRetry,
        Retry,
        Ignore,
        StopReplay
    }
    public partial class ReplyTestStepInfoForm : Form
    {

        public MarsTestStep_replay_action ReplayAction = MarsTestStep_replay_action.None;

        public ReplyTestStepInfoForm()
        {
            InitializeComponent();
        }
        private const int cnst_indent_padding_left = 10;

        private string StatusMessage = null;
        public void setStatusMessage(string strStatusMessage) { 
            StatusMessage = strStatusMessage;
            statusLabel.Text = StatusMessage;
        }

        public void loadReplayTestStep(MarsRecordReplayStep stp)
        {
            if (stp == null) { return; }
            int iRowIdx = this.dataGridView1.Rows.Add("Keyword", stp.keyWord);
            this.dataGridView1.Rows[iRowIdx].Tag = "Keyword";
            iRowIdx = this.dataGridView1.Rows.Add("Parent Window info", "");
            this.dataGridView1.Rows[iRowIdx].Tag = stp.pegQuickAccess;
            this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.Silver;
            this.dataGridView1.Rows[iRowIdx].ReadOnly = true;
            //DataGridView parentWindowChildGrid = new DataGridView
            //{
            //    Dock = DockStyle.Fill,
            //    AutoGenerateColumns = false,
            //    ColumnCount = 2,
            //    Visible = true,
            //    Height = 120,
            //    BackgroundColor = Color.LightGray,
            //};
            //this.dataGridView1.Controls.Add(parentWindowChildGrid);
            //var tmpLocation = dataGridView1.GetCellDisplayRectangle(0, iRowIdx, true).Location;
            //parentWindowChildGrid.Columns[0].Name = "Catalog";
            //parentWindowChildGrid.Columns[1].Name = "Value";
            if (stp.pegQuickAccess != null)
            {
                iRowIdx = this.dataGridView1.Rows.Add("SwfName", stp.pegQuickAccess.objectName);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, -5, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "Pegwin:objectName";
                iRowIdx = this.dataGridView1.Rows.Add("SwfName Path", stp.pegQuickAccess.objectNamePath);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, -5, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "Pegwin:objectNamePath";
                iRowIdx = this.dataGridView1.Rows.Add("Text", stp.pegQuickAccess.Text);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "Pegwin:Text)";
                iRowIdx = this.dataGridView1.Rows.Add("Object Type", stp.pegQuickAccess.objectType);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "Pegwin:objectType";
                iRowIdx = this.dataGridView1.Rows.Add("SwfType Path", stp.pegQuickAccess.objectTypePath);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "Pegwin:objectTypePath";
                iRowIdx = this.dataGridView1.Rows.Add("Is Child Window", stp.pegQuickAccess.isChildWindow);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "Pegwin:isChildWindow";
                iRowIdx = this.dataGridView1.Rows.Add("Is Owned Window", stp.pegQuickAccess.isOwnedWindow);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "Pegwin:isOwnedWindow";
            }
            if (stp.objectQuickAccess != null)
            {
                iRowIdx = this.dataGridView1.Rows.Add("Object Info", "");
                this.dataGridView1.Rows[iRowIdx].Tag = stp.objectQuickAccess;
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.DeepSkyBlue;
                this.dataGridView1.Rows[iRowIdx].ReadOnly = true;
                iRowIdx = this.dataGridView1.Rows.Add("SwfName", stp.objectQuickAccess.objectName);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.SkyBlue;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, -5, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "object:objectName";
                iRowIdx = this.dataGridView1.Rows.Add("SwfName Path", stp.objectQuickAccess.objectNamePath);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.SkyBlue;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, -5, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "object:objectNamePath";
                iRowIdx = this.dataGridView1.Rows.Add("Text", stp.objectQuickAccess.Text);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.SkyBlue;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "object:Text)";
                iRowIdx = this.dataGridView1.Rows.Add("Object Type", stp.objectQuickAccess.objectType);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.SkyBlue;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "object:objectType";
                iRowIdx = this.dataGridView1.Rows.Add("SwfType Path", stp.objectQuickAccess.objectTypePath);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.SkyBlue;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "object:objectTypePath";
                iRowIdx = this.dataGridView1.Rows.Add("Is Child Window", stp.objectQuickAccess.isChildWindow);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.SkyBlue;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "object:isChildWindow";
                iRowIdx = this.dataGridView1.Rows.Add("Is Owned Window", stp.objectQuickAccess.isOwnedWindow);
                this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.SkyBlue;
                this.dataGridView1.Rows[iRowIdx].Cells[0].Style.Padding = new Padding(cnst_indent_padding_left, 0, 0, 0);
                this.dataGridView1.Rows[iRowIdx].Tag = "object:isOwnedWindow";
            }
            iRowIdx = this.dataGridView1.Rows.Add("Data", stp.opText);
            this.dataGridView1.Rows[iRowIdx].Tag = "Data";
            iRowIdx = this.dataGridView1.Rows.Add("SwfType", stp.objectMarsType);
            this.dataGridView1.Rows[iRowIdx].Tag = "SwfType";
            this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.Silver;
            iRowIdx = this.dataGridView1.Rows.Add("Table::Row ID", stp.tableExtension_RowId);
            this.dataGridView1.Rows[iRowIdx].Tag = "tableExtension_RowId";
            iRowIdx = this.dataGridView1.Rows.Add("Table::Column", stp.tableExtension_column);
            this.dataGridView1.Rows[iRowIdx].Tag = "tableExtension_column";
            this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.Silver;
            iRowIdx = this.dataGridView1.Rows.Add("Table::Cell Text", stp.tableExtension_text);
            this.dataGridView1.Rows[iRowIdx].Tag = "tableExtension_text";
            this.dataGridView1.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.Silver;
        }

        private void ReplyTestStepInfoForm_Load(object sender, EventArgs e)
        {
            int screenWidth     = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight    = Screen.PrimaryScreen.WorkingArea.Height;

            // 设置窗口的位置为屏幕的左下角
            this.StartPosition  = FormStartPosition.Manual;
            this.Location       = new Point(0, screenHeight - this.Height);
            //BringToFront();

            this.TopMost = true;
        }

        private void UpdateAndRetryButton_Click(object sender, EventArgs e)
        {
            this.ReplayAction = MarsTestStep_replay_action.saveAndRetry;
            Close();
        }

        private void RetryButton_Click(object sender, EventArgs e)
        {
            this.ReplayAction = MarsTestStep_replay_action.Retry;
            Close();
        }

        private void IgnoreButtonClick_Click(object sender, EventArgs e)
        {
            this.ReplayAction = MarsTestStep_replay_action.Ignore;
            Close();
        }

        private void StopReplayButtonClick_Click(object sender, EventArgs e)
        {
            this.ReplayAction = MarsTestStep_replay_action.StopReplay;
            Close();
        }
    }
}
