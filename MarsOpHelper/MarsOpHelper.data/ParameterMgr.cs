using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsOpHelper.MarsOpHelper.data
{
    enum Mars_OpParaStatus : int
    {
        en_error_xyNotNum = -6,
        en_error_noType   = -5,
        en_error_nullPara = -4,
        en_error_noData   = -3,
        en_error_wrongXY  = -2,
        en_error_wrongType= -1, 
        en_None           =  0,
        en_Key            =  1, 
        en_mouse          =  2
    }

    public enum Mars_mouseSubType: int
    {
        
        en_LeftClick  = 1,
        
        en_LeftDblClick =2,
        
        en_rightClick =3,
        
        en_move = 4
    }
    /// <summary>
    /// 格式示例:
    /// -Type Mouse -X 100 -Y 200 -SubType LeftClick|LeftDoubleClick|RightClick|Move
    /// </summary>
    class ParameterMgr
    {
        public Mars_OpParaStatus opType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string dataForKey { get; set; }
        public Mars_mouseSubType subMouseType { get; set; }

        public const string cnst_X = "-X";
        public const string cnst_Y = "-Y";
        public const string cnst_Type = "-Type";
        public const string cnst_Data = "-Data";
        public const string cnst_clickType = "-ClickType";

        private ParameterMgr()
        {

        }

        internal static ParameterMgr GetInstance(string[] args,ref Mars_OpParaStatus opStatus, ref string strError )
        {
            if ((args == null) || (args.Length == 0))
            {
                opStatus = Mars_OpParaStatus.en_error_nullPara;
                strError = "args is null or empty";
                return null;
            }
            var t = args.Select((v, i) => new { data = v, index = i+1 }).FirstOrDefault(v=>cnst_Type.Equals(v.data, StringComparison.OrdinalIgnoreCase));
            var x = args.Select((v, i) => new { data = v, index = i+1 }).FirstOrDefault(v=>   cnst_X.Equals(v.data, StringComparison.OrdinalIgnoreCase));
            var y = args.Select((v, i) => new { data = v, index = i+1 }).FirstOrDefault(v=>   cnst_Y.Equals(v.data, StringComparison.OrdinalIgnoreCase));
            var d = args.Select((v, i) => new { data = v, index = i+1 }).FirstOrDefault(v=>cnst_Data.Equals(v.data, StringComparison.OrdinalIgnoreCase));
            var s = args.Select((v, i) => new { data = v, index = i+1 }).FirstOrDefault(v => cnst_clickType.Equals(v.data, StringComparison.OrdinalIgnoreCase));
            if (t == null)
            {
                opStatus = Mars_OpParaStatus.en_error_noType;
                strError = "no type";
                return null;
            }
            string strType = (t.index) >= args.Length ? null : args[t.index];
            int ix = int.MinValue, iy = int.MinValue;
            switch (strType.ToUpper())
            {
                case "M":
                case "MOUSE":
                    if ((x == null) || (y == null)||(x.index>=args.Length)||(y.index>=args.Length))
                    {
                        opStatus = Mars_OpParaStatus.en_error_wrongXY;
                        strError = "x or y value is empty";
                        return null;
                    }
                    if ((!int.TryParse(args[x.index], out ix))||(!int.TryParse(args[y.index],out iy))){
                        opStatus = Mars_OpParaStatus.en_error_xyNotNum;
                        strError = "x or y values should be number";
                        return null;
                    }
                    Mars_mouseSubType mouseSub = Mars_mouseSubType.en_LeftClick;
                    opStatus = Mars_OpParaStatus.en_mouse;
                    if (s != null)
                    {
                        
                        string subType = s.index>=args.Length?"LeftClick":args[s.index];

                        if ("RightClick".Equals(subType, StringComparison.OrdinalIgnoreCase))
                        {
                            mouseSub = Mars_mouseSubType.en_rightClick;
                        }else if ("LeftClick".Equals(subType, StringComparison.OrdinalIgnoreCase))
                        {
                            mouseSub = Mars_mouseSubType.en_LeftClick;
                        }else if (("LeftDblClick".Equals(subType, StringComparison.OrdinalIgnoreCase))
                                ||("LeftDoubleclick".Equals(subType, StringComparison.OrdinalIgnoreCase)))
                        {
                            mouseSub = Mars_mouseSubType.en_LeftDblClick;
                        }
                    } 
                    return new ParameterMgr()
                    {
                        opType = Mars_OpParaStatus.en_mouse,
                        X = ix,
                        Y = iy,
                        subMouseType = mouseSub
                    };
                    
                case "K":
                case "KEYBOARD":
                    if ((d == null) || (d.index >= args.Length))
                    {
                        opStatus = Mars_OpParaStatus.en_error_noData;
                        strError = @"Keyboard format is  
  MarsOpHelper -Type Keyboard -Data [string for keyboard event]";
                        return null;
                    }
                    opStatus = Mars_OpParaStatus.en_Key;
                    return new ParameterMgr()
                    {
                        opType = Mars_OpParaStatus.en_Key,
                        dataForKey = args[d.index]
                    };
                    
                default:
                    opStatus = Mars_OpParaStatus.en_error_noData;
                    strError = "Wrong type, Only";
                    return null;
            }
        }
    }
}
