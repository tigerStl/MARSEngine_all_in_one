extern alias clientWCF;

using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using Mars.AutoTestingDriver.ExecuteTestcase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mars.AutoTestingDriver.ExecuteStoryboard
{
    public partial class TestStepsNavigator : Form
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestStepsNavigator));

        MARSRecoverMgr currentRecoverMgr = null;
        public TestStepsNavigator()
        {
            InitializeComponent();
        }

        public string ErrorInfo
        {
            set => this.errorLbl.Text = value; 
            get => this.errorLbl.Text; 
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        internal void setTestStepsInfo(MARSRecoverMgr objRecoverMgr)
        {
            this.currentRecoverMgr = objRecoverMgr;

            /// 在grid中显示
            /// 
            if (objRecoverMgr == null) return;
            if (objRecoverMgr.currentSteps == null) return;
            this.testStepGrid.Rows.Clear();
            DataGridViewRow tmpRow = null;

            int idxError = -1;
            for (int i = 0; i < objRecoverMgr.currentSteps.Count; i++)
            {
                if (this.testStepGrid.Rows.Count <= 0)
                {
                    this.testStepGrid.Rows.Add();                    
                }
                tmpRow = (DataGridViewRow)this.testStepGrid.Rows[0].Clone();

                int idx = this.testStepGrid.Rows.Add(tmpRow);
                //int iIdx = this.testStepGrid.Rows.Add();
                //Logger.Info("setTestStepsInfo", $"row idx,[{objRecoverMgr.currentSteps[i].Keyword}]/{i} ");
                tmpRow = this.testStepGrid.Rows[idx];
                tmpRow.Cells[runOrder.Index].Value = objRecoverMgr.currentSteps[i].RunId;
                tmpRow.Cells[keywordColumn.Index].Value = objRecoverMgr.currentSteps[i].Keyword;
                tmpRow.Cells[happyNameColumn.Index].Value = objRecoverMgr.currentSteps[i].ObjectName;
                tmpRow.Cells[objectDetailColumn.Index].Value = objRecoverMgr.currentSteps[i].StepObject?.QUICK_ACCESS;
                tmpRow.Cells[runOrder.Index].Value = objRecoverMgr.currentSteps[i].RunId;
                tmpRow.Cells[dataColumn.Index].Value = objRecoverMgr.currentSteps[i].DATA_VALUE;
                tmpRow.Cells[parameterColumn.Index].Value = objRecoverMgr.currentSteps[i].GetPara();
                if ((objRecoverMgr.currentSteps[i].StepData != null)
                    && (objRecoverMgr.currentSteps[i].StepData.DATA_DIRECTION!=null)
                    && (objRecoverMgr.currentSteps[i].StepData.DATA_DIRECTION == 4))
                {
                    tmpRow.Cells[IsSkipColumn.Index].Value = true;
                }
                else
                {
                    tmpRow.Cells[IsSkipColumn.Index].Value = false;
                }
                tmpRow.Tag = objRecoverMgr.currentSteps[i];
                if (objRecoverMgr.currentStep != null)
                {
                    if (objRecoverMgr.currentSteps[i].RunId == objRecoverMgr.currentStep.RunId)
                    {
                        tmpRow.DefaultCellStyle.BackColor = Color.Red;
                        tmpRow.Cells[statusColumn.Index].Value = "FAILED";
                        idxError = tmpRow.Index;
                    }
                    else
                    {
                        if (objRecoverMgr.currentSteps[i].RunId < objRecoverMgr.currentStep.RunId)
                        {
                            tmpRow.Cells[statusColumn.Index].Value = "DONE";
                            tmpRow.Cells[testResultColumn.Index].Value =  "n/a";
                        }
                    }

                }
                else
                {
                    ///第一句出错
                    ///
                    if (i == 0)
                    {
                        tmpRow.DefaultCellStyle.BackColor = Color.Red;
                        tmpRow.Cells[statusColumn.Index].Value = "FAILED";
                        idxError = tmpRow.Index;
                    }
                    else
                    {
                        tmpRow.Cells[statusColumn.Index].Value = "DONE";
                        tmpRow.Cells[testResultColumn.Index].Value = "n/a";
                    }
                }
                //tmpRow.Cells[statusColumn.Index].value
               
            }
            resizeCols();
            this.testStepGrid.FirstDisplayedScrollingRowIndex = idxError < 0 ? 0 : ((idxError - 1) < 0 ? idxError : idxError - 1);
        }

        internal void resizeCols()
        {
            int colCount = this.testStepGrid.Columns.Count; // this returns the total number of columns (=6)
                                                             //MessageBox.Show(colCount.ToString());
            colCount = colCount - 1; // =5
            for (int i = 0; i < colCount; i++)
            {
                DataGridViewColumn column = testStepGrid.Columns[i]; // column[1] selects the required column 
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; // sets the AutoSizeMode of column defined in previous line
                int colWidth = column.Width; // store columns width after auto resize
                                             //MessageBox.Show(colWidth.ToString()); // show me the autoresize width (used as a visual check really)

                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // set the column resize mode to 'none' to allow manual/program changes
                colWidth += 20; // add 20 pixels to what 'colWidth' already is
                this.testStepGrid.Columns[i].Width = colWidth; // set the columns width to the value stored in 'colWidth'
            }
        }

        private void testStepGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //
            if (this.testStepGrid.Rows[e.RowIndex].Tag == null) return;

        }

        private void restartButton_Click(object sender, EventArgs e)
        {
            if (wcfClient.WcfClientAgent.IsWcfOffLine())
            {
                wcfClient.WcfClientAgent.ReconnectTo();
            }
            if (this.testStepGrid.SelectedRows.Count!=1)
            {
                MessageBox.Show("Please select Only One row");
                return;
            }
            this.currentRecoverMgr.restoredFrom = this.testStepGrid.SelectedRows[0].Tag as ExecutableTestCaseStep;
            this.DialogResult = DialogResult.OK;
            //Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void testStepGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (this.testStepGrid.Rows[e.RowIndex].Tag==null)
            {
                lblHint.Text = "No such row Index";
                return;
            }

            ExecutableTestCaseStep curStep = this.testStepGrid.Rows[e.RowIndex].Tag as ExecutableTestCaseStep;
            if (curStep == null)
            {
                lblHint.Text = "Row object is not a test step object";
                return;
            }
            string strOldValue = "";
            if (e.ColumnIndex == this.dataColumn.Index)
            {
                strOldValue = curStep.GetData();
                var tmpCellValue = this.testStepGrid.Rows[e.RowIndex].Cells[this.dataColumn.Index].Value;
                curStep.SetData( tmpCellValue ==null? "":tmpCellValue.ToString());
                lblHint.Text = $"Data column is changed from [{strOldValue}] to [{curStep.GetData()}]";
                return;
            }

            if (e.ColumnIndex == this.parameterColumn.Index)
            {
                strOldValue = curStep.GetPara();
                var tmpCellValue = this.testStepGrid.Rows[e.RowIndex].Cells[this.parameterColumn.Index].Value;
                curStep.setPara(tmpCellValue == null ? "" : tmpCellValue.ToString());
                lblHint.Text = $"Para column is changed from [{strOldValue}] to [{curStep.GetPara()}]";
                return;
            }

            if (e.ColumnIndex == this.objectDetailColumn.Index)
            {
                strOldValue = curStep.StepObject?.QUICK_ACCESS;
                var tmpCellValue = this.testStepGrid.Rows[e.RowIndex].Cells[this.objectDetailColumn.Index].Value;
                curStep.SetObjectDetailInfo(tmpCellValue == null ? "" : tmpCellValue.ToString());
                lblHint.Text = $"Object Detail column is changed from [{strOldValue}] to [{curStep.GetPara()}]";
                return;
            }
        }

        private void TestStepsNavigator_Shown(object sender, EventArgs e)
        {
            this.BringToFront();
        }
    }
}
