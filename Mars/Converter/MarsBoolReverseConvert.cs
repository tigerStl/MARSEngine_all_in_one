using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Mars.Converter
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class MarsBoolReverseConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }
    }
    [ValueConversion(typeof(long?), typeof(string))]
    public class MarsTestReportResultConvert:IValueConverter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTestReportResultConvert));


        private const string SUCCESS = "PASS";
        private const string FAILED = "FAIL";
        private const string UNKNOWN = "FAIL";
        private const string PARTIAL = "PARTIAL";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";
            try
            {
                switch((short)value)
                {
                    case 1: return SUCCESS;
                    case 2: return FAILED;
                    case 3: return PARTIAL;
                    default:return UNKNOWN;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Convert",string.Format("Exception:[{0}]",e.Message),e);
                return "UNKNOW-ERROR";
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return -1;
            if (SUCCESS.CompareTo(value.ToString()) == 0) return 1;
            if (FAILED.CompareTo(value.ToString()) == 0) return 0;
            return -1;
        }
    }
}
