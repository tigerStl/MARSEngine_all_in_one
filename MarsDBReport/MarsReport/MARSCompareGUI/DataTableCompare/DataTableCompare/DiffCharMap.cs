using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class DiffCharMap
    {
        public List<HighLightData> leftDiffData = new List<HighLightData>();
        public List<HighLightData> rightDiffData = new List<HighLightData>();
        public List<int> leftLineIndex = new List<int>();
        public List<int> rightLineIndex = new List<int>();

        public void GenerateMap(string leftText, string rightText)
        {
            var dmp = new diff_match_patch();
 
            var diffs = dmp.diff_main(leftText, rightText);

            dmp.diff_cleanupSemantic(diffs);
            
            leftLineIndex = CreateLineIndex(leftText);
            rightLineIndex = CreateLineIndex(rightText);
            CreateDiffData(diffs);
        }

        private List<int> CreateLineIndex(string text)
        {
            List<int> lineIdxList = new List<int>();
            int idx = 0;
            string[] lines = text.Split('\n');
            lineIdxList.Add(0);

            foreach (string line in lines)
            {
                idx += line.Length + 1;
                lineIdxList.Add(idx);
            }

            return lineIdxList;
        }

        private void CreateDiffData(List<Diff> diffs)
        {
            int leftPosition = 0;
            int rightPosition = 0;

            int lineStart = 0;
            int lineEnd = 0;

            HighLightData hlData;

            foreach (var diff in diffs)
            {
                switch (diff.operation)
                {
                    case Operation.EQUAL:
                        leftPosition += diff.text.Length;
                        rightPosition += diff.text.Length;
                        break;

                    case Operation.DELETE:
                        lineStart = getLineStart(leftPosition, "LEFT");
                        lineEnd = getLineEnd(leftPosition, "LEFT");
                        hlData = new HighLightData(leftPosition, leftPosition + diff.text.Length, lineStart, lineEnd);
                        leftDiffData.Add(hlData);
                        leftPosition += diff.text.Length;
                        break;

                    case Operation.INSERT:
                        lineStart = getLineStart(rightPosition, "RIGHT");
                        lineEnd = getLineEnd(rightPosition, "RIGHT");
                        hlData = new HighLightData(rightPosition, rightPosition + diff.text.Length, lineStart, lineEnd);
                        rightDiffData.Add(hlData);
                        rightPosition += diff.text.Length;
                        break;
                }

            }

            DataTable ddL = ToDataTable(leftDiffData);
        }

        private int getLineEnd(int leftPosition, string mode)
        {
            int num = 0;
            List<int> lineIindexList;
            if (mode.Equals("LEFT"))
                lineIindexList = leftLineIndex;
            else
                lineIindexList = rightLineIndex;

            foreach (int idx in lineIindexList)
            {
               // AF if (leftPosition <=idx)
                if (idx > leftPosition )
                {
                    num = idx;
                    break;
                }
            }

            return num;
        }

        private int getLineStart(int leftPosition, string mode)
        {
            int num = 0;
            List<int> lineIindexList;
            if (mode.Equals("LEFT"))
                lineIindexList = leftLineIndex;
            else
                lineIindexList = rightLineIndex;

            for (int i = lineIindexList.Count() - 1; i >= 0; i-- )
            {
                int idx = lineIindexList[i];
                if (leftPosition >= idx)
                {
                    num = idx;
                    break;
                }
            }

            return num;
        }

        public  DataTable ToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection props =
                TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                table.Columns.Add(prop.Name, prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (T item in data)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }
            return table;
        }

        /*
         *  public static DataTable ToDataTable<T>(this IList<T> data)
        {
            PropertyDescriptorCollection props =
                TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                table.Columns.Add(prop.Name, prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (T item in data)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }
            return table;
        }
         */
    }

    public class HighLightData
    {
        private int start;
        private int end;
        private int lineStart;
        private int lineEnd;
       
        public int Start
        {
            get
            {
                return start;
            }

            set
            {
                start = value;
            }
        }

        public int End
        {
            get
            {
                return end;
            }

            set
            {
                end = value;
            }
        }

        public int LineStart
        {
            get
            {
                return lineStart;
            }

            set
            {
                lineStart = value;
            }
        }

        public int LineEnd
        {
            get
            {
                return lineEnd;
            }

            set
            {
                lineEnd = value;
            }
        }

        public HighLightData(int s, int e, int ls, int le)
        {
            start = s;
            end = e;
            lineStart = ls;
            lineEnd = le;
        }
    }
}
