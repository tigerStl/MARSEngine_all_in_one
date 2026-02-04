using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.pdf.Interface
{
    public interface ReportGridDataInterface
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Column Name and its width</returns>
        List<KeyValuePair<string, int>> GetGridColumnInfo();
        bool BeginFetchRows();
        List<KeyValuePair<string, string>> FetchOneRowData();
        bool MoveToNextRow();
        void EndFetchRow();

        byte[] GetExtendImgData();
    }

    public interface ReportGridDataMasterInterface
    {
        int GetTopLevelLoopCount();
        
        void SetCurrentLoopId(int iLoopId);

        
    }

    public enum MarsRptGraphType
    {
        en_Pie=0x01,
        en_Bar,
        en_Line
    }

    public interface ReportGraphEnhance
    {
        MarsRptGraphType GetGrphType();

    }

    public interface MarsPieGraphEnhance: ReportGraphEnhance
    {
        int GetRadius();
        /// <summary>
        /// Pie graph captions and its percents
        /// </summary>
        /// <returns></returns>
        List<KeyValuePair<string, double>> GetPartsInfo();
    }
}
