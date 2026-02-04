using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.AutoSteps
{

    public class Nlp_TestSteps
    {
        public string? keyword { get; set; } = null;
        public string? objectHappyName { get; set; }

        public string? data { get; set; }
        public string? parameter { set; get; }

        public string getTestStep()
        {
            return isError ? message??"No Message for the step":
                $"{keyword}({objectHappyName}, {parameter}, {data})";
        }

        public bool isError { get; set; } = false;
        public string? message { get; set; } = "";

        public override bool Equals(object? objSource)
        {
            if (objSource == null) return false;
            var obj = objSource as Nlp_TestSteps;

            return (((keyword==null)&&(obj.keyword==null))||((keyword!=null)&&(obj!=null)&&(obj.keyword!=null)&&(keyword.Equals(obj.keyword))))
                &&((((objectHappyName == null) && (obj.objectHappyName == null)) || ((objectHappyName != null) && (obj != null) && (obj.objectHappyName != null) && (objectHappyName.Equals(obj.objectHappyName)))))
                && ((((data == null) && (obj.data == null)) || ((data != null) && (obj != null) && (obj.data != null) && (data.Equals(obj.data)))))
                && ((((parameter == null) && (obj.parameter == null)) || ((parameter != null) && (obj != null) && (obj.parameter != null) && (parameter.Equals(obj.parameter)))))
                && ((((message == null) && (obj.message == null)) || ((message != null) && (obj != null) && (obj.message != null) && (message.Equals(obj.message)))))
                && (isError==obj.isError);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(keyword, objectHappyName,data, parameter, message, isError);
        }

        public static List<Nlp_TestSteps> RemoveConsecutiveDuplicates(List<Nlp_TestSteps> lstSource)
        {
            if (lstSource == null) return null;
            return lstSource.Distinct().ToList();
        }
    }
}
