using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex;
using System.IO;

namespace Mars.TestFramework.DataCompare
{
    public class TextFileCompare
    {


        public void Compare(string fileName1, string fileName2, string outputFileName)
        {
            string str1;
            string str2;
            using (StreamReader sr = File.OpenText(fileName1))
            {
                str1 = sr.ReadToEnd();
            }

            using (StreamReader sr = File.OpenText(fileName2))
            {
                str2 = sr.ReadToEnd();
            }

            var d = new Differ();
            var sBuilder = new SideBySideDiffBuilder(d);
            var result = sBuilder.BuildDiffModel(str1, str2);

            ExcelWrapper excelFile = new ExcelWrapper();
            excelFile.Open();

            excelFile.ProcessTextDiff(result);

            excelFile.SaveAndClose(outputFileName);


        }


        // Test data

        private const string OldText =
           @"COMMON1
COMMON2
MISSING
COMMON3
DIFFERENT_TEXT_1";

        private const string NewText =
           @"COMMON1
COMMON2
COMMON3
DIFFERENT_TEXT_2";


        public void test()
        {
            var d = new Differ();
            var sBuilder = new SideBySideDiffBuilder(d);
            var result = sBuilder.BuildDiffModel(OldText, NewText);
        }
    }
}
