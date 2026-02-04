namespace MARS.CompareGUI
{
    internal class ProfileWithID
    {
        public ProfileWithID()
        {
        }

        public  string BaselineFmt { get; internal set; }
        public  string BaselineRpt { get; internal set; }
        public  string CompareFmt { get; internal set; }
        public  string CompareRpt { get; internal set; }
        public  string outDir { get; internal set; }
        public  string ProfileNameID { get; internal set; }
    }
}