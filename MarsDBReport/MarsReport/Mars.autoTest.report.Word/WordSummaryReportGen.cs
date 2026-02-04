using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using Microsoft.Office;
using MSWord = Microsoft.Office.Interop.Word;

using Microsoft.Office.Interop.Word;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Graph;
using Graph = Microsoft.Office.Interop.Graph;
using System.IO;
using System.Data;
using System.ComponentModel;

namespace Mars.autoTest.report.Word
{
    public class WordSummaryReportGen
    {
        private WordSummaryReportConfig config;

        private WordWrapper ww = new WordWrapper();

        public WordSummaryReportGen(WordSummaryReportConfig config)
        {
            this.config = config;
        }

        public void OpenDocument()
        {
            ww.OpenDocument(config.TemplateFilePath);
        }

        public void SaveDocument()
        {
            ww.SaveDocument(config.OutputFilePath);
        }

        private void ReplaceWordTags()
        {
            ww.ReplaceTag("MARS_PROJECT_COUNT", "" + config.MarsProjectCount);
            ww.ReplaceTag("MARS_SB_COUNT", "" + config.MarsStoryboardCount);
            ww.ReplaceTag("MARS_TC_COUNT", "" + config.MarsTestCaseCount);
            ww.ReplaceTag("MARS_TEST_STEP_COUNT", "" + config.MarsTestStepCount);

            ww.ReplaceTag("MARS_REP_GEN_DATE", "" + config.ReportGenDate);

            ww.ReplaceTag("MARS_B_SUCC", "" + config.MarsBSucc);
            ww.ReplaceTag("MARS_C_SUCC", "" + config.MarsCSucc);

            ww.ReplaceTag("MARS_B_FAIL", "" + config.MarsBFail);
            ww.ReplaceTag("MARS_B_UNPR", "" + config.MarsBUnpr);

            ww.ReplaceTag("MARS_C_FAIL", "" + config.MarsCFail);
            ww.ReplaceTag("MARS_C_UNPR", "" + config.MarsCUnpr);

            ww.ReplaceTag("MARS_B_PART", "" + config.MarsBPartial);
            ww.ReplaceTag("MARS_C_PART", "" + config.MarsCPartial);
        }

        private void CreateSummaryChart(string wordToFind)
        {
            MSWord.Range location = ww.GetLocation("[" + wordToFind + "]");
            location.Text = "";
            int passed = config.MarsCSucc;
            int failed = config.MarsCFail;
            int partial = config.MarsCPartial;
            int unprocessed = config.MarsCUnpr;
            //ww.CreatePieChart(location, passed, failed, partial, unprocessed);
            ww.CreatePieChartExcel(location, passed, failed, partial, unprocessed);
        }



        public static System.Data.DataTable ConvertToDataTable<T>(IEnumerable<T> data)
        {
            List<IDataRecord> list = data.Cast<IDataRecord>().ToList();

            PropertyDescriptorCollection props = null;
            System.Data.DataTable table = new System.Data.DataTable();
            if (list != null && list.Count > 0)
            {
                props = TypeDescriptor.GetProperties(list[0]);
                for (int i = 0; i < props.Count; i++)
                {
                    PropertyDescriptor prop = props[i];
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
            }
            if (props != null)
            {
                object[] values = new object[props.Count];
                foreach (T item in data)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = props[i].GetValue(item) ?? DBNull.Value;
                    }
                    table.Rows.Add(values);
                }
            }
            return table;
        }

        private void CreateProjectDetailSection()
        {
            // Find test case tag
            MSWord.Range tableLocation = ww.GetLocation("[" + "MARS_PROJECT_DETAILS" + "]");
            tableLocation.Text = "";
          
            // For each TC add header and table
            foreach (var proj in config.projects)
            {
                string heading = "Project: " + proj.projectName;
                ww.AddHeading(heading, 1, true);
                InsertProjectDetailTable(proj, heading);
                foreach(var sb in proj.sbList)
                {
                    string sbHeading = "Storyboard: " + sb.StoryBoardName;
                    ww.AddHeading(sbHeading, 2, true);
                    ww.ReplaceTag(sbHeading, sb.StoryBoardDescr);

                    sbHeading = "Testing Summary for " + sb.StoryBoardName;
                    ww.AddHeading(sbHeading, 3, true);
                    InsertSbTestingSummaryTable(sb, sbHeading);

                    sbHeading = "Report Summary for " + sb.StoryBoardName;
                    ww.AddHeading(sbHeading, 3, true);
                    InsertSbReportSummaryTable(sb, sbHeading);

                    sbHeading = "Chart " + sb.StoryBoardName;
                    ww.AddHeading(sbHeading, 3, true);
                    InsertSbChart(sb, sbHeading);

                    sbHeading = "Storyboard Details for " + sb.StoryBoardName;
                    ww.AddHeading(sbHeading, 3, true);
                    InsertSbDetailsTable(sb, sbHeading);
                }
            }
        }

        private void InsertProjectDetailTable(WordReportConfigProject proj, string heading)
        {
            MSWord.Table tbl = null;
            MSWord.Range tableLocation = ww.GetLocation("[" + heading + "]");
            tbl = ww.InsertTable(proj.ProjectStoryboardData, tableLocation);
            tbl.Columns[1].PreferredWidth = 25;
            tbl.Columns[2].PreferredWidth = 150;
            tbl.Columns[3].PreferredWidth = 150;

            // make table headings appear on every page
            tbl.Rows[1].HeadingFormat = -1;
        }

        private void InsertSbDetailsTable(WordReportConfigStoryboard sb, string sbHeading)
        {
            MSWord.Table tbl = null;
            MSWord.Range tableLocation = ww.GetLocation("[" + sbHeading + "]");
            tbl = ww.InsertTable(sb.StoryBoardData, tableLocation);
            tbl.Columns[1].PreferredWidth = 25;
            tbl.Columns[2].PreferredWidth = 100;
            tbl.Columns[3].PreferredWidth = 100;
            tbl.Columns[4].PreferredWidth = 80;
            tbl.Columns[5].PreferredWidth = 60;
            tbl.Columns[6].PreferredWidth = 90;
            tbl.Columns[7].PreferredWidth = 60;
            tbl.Columns[8].PreferredWidth = 90;
            tbl.Columns[9].PreferredWidth = 60;

            // make table headings appear on every page
            tbl.Rows[1].HeadingFormat = -1;
        }

        private void InsertSbChart(WordReportConfigStoryboard sb, string sbHeading)
        {
            MSWord.Range location = ww.GetLocation("[" + sbHeading + "]");
            location.Text = "";
            int passed = sb.successCount;
            int failed = sb.failCount;
            int partial = sb.partialCount;
            int unprocessed = sb.unprCount;
            ww.CreatePieChartExcel(location, passed, failed, partial, unprocessed);
        }

        private void InsertSbReportSummaryTable(WordReportConfigStoryboard sb, string sbHeading)
        {
            MSWord.Table tbl = null;
            MSWord.Range tableLocation = ww.GetLocation("[" + sbHeading + "]");
            tbl = ww.InsertTable(sb.StoryBoardReportData, tableLocation);

            tbl.Columns[1].PreferredWidth = 80;
            tbl.Columns[2].PreferredWidth = 100;
            tbl.Columns[3].PreferredWidth = 100;

            // make table headings appear on every page
            tbl.Rows[1].HeadingFormat = -1;
        }
    

        private void InsertSbTestingSummaryTable(WordReportConfigStoryboard sb, string sbHeading)
        {
            MSWord.Table tbl = null;
            MSWord.Range tableLocation = ww.GetLocation("[" + sbHeading + "]");
            tbl = ww.InsertTable(sb.StoryBoardTestingData, tableLocation);
            tbl.Columns[1].PreferredWidth = 20;
            tbl.Columns[2].PreferredWidth = 150;
            tbl.Columns[3].PreferredWidth = 70;

            // make table headings appear on every page
            tbl.Rows[1].HeadingFormat = -1;
        }

        private void CreateProjectTable()
        {
            ww.ChangeOrientation("MARS_CHANGE_TO_LANDSCAPE", WdOrientation.wdOrientLandscape);

            MSWord.Table tbl = ww.InsertTable(config.ProjectSummaryData, "MARS_PROJECTS");
            tbl.Columns[1].PreferredWidth = 25;
            tbl.Columns[2].PreferredWidth = 150;
            tbl.Columns[3].PreferredWidth = 200;

            // make table headings appear on every page
            tbl.Rows[1].HeadingFormat = -1;
        }


        // Main Document Generation driver
        public void GenerateDocument()
        {
            ReplaceWordTags();
            CreateSummaryChart("MARS_PROJECT_COMPLETION_CHART");
            CreateProjectTable();
            CreateProjectDetailSection();

            ww.CreateTOC("MARS_TOC");
            ww.CloseExcelApps();
        }
    
    }
}
