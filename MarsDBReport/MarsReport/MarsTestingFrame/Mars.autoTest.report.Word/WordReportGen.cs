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
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.autoTest.report.Word
{
    public class WordReportGen
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(WordReportGen));
        private WordReportConfig config;
        private WordWrapper ww = new WordWrapper();
       
        public WordReportGen(WordReportConfig config)
        {
            this.config = config;
        }

        public void OpenDocument()
        {
            ww.OpenDocument(config.TemplateFilePath);
        }

        public void SaveDocument()
        {
            Logger.logBegin("SaveDocument");
           
            ww.CloseExcelApps();
            ww.SaveDocument(config.OutputFilePath);
            Logger.logEnd("SaveDocument");
        }

        private void ReplaceWordTags()
        {
            ww.ReplaceTag("MARS_PROJECT", config.ProjectName);
            ww.ReplaceTag("MARS_PROJECT_DESCRIPTION", config.ProjectDescription);
            ww.ReplaceTag("MARS_STORYBOARD", config.StoryBoardConfig.StoryBoardName);
            ww.ReplaceTag("MARS_STORYBOARD_DESCRIPTION", config.StoryBoardConfig.StoryBoardDescr);
            ww.ReplaceTag("MARS_STORYBOARDNAME", config.StoryBoardConfig.StoryBoardName);
            ww.ReplaceTag("MARS_TS_COUNT", "" + config.MarsTSCount);
            ww.ReplaceTag("MARS_TC_COUNT", "" + config.MarsTCCount);
            ww.ReplaceTag("MARS_TEST_STEP_COUNT", "" + config.MarsTestStepCount);
            ww.ReplaceTag("MARS_REP_GEN_DATE", "" + config.ReportGenDate);

            ww.ReplaceTag("MARS_B_SUCC", "" + config.MarsBSucc);
            ww.ReplaceTag("MARS_C_SUCC", "" + config.MarsCSucc);
            ww.ReplaceTag("MARS_B_FAIL", "" + config.MarsBFail);
            ww.ReplaceTag("MARS_B_UNPR", "" + config.MarsBUnpr);
            ww.ReplaceTag("MARS_C_FAIL", "" + config.MarsCFail);
            ww.ReplaceTag("MARS_C_UNPR", "" + config.MarsCUnpr);
            ww.ReplaceTag("MARS_B_PART", "" + 0);
            ww.ReplaceTag("MARS_C_PART", "" + config.MarsCPartial);

        }


        private void CreateTestCompletionChart(string wordToFind)
        {
            MSWord.Range location = ww.GetLocation("[" + wordToFind + "]");
            location.Text = "";
            int passed = config.MarsCSucc;
            int failed = config.MarsCFail;
            int partial = config.MarsCPartial;
            int unprocessed = config.MarsCUnpr;
            ww.CreatePieChartExcel(location, passed, failed, partial, unprocessed);
        }


        private void CreateStoryboardTable()
        {
            ww.ChangeOrientation("MARS_CHANGE_TO_LANDSCAPE", WdOrientation.wdOrientLandscape);

            MSWord.Table tbl = ww.InsertTable(config.StoryBoardConfig.StoryBoardData, "MARS_STORYBOARD_TABLE");
            /*
            tbl.Columns[1].PreferredWidth = 25;    // #
            tbl.Columns[2].PreferredWidth = 130;   // TC_Name
            tbl.Columns[3].PreferredWidth = 100;   // TS_Name
            tbl.Columns[4].PreferredWidth = 100;   // Data_Set
            tbl.Columns[5].PreferredWidth = 50;    // Result
            tbl.Columns[6].PreferredWidth = 100;   // BL Start
            tbl.Columns[7].PreferredWidth = 100;   // BL Duration
            */

            tbl.Columns[1].PreferredWidth = 25;    // #
            tbl.Columns[2].PreferredWidth = 130;   // TC_Name
            tbl.Columns[3].PreferredWidth = 80;    // TS_Name
            tbl.Columns[4].PreferredWidth = 90;   // Data_Set
            tbl.Columns[5].PreferredWidth = 60;    // BL Start
            tbl.Columns[6].PreferredWidth = 50;    // BL Duration
            tbl.Columns[7].PreferredWidth = 60;    // CP Start
            tbl.Columns[8].PreferredWidth = 50;    // CP Duration
            tbl.Columns[9].PreferredWidth = 50;    // BL Result
            tbl.Columns[10].PreferredWidth = 60;   // CP Result
        }

        private void CreateTestCaseSection()
        {
            // Find test case tag
            MSWord.Range tableLocation = ww.GetLocation("[" + "MARS_TEST_CASES" + "]");
            tableLocation.Text = "";
            MSWord.Table tbl = null; ;

            // For each TC add header and table
            foreach (var tc in config.TestCaseConfigList)
            {
                string heading = "Test Case: " + tc.TestCaseName + "          Data Set: " + tc.DataSetName;
                ww.AddHeading(heading, 1, true);
                tableLocation = ww.GetLocation("[" + heading + "]");

                //AF
                tableLocation.InsertBefore( "\r DS Description: " + tc.DataSetDescr + "\r");
                tableLocation = ww.GetLocation("[" + heading + "]");
                //AF

                tbl = ww.InsertTable(tc.TestCaseData, tableLocation);

                // make table headings appear on every page
                tbl.Rows[1].HeadingFormat = -1;

                tbl.Columns[1].PreferredWidth = 25;
                tbl.Columns[2].PreferredWidth = 100;
                tbl.Columns[3].PreferredWidth = 100;
                tbl.Columns[4].PreferredWidth = 80;
                tbl.Columns[5].PreferredWidth = 100;
                tbl.Columns[6].PreferredWidth = 75;
                tbl.Columns[7].PreferredWidth = 75;
                tbl.Columns[8].PreferredWidth = 60;
                tbl.Columns[9].PreferredWidth = 40;
            }
        }
        

        private void CreatePictureAppendixSection()
        {
            ww.ChangeOrientationAtEnd(WdOrientation.wdOrientPortrait);

            ww.AddHeading("Appendix 1 - Pictures", 0, true);

           
            foreach (MSWord.Range rng in ww.pictureRangeList)
            {

                string cleanText = new string(rng.Text.Where(c => !char.IsControl(c)).ToArray());
                string heading = ww.CreateUserReadablePictureHeading(cleanText);
                string pictureFileName = cleanText;

                // Add heading
                string newHeading = ww.AddHeading(heading + "\r\a", 1, false);

                // Find location of corresponding tag
                MSWord.Range location = ww.GetLocation("[" + heading + "]");
                location.Text = "";

                // Insert pictures for baseline and compare
                FileInfo fi = new FileInfo(pictureFileName);
                string b_fileName = "B" +  fi.Name;
                string c_fileName = "C" + fi.Name;
                string folderName = fi.DirectoryName;

                //location.InlineShapes.AddPicture(pictureFileName);
               
                location.InlineShapes.AddPicture(folderName + "\\" + c_fileName);
                location.InlineShapes.AddPicture(folderName + "\\" + b_fileName);

                // Insert crossreference into table
                newHeading = new string(newHeading.Where(c => !char.IsControl(c)).ToArray());
                ww.InsertCrossReference(rng, newHeading);
            
            }
        }
        
        // Main Document Generation driver
        public void GenerateDocument()
        {
            ReplaceWordTags();
            CreateTestCompletionChart("MARS_COMPLETION_CHART");
            CreateStoryboardTable();
            CreateTestCaseSection();
            CreatePictureAppendixSection();
            ww.CreateTOC("MARS_TOC");
        }

    }
}
