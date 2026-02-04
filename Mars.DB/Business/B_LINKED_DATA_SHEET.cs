namespace Mars.message.Business
{
    public class B_LINKED_DATA_SHEET
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
            }
        }

        private string _dataItemName;

        public string DataItemName
        {
            get { return _dataItemName; }
            set { _dataItemName = value; }
        }

        private string _dataItemDescription;

        public string DataItemDescription
        {
            get { return _dataItemDescription; }
            set { _dataItemDescription = value; }
        }

        long _id;

        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

    }
}
