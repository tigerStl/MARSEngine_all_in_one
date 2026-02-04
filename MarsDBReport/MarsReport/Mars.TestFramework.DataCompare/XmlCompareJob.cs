using ColorCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Mars.TestFramework.DataCompare
{
    public class XmlCompareJob
    {
        string _inputFileName1;
        string _inputFileName2;
        string _outputFileName;

        string _intermediateFileName1;
        string _intermediateFileName2;

        string _finalFileName1;
        string _finalFileName2;

        string _finalFileNameHtml1;
        string _finalFileNameHtml2;

        string workDiectoryName = @"..\work";

        //int stepCompleted = 0;

        string htmlHeader = @"<!DOCTYPE html><html><head>  <title>Hi there</title></head><body>";
        string htmlTail = @"</body></html>";
        ExcelWrapper eWrapper;
        public XmlCompareJob(string inputFileName1, string inputFileName2, string outputFileName)
        {
            _inputFileName1 = inputFileName1;
            _inputFileName2 = inputFileName2;
            _outputFileName = outputFileName;

            _intermediateFileName1 = workDiectoryName + "\\Interm1.xml";
            _intermediateFileName2 = workDiectoryName + "\\Interm2.xml";

            _finalFileName1 = workDiectoryName + "\\Final1.xml";
            _finalFileName2 = workDiectoryName + "\\Final2.xml";

            _finalFileNameHtml1 = workDiectoryName + "\\Final1.html";
            _finalFileNameHtml2 = workDiectoryName + "\\Final2.html";

            bool exists = System.IO.Directory.Exists(workDiectoryName);

            if (!exists)
                System.IO.Directory.CreateDirectory(workDiectoryName);
            else
            {
                DeleteFile(_intermediateFileName2);
                DeleteFile(_intermediateFileName1);
                DeleteFile(_finalFileName1);
                DeleteFile(_finalFileName2);
                DeleteFile(_finalFileNameHtml1);
                DeleteFile(_finalFileNameHtml2);

            }


            System.IO.Directory.SetCurrentDirectory(workDiectoryName);
        }

        private void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void Execute()
        {
            JobContext.DisplayStopwatch("Start Compare");
            Compare();
            JobContext.DisplayStopwatch("End   Compare");
            JobContext.DisplayStopwatch("Start FormatHtml");
            FormatHtml();
            JobContext.DisplayStopwatch("End FormatHtml");
            OpenExcel();

            //stepCompleted = 1;
            JobContext.DisplayStopwatch("Start PasteIntoExcel 1");
            PasteIntoExcel(1, _finalFileNameHtml1);
            JobContext.DisplayStopwatch("End PasteIntoExcel 1");
            JobContext.DisplayStopwatch("Start PasteIntoExcel 2");
            PasteIntoExcel(2, _finalFileNameHtml2);
            JobContext.DisplayStopwatch("End PasteIntoExcel 2");
            ProcessDiffs();
            PopulateHeader();
            DrawDivider();
            CloseExcel(this._outputFileName);
        }

        private void DrawDivider()
        {
            eWrapper.DrawDivider();
        }

        private void PopulateHeader()
        {
            eWrapper.PopulateHeader(1, this._inputFileName1);
            eWrapper.PopulateHeader(2, this._inputFileName2);
        }

        private void CloseExcel(string path)
        {
            eWrapper.SaveAndClose(path);
        }



        private void OpenExcel()
        {
            eWrapper = new ExcelWrapper();
            eWrapper.Open();
        }

        private void FinalExcelProcessing()
        {

        }

        private void PasteIntoExcel(int column, string fileName)
        {
            eWrapper.PasteXmlCol(column, fileName);
        }

        private void ProcessDiffs()
        {
            eWrapper.ProcessDiffsBySpreadsheet();
            //eWrapper.ProcessDiffsByData();

        }


        private void ReadIntoBrowser(string fileName)
        {
            string myDir = workDiectoryName;
            string myFile = fileName.Substring(fileName.LastIndexOf("\\") + 1);
            myDir = Directory.GetCurrentDirectory();

            WebBrowser1.Url = new Uri(String.Format("file:///{0}/{1}", myDir, myFile));

            string allText = WebBrowser1.DocumentText;
            WebBrowser1.Document.ExecCommand("SelectAll", true, null);
            WebBrowser1.Document.ExecCommand("Copy", false, null);
        }

        private void FormatHtml()
        {
            string sourceCode = File.ReadAllText(_finalFileName1);
            string colorizedSourceCode = new CodeColorizer().Colorize(sourceCode, Languages.Xml);
            System.IO.File.WriteAllText(_finalFileNameHtml1, htmlHeader + colorizedSourceCode + htmlTail);

            sourceCode = File.ReadAllText(_finalFileName2);
            colorizedSourceCode = new CodeColorizer().Colorize(sourceCode, Languages.Xml);
            System.IO.File.WriteAllText(_finalFileNameHtml2, htmlHeader + colorizedSourceCode + htmlTail);
        }

        internal void Compare()
        {
            XmlCompareProcessor xp = new XmlCompareProcessor();
            xp.ProcessDocument(_inputFileName1,
                               _inputFileName2,
                               _intermediateFileName1,
                               _intermediateFileName2);

          
            xp = new XmlCompareProcessor();
            xp.ProcessDocument(_intermediateFileName2,
                               _intermediateFileName1,
                               _finalFileName2,
                               _finalFileName1);
        }



        public System.Windows.Forms.WebBrowser WebBrowser1 { get; set; }
    }
}
