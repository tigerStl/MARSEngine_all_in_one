using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.MarsImage
{
    public class MarsImagePatternConst
    {
        public const string CNST_IMAGE_PATTERN_TYPE_SWFIMAGE_EDIT = "SwfImageEdit";
        public const string CNST_IMAGE_PATTERN_TYPE_SWFIMAGE_BUTTON = "SwfImageButton";
        public static string[] CNST_IMAGE_PATTERN_TYPE_LIST = new string[] { CNST_IMAGE_PATTERN_TYPE_SWFIMAGE_EDIT, CNST_IMAGE_PATTERN_TYPE_SWFIMAGE_BUTTON };

        public const string CNST_IMAGE_PATTERN_ID_SWFIMAGE_FILE = "SwfImageFile";
        public const string CNST_IMAGE_PATTERN_ID_SWFIMAGE_STREAM = "SwfImageStream"; // true or false
        //public const string CNST_IMAGE_PATTERN_ID_SWFIMAGE_BUTTON_KEY = "EditOffsetX";

    }
}
