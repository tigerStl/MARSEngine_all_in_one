using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;
using System.IO;

namespace MARS.CompareGUI.GUI
{
    public partial class QueryResultForm : Form
    {
        DataTable ResultDataTable;

        public QueryResultForm(DataTable dt)
        {
            ResultDataTable = dt;
            InitializeComponent();

            this.ResultDataGridView.RowsDefaultCellStyle.BackColor = Color.Bisque;
            this.ResultDataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.Beige;
            ResultDataGridView.DataSource = dt;
            this.ResultDataGridView.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(ResultDataGridView_RowPostPaint);
        }

        public static QueryResultForm Create(DataTable dt)
        {
            QueryResultForm form = new QueryResultForm(dt);
            return form;
        }

        private void ResultDataGridView_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(ResultDataGridView.RowHeadersDefaultCellStyle.ForeColor))
            {
                e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.InheritedRowStyle.Font, b, e.RowBounds.Location.X + 10, e.RowBounds.Location.Y + 4);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.ShowDialog();

            string fileName = saveFileDialog1.FileName;

            if (fileName.Length > 5)
               SaveDataTableToCSV( ResultDataTable, ',', fileName);
            // ResultDataTable
        }

        public  void  SaveDataTableToCSV(DataTable datatable, char seperator, string CsvFpath)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < datatable.Columns.Count; i++)
            {
                sb.Append(datatable.Columns[i]);
                if (i < datatable.Columns.Count - 1)
                    sb.Append(seperator);
            }
            sb.AppendLine();
            foreach (DataRow dr in datatable.Rows)
            {
                for (int i = 0; i < datatable.Columns.Count; i++)
                {
                    sb.Append(dr[i].ToString());

                    if (i < datatable.Columns.Count - 1)
                        sb.Append(seperator);
                }
                sb.AppendLine();
            }
            System.IO.StreamWriter csvFileWriter = new System.IO.StreamWriter(CsvFpath, false);
            csvFileWriter.WriteLine(sb.ToString());
            csvFileWriter.Flush();
            csvFileWriter.Close();
        }


        private void SaveToCSV(string fileName)
        {
            string CsvFpath = fileName;
            try
            {
                System.IO.StreamWriter csvFileWriter = new System.IO.StreamWriter(CsvFpath, false);

                string columnHeaderText = "";

                int countColumn = ResultDataGridView.ColumnCount - 1;

                if (countColumn >= 0)
                {
                    columnHeaderText = ResultDataGridView.Columns[0].HeaderText;
                }

                for (int i = 1; i <= countColumn; i++)
                {
                    columnHeaderText = columnHeaderText + ',' + ResultDataGridView.Columns[i].HeaderText;
                }


                csvFileWriter.WriteLine(columnHeaderText);

                foreach (DataGridViewRow dataRowObject in ResultDataGridView.Rows)
                {
                    if (!dataRowObject.IsNewRow)
                    {
                        string dataFromGrid = "";

                        dataFromGrid = dataRowObject.Cells[0].Value.ToString();

                        for (int i = 1; i <= countColumn; i++)
                        {
                            dataFromGrid = dataFromGrid + ',' + dataRowObject.Cells[i].Value.ToString();

                            csvFileWriter.WriteLine(dataFromGrid);
                        }
                    }
                }


                csvFileWriter.Flush();
                csvFileWriter.Close();
            }
            catch (Exception exceptionObject)
            {
                MessageBox.Show(exceptionObject.ToString());
            }
        }

        private void saveAsExcelWorkbookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.ShowDialog();

            string fileName = saveFileDialog1.FileName;

            if (fileName.Length > 5)
                SaveDataTableToExcel(ResultDataTable,  fileName);
        }

        private void SaveDataTableToExcel(DataTable resultDataTable, string fileName)
        {
            XLWorkbook wb = new XLWorkbook();
            wb.Worksheets.Add(resultDataTable, "Result");
            if (File.Exists(fileName))
                File.Delete(fileName);

            wb.SaveAs(fileName, false);
        }
    }
}
