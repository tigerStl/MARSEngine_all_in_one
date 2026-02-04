#if _VEDIO_TIGER_
using WMPLib;
#endif

namespace TestFrameMonitor.source.media
{
    public class MarsPlayer
    {
#if _VEDIO_TIGER_
        private static WindowsMediaPlayer gObjPlayer = null;
#endif
        public static void PlayOneFullTestCase(string strFileName)
        {
#if _VEDIO_TIGER_
            if (gObjPlayer == null)
                gObjPlayer = new WindowsMediaPlayer();
            gObjPlayer.URL = strFileName;
            
            gObjPlayer.controls.currentPosition = 60.3;
            gObjPlayer.controls.play();

            gObjPlayer.controls.currentPosition = 60.3;
            //gObjPlayer.controls.currentPosition

            //gObjPlayer.openPlayer(strFileName);
#endif
        }
    }

#if _VEDIO_TIGER_
    public class PlaySpecialPositionForVedio
    {
        public DateTime PlayStart { get; set; }
        public DateTime PlayEnd { get; set; }
        public string CaptionIndexFile { get; set; }
        public string VedioFile { get; set; }
    }
#endif
}
